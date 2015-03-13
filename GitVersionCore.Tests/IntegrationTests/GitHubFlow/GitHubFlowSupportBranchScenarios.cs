﻿using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class GitHubFlowSupportBranchScenarios
{
    [Test]
    public void SupportIsCalculatedCorrectly()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            // Start at 1.0.0
            fixture.Repository.MakeACommit();
            fixture.Repository.ApplyTag("1.1.0");

            // Create 2.0.0 release
            var releaseBranch = fixture.Repository.CreateBranch("release-2.0.0");
            fixture.Repository.Checkout(releaseBranch);
            fixture.Repository.MakeCommits(2);

            // Merge into develop and master
            fixture.Repository.Checkout("master");
            fixture.Repository.MergeNoFF("release-2.0.0");
            fixture.Repository.ApplyTag("2.0.0");
            fixture.AssertFullSemver("2.0.0+0");

            // Now lets support 1.x release
            fixture.Repository.Checkout("1.1.0");
            var supportBranch = fixture.Repository.CreateBranch("support/1.0.0");
            fixture.Repository.Checkout(supportBranch);
            fixture.AssertFullSemver("1.1.0+0");

            // Create release branch from support branch
            var newReleaseBranch = fixture.Repository.CreateBranch("release/1.2.0");
            fixture.Repository.Checkout(newReleaseBranch);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.0-beta.1+1");

            // Create 1.2.0 release
            fixture.Repository.Checkout("support/1.0.0");
            fixture.Repository.MergeNoFF("release/1.2.0");
            fixture.AssertFullSemver("1.2.0+2");
            fixture.Repository.ApplyTag("1.2.0");

            // Create 1.2.1 hotfix
            var hotfixBranch = fixture.Repository.CreateBranch("hotfix/1.2.1");
            fixture.Repository.Checkout(hotfixBranch);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.2.1+1");
            fixture.Repository.Checkout("support/1.0.0");
            fixture.Repository.MergeNoFF("hotfix/1.2.1");
            fixture.AssertFullSemver("1.2.1+2");
        }
    }

    [Test]
    public void WhenSupportIsBranchedAndTaggedFromAnotherSupportEnsureNewMinorIsUsed()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("Support-1.2.0");
            fixture.Repository.Checkout("Support-1.2.0");
            fixture.Repository.MakeACommit();
            fixture.Repository.ApplyTag("1.2.0");

            fixture.Repository.CreateBranch("Support-1.3.0");
            fixture.Repository.Checkout("Support-1.3.0");
            fixture.Repository.ApplyTag("1.3.0");

            //Move On
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.3.1+2");
        }
    }
}