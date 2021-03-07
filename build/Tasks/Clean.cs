using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Frosting;

namespace GitVersion.Build.Tasks
{

    [TaskName(nameof(Clean))]
    [TaskDescription("Cleans build artifacts")]
    public sealed class Clean : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.Information("Cleaning directories...");

            context.CleanDirectories(context.Paths.Src + "/**/bin/" + context.Configuration);
            context.CleanDirectories(context.Paths.Src + "/**/obj");
        }
    }
}
