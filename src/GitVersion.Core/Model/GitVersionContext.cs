using GitVersion.Model.Configuration;

namespace GitVersion;

/// <summary>
///     Contextual information about where GitVersion is being run
/// </summary>
public class GitVersionContext
{
    public GitVersionContext(
        IBranch currentBranch,
        ICommit? currentCommit,
        Config configuration,
        EffectiveConfiguration effectiveConfiguration,
        SemanticVersion currentCommitTaggedVersion,
        int numberOfUncommittedChanges)
    {
        CurrentBranch = currentBranch;
        CurrentCommit = currentCommit;
        FullConfiguration = configuration;
        Configuration = effectiveConfiguration;
        CurrentCommitTaggedVersion = currentCommitTaggedVersion;
        NumberOfUncommittedChanges = numberOfUncommittedChanges;
    }

    /// <summary>
    ///     Contains the raw configuration, use Configuration for specific config based on the current GitVersion context.
    /// </summary>
    public Config FullConfiguration { get; }

    public SemanticVersion? CurrentCommitTaggedVersion { get; }
    public EffectiveConfiguration Configuration { get; }
    public IBranch CurrentBranch { get; }
    public ICommit? CurrentCommit { get; }
    public bool IsCurrentCommitTagged => CurrentCommitTaggedVersion != null;
    public int NumberOfUncommittedChanges { get; }
}
