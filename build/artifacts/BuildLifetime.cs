using System.Linq;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Docker;
using Common.Utilities;
using Constants = Common.Utilities.Constants;

namespace Artifacts
{
    public class BuildLifetime : BuildLifetimeBase<BuildContext>
    {
        public override void Setup(BuildContext context)
        {
            base.Setup(context);

            context.IsDockerOnLinux = context.DockerCustomCommand("info --format '{{.OSType}}'").First().Replace("'", string.Empty) == "linux";

            var architecture = context.Argument(Arguments.Architecture, Architecture.Amd64);
            var dockerRegistry = context.Argument(Arguments.DockerRegistry, DockerRegistry.DockerHub);
            var dotnetVersion = context.Argument(Arguments.DockerDotnetVersion, string.Empty).ToLower();
            var dockerDistro = context.Argument(Arguments.DockerDistro, string.Empty).ToLower();

            var versions = string.IsNullOrWhiteSpace(dotnetVersion) ? Constants.VersionsToBuild : new[] { dotnetVersion };
            var distros = string.IsNullOrWhiteSpace(dockerDistro) ? Constants.DockerDistrosToBuild : new[] { dockerDistro };
            var archs = string.IsNullOrWhiteSpace(dockerDistro) ? Constants.ArchToBuild : new[] { architecture };

            var registry = dockerRegistry == DockerRegistry.DockerHub ? Constants.DockerHubRegistry : Constants.GitHubContainerRegistry;
            context.Images = from version in versions
                             from distro in distros
                             from arch in archs
                             select new DockerImage(distro, version, arch, registry, true);

            context.StartGroup("Build Setup");

            LogBuildInformation(context);

            context.Information("IsDockerOnLinux:   {0}", context.IsDockerOnLinux);
            context.Information($"Building for Version: {dotnetVersion}, Distro: {dockerDistro}");
            context.EndGroup();
        }
    }
}
