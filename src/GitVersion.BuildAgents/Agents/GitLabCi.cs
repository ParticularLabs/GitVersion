using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class GitLabCi : BuildAgentBase
{
    public const string EnvironmentVariableName = "GITLAB_CI";
    private string? file;

    public GitLabCi(IEnvironment environment, ILog log) : base(environment, log) => WithPropertyFile("gitversion.properties");

    public void WithPropertyFile(string propertiesFileName) => this.file = propertiesFileName;

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string GenerateSetVersionMessage(GitVersionVariables variables) => variables.FullSemVer;

    public override string[] GenerateSetParameterMessage(string name, string? value) =>
    [
        $"GitVersion_{name}={value}"
    ];

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        // CI_COMMIT_REF_NAME can contain either the branch or the tag
        // See https://docs.gitlab.com/ee/ci/variables/predefined_variables.html

        // CI_COMMIT_TAG is only available in tag pipelines,
        // so we can exit if CI_COMMIT_REF_NAME would return the tag
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI_COMMIT_TAG")))
            return null;

        return Environment.GetEnvironmentVariable("CI_COMMIT_REF_NAME");
    }

    public override bool PreventFetch() => true;

    public override void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true)
    {
        if (this.file is null)
            return;

        base.WriteIntegration(writer, variables, updateBuildNumber);
        writer($"Outputting variables to '{this.file}' ... ");

        File.WriteAllLines(this.file, GenerateBuildLogOutput(variables));
    }
}
