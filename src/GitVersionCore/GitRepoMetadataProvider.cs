﻿using JetBrains.Annotations;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GitVersion
{
    public class GitRepoMetadataProvider
    {
        private Dictionary<Branch, List<BranchCommit>> mergeBaseCommitsCache;
        private Dictionary<MergeBaseKey, Commit> mergeBaseCache;
        private Dictionary<Branch, List<SemanticVersion>> semanticVersionTagsOnBranchCache;
        private IRepository Repository { get; set; }
        const string missingTipFormat = "{0} has no tip. Please see http://example.com/docs for information on how to fix this.";

        public GitRepoMetadataProvider(IRepository repository)
        {
            mergeBaseCache = new Dictionary<MergeBaseKey, Commit>();
            mergeBaseCommitsCache = new Dictionary<Branch, List<BranchCommit>>();
            semanticVersionTagsOnBranchCache = new Dictionary<Branch, List<SemanticVersion>>();
            Repository = repository;
        }

        public IEnumerable<SemanticVersion> GetVersionTagsOnBranch(Branch branch, string tagPrefixRegex)
        {
            if (semanticVersionTagsOnBranchCache.ContainsKey(branch))
            {
                Logger.WriteDebug(string.Format("Cache hit for version tags on branch '{0}", branch.CanonicalName));
                return semanticVersionTagsOnBranchCache[branch];
            }

            using (Logger.IndentLog(string.Format("Getting version tags from branch '{0}'.", branch.CanonicalName)))
            {
                var tags = Repository.Tags.Select(t => t).ToList();

                var versionTags = Repository.Commits.QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = branch.Tip
                })
                .SelectMany(c => tags.Where(t => c.Sha == t.Target.Sha).SelectMany(t =>
                {
                    SemanticVersion semver;
                    if (SemanticVersion.TryParse(t.FriendlyName, tagPrefixRegex, out semver))
                        return new[] { semver };
                    return new SemanticVersion[0];
                })).ToList();

                semanticVersionTagsOnBranchCache.Add(branch, versionTags);
                return versionTags;
            }
        }

        // TODO Should we cache this?
        public IEnumerable<Branch> GetBranchesContainingCommit([NotNull] Commit commit, IList<Branch> branches, bool onlyTrackedBranches)
        {
            if (commit == null)
            {
                throw new ArgumentNullException("commit");
            }

            using (Logger.IndentLog(string.Format("Getting branches containing the commit '{0}'.", commit.Id)))
            {
                var directBranchHasBeenFound = false;
                Logger.WriteInfo("Trying to find direct branches.");
                // TODO: It looks wasteful looping through the branches twice. Can't these loops be merged somehow? @asbjornu
                foreach (var branch in branches)
                {
                    if (branch.Tip != null && branch.Tip.Sha != commit.Sha || (onlyTrackedBranches && !branch.IsTracking))
                    {
                        continue;
                    }

                    directBranchHasBeenFound = true;
                    Logger.WriteInfo(string.Format("Direct branch found: '{0}'.", branch.FriendlyName));
                    yield return branch;
                }

                if (directBranchHasBeenFound)
                {
                    yield break;
                }

                Logger.WriteInfo(string.Format("No direct branches found, searching through {0} branches.", onlyTrackedBranches ? "tracked" : "all"));
                foreach (var branch in branches.Where(b => onlyTrackedBranches && !b.IsTracking))
                {
                    Logger.WriteInfo(string.Format("Searching for commits reachable from '{0}'.", branch.FriendlyName));

                    var commits = Repository.Commits.QueryBy(new CommitFilter
                    {
                        IncludeReachableFrom = branch
                    }).Where(c => c.Sha == commit.Sha);

                    if (!commits.Any())
                    {
                        Logger.WriteInfo(string.Format("The branch '{0}' has no matching commits.", branch.FriendlyName));
                        continue;
                    }

                    Logger.WriteInfo(string.Format("The branch '{0}' has a matching commit.", branch.FriendlyName));
                    yield return branch;
                }
            }
        }

        /// <summary>
        /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
        /// Note that a local branch and a remote branch tracked by it (or when untracked, has the same name) are considered the same.
        /// </summary>
        public Commit FindMergeBase(Branch branch, Branch otherBranch)
        {
            if (branch.IsSameBranch(otherBranch))
            {
                Logger.WriteDebug(string.Format(
                    "The branches '{0}' and '{1}' are considered equal.",
                    branch.FriendlyName, otherBranch.FriendlyName));
                return null;
            }

            Commit mergeBase;
            var key = new MergeBaseKey(branch, otherBranch);
            if (mergeBaseCache.TryGetValue(key, out mergeBase))
            {
                Logger.WriteDebug(string.Format(
                    "Cache hit for merge base between '{0}' and '{1}'.",
                    branch.FriendlyName, otherBranch.FriendlyName));
                return mergeBase;
            }

            using (Logger.IndentLog(string.Format("Finding merge base between '{0}' and '{1}'.", branch.FriendlyName, otherBranch.FriendlyName)))
            {
                // Otherbranch tip is a forward merge
                var commitToFindCommonBase = otherBranch.Tip;
                var commit = branch.Tip;
                if (otherBranch.Tip.Parents.Contains(commit))
                {
                    commitToFindCommonBase = otherBranch.Tip.Parents.First();
                }

                mergeBase = Repository.ObjectDatabase.FindMergeBase(commit, commitToFindCommonBase);
                if (mergeBase != null)
                {
                    Logger.WriteInfo(string.Format("Found merge base of {0}", mergeBase.Sha));
                    // We do not want to include merge base commits which got forward merged into the other branch
                    bool mergeBaseWasForwardMerge;
                    do
                    {
                        // Now make sure that the merge base is not a forward merge
                        mergeBaseWasForwardMerge = otherBranch.Commits
                            .SkipWhile(c => c != commitToFindCommonBase)
                            .TakeWhile(c => c != mergeBase)
                            .Any(c => c.Parents.Contains(mergeBase));
                        if (mergeBaseWasForwardMerge)
                        {
                            var secondParent = commitToFindCommonBase.Parents.First();
                            var forwardMergeBase = Repository.ObjectDatabase.FindMergeBase(commit, secondParent);
                            if (forwardMergeBase == mergeBase)
                            {
                                break;
                            }
                            mergeBase = forwardMergeBase;
                            Logger.WriteInfo(string.Format("Merge base was due to a forward merge, next merge base is {0}", mergeBase));
                        }
                    } while (mergeBaseWasForwardMerge);
                }

                // Store in cache.
                mergeBaseCache.Add(key, mergeBase);

                return mergeBase;
            }
        }

        /// <summary>
        /// Find the commit where the given branch was branched from another branch.
        /// If there are multiple such commits and branches, returns the newest commit.
        /// </summary>
        public BranchCommit FindCommitBranchWasBranchedFrom([NotNull] Branch branch, params Branch[] excludedBranches)
        {
            if (branch == null)
            {
                throw new ArgumentNullException("branch");
            }

            using (Logger.IndentLog(string.Format("Finding branch source of '{0}'", branch.FriendlyName)))
            {
                if (branch.Tip == null)
                {
                    Logger.WriteWarning(string.Format(missingTipFormat, branch.FriendlyName));
                    return BranchCommit.Empty;
                }

                return GetMergeCommitsForBranch(branch).ExcludingBranches(excludedBranches).FirstOrDefault(b => !branch.IsSameBranch(b.Branch));
            }
        }

        List<BranchCommit> GetMergeCommitsForBranch(Branch branch)
        {
            if (mergeBaseCommitsCache.ContainsKey(branch))
            {
                Logger.WriteDebug(string.Format(
                    "Cache hit for getting merge commits for branch {0}.",
                    branch.CanonicalName));
                return mergeBaseCommitsCache[branch];
            }

            // Since local and remote branches are considered equal, make sure that local branches are considered first.
            var branchesSortedLocalFirst = Repository.Branches.Where(b => !b.IsRemote).Concat(Repository.Branches.Where(b => b.IsRemote));
            var branchMergeBases = branchesSortedLocalFirst.Select(otherBranch =>
            {
                if (otherBranch.Tip == null)
                {
                    Logger.WriteWarning(string.Format(missingTipFormat, otherBranch.FriendlyName));
                    return BranchCommit.Empty;
                }

                var findMergeBase = FindMergeBase(branch, otherBranch);
                return new BranchCommit(findMergeBase, otherBranch);
            }).Where(b => b.Commit != null).OrderByDescending(b => b.Commit.Committer.When).ToList();
            mergeBaseCommitsCache.Add(branch, branchMergeBases);

            return branchMergeBases;
        }

        /// <summary>
        /// The key for the merge base data.
        /// Note that the merge base of two branches is symmetric,
        /// i.e. the merge base of 'branchA' and 'branchB' is the same as 'branchB' and 'branchA'.
        /// </summary>
        [DebuggerDisplay("A: {BranchA.CanonicalName}; B: {BranchB.CanonicalName}")]
        private struct MergeBaseKey : IEquatable<MergeBaseKey>
        {
            private Branch BranchA { get; set; }
            private Branch BranchB { get; set; }

            public MergeBaseKey(Branch branchA, Branch branchB) : this()
            {
                BranchA = branchA;
                BranchB = branchB;
            }

            public bool Equals(MergeBaseKey other)
            {
                return (BranchA.IsSameBranch(other.BranchA) && BranchB.IsSameBranch(other.BranchB)) ||
                       (BranchB.IsSameBranch(other.BranchA) && BranchA.IsSameBranch(other.BranchB));
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                return obj is MergeBaseKey && Equals((MergeBaseKey)obj);
            }

            public override int GetHashCode()
            {
                return BranchA.GetComparisonBranchName().GetHashCode() ^ BranchB.GetComparisonBranchName().GetHashCode();
            }

            public static bool operator ==(MergeBaseKey left, MergeBaseKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(MergeBaseKey left, MergeBaseKey right)
            {
                return !left.Equals(right);
            }
        }
    }
}