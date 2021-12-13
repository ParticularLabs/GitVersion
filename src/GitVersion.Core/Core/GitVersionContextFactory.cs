using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using Microsoft.Extensions.Options;

namespace GitVersion;

public class GitVersionContextFactory : IGitVersionContextFactory
{
    private readonly IConfigProvider configProvider;
    private readonly IRepositoryStore repositoryStore;
    private readonly IBranchConfigurationCalculator branchConfigurationCalculator;
    private readonly IOptions<GitVersionOptions> options;

    public GitVersionContextFactory(IConfigProvider configProvider, IRepositoryStore repositoryStore, IBranchConfigurationCalculator branchConfigurationCalculator, IOptions<GitVersionOptions> options)
    {
        this.configProvider = configProvider.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.branchConfigurationCalculator = branchConfigurationCalculator.NotNull();
        this.options = options.NotNull();
    }

    public GitVersionContext Create(GitVersionOptions? gitVersionOptions)
    {
        var currentBranch = this.repositoryStore.GetTargetBranch(gitVersionOptions?.RepositoryInfo.TargetBranch);
        if (currentBranch == null)
            throw new InvalidOperationException("Need a branch to operate on");

        var configuration = this.configProvider.Provide(overrideConfig: this.options.Value.ConfigInfo.OverrideConfig);

        var currentCommit = this.repositoryStore.GetCurrentCommit(currentBranch, gitVersionOptions?.RepositoryInfo.CommitId);

        if (currentBranch.IsDetachedHead)
        {
            var branchForCommit = this.repositoryStore.GetBranchesContainingCommit(currentCommit, onlyTrackedBranches: gitVersionOptions?.Settings.OnlyTrackedBranches == true).OnlyOrDefault();
            currentBranch = branchForCommit ?? currentBranch;
        }

            var currentBranchConfig = this.branchConfigurationCalculator.GetBranchConfiguration(0, currentBranch, currentCommit, configuration);
        var effectiveConfiguration = configuration.CalculateEffectiveConfiguration(currentBranchConfig);
        var currentCommitTaggedVersion = this.repositoryStore.GetCurrentCommitTaggedVersion(currentCommit, effectiveConfiguration);
        var numberOfUncommittedChanges = this.repositoryStore.GetNumberOfUncommittedChanges();

        return new GitVersionContext(currentBranch, currentCommit, configuration, effectiveConfiguration, currentCommitTaggedVersion, numberOfUncommittedChanges);
    }
}
