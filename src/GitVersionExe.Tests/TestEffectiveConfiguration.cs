namespace GitVersionExe.Tests
{
    using GitVersion;
    using GitVersion.VersionFilters;
    using System.Collections.Generic;
    using System.Linq;

    public class TestEffectiveConfiguration : EffectiveConfiguration
    {
        public TestEffectiveConfiguration(
            AssemblyVersioningScheme assemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
            AssemblyFileVersioningScheme assemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch,
            string assemblyVersioningFormat = null,
            string assemblyFileVersioningFormat = null,
            string assemblyInformationalFormat = null,
            VersioningMode versioningMode = VersioningMode.ContinuousDelivery,
            string gitTagPrefix = "v",
            string tag = "",
            string nextVersion = null,
            string branchPrefixToTrim = "",
            bool preventIncrementForMergedBranchVersion = false,
            string tagNumberPattern = null,
            string continuousDeploymentFallbackTag = "ci",
            bool trackMergeTarget = false,
            string majorMessage = null,
            string minorMessage = null,
            string patchMessage = null,
            string noBumpMessage = null,
            CommitMessageIncrementMode commitMessageMode = CommitMessageIncrementMode.Enabled,
            int legacySemVerPadding = 4,
            int buildMetaDataPadding = 4,
            int commitsSinceVersionSourcePadding = 4,
            IEnumerable<IVersionFilter> versionFilters = null,
            bool tracksReleaseBranches = false,
            bool isRelease = false,
            string commitDateFormat = "yyyy-MM-dd",
            bool useMergeMessageVersion = true) :
             base(assemblyVersioningScheme, assemblyFileVersioningScheme, assemblyInformationalFormat, assemblyVersioningFormat, assemblyFileVersioningFormat, versioningMode, gitTagPrefix, tag, nextVersion, IncrementStrategy.Patch,
                    branchPrefixToTrim, preventIncrementForMergedBranchVersion, tagNumberPattern, continuousDeploymentFallbackTag,
                    trackMergeTarget,
                    majorMessage, minorMessage, patchMessage, noBumpMessage,
                    commitMessageMode, legacySemVerPadding, buildMetaDataPadding, commitsSinceVersionSourcePadding,
                    versionFilters ?? Enumerable.Empty<IVersionFilter>(),
                    tracksReleaseBranches, isRelease, commitDateFormat, useMergeMessageVersion)

        {
        }
    }
}
