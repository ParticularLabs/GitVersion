using System;
using GitVersion;
using LibGit2Sharp;
using Branch = GitVersion.Branch;
using BranchCollection = GitVersion.BranchCollection;
using ReferenceCollection = GitVersion.ReferenceCollection;
using TagCollection = GitVersion.TagCollection;

namespace GitVersionCore.Tests.Mocks
{
    public class MockRepository : IGitRepository
    {
        private CommitCollection commits;

        public MockRepository()
        {
            Tags = new MockTagCollection();
            Refs = new MockReferenceCollection();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Branch Head { get; set; }
        public ReferenceCollection Refs { get; set; }

        public CommitCollection Commits
        {
            get => commits ?? Head.Commits;
            set => commits = value;
        }

        public BranchCollection Branches { get; set; }
        public TagCollection Tags { get; set; }
        public RepositoryInformation Info { get; set; }
        public Diff Diff { get; set; }
        public ObjectDatabase ObjectDatabase { get; set; }

        public Network Network { get; set; }
        public RepositoryStatus RetrieveStatus()
        {
            throw new NotImplementedException();
        }
        public IGitRepositoryCommands Commands { get; }
    }
}
