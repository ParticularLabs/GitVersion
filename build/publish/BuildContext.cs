using System.Collections.Generic;
using Cake.Core;
using Common.Utilities;
using Publish.Utilities;

namespace Publish
{
    public class BuildContext : BuildContextBase
    {
        public BuildCredentials? Credentials { get; set; }


        public List<NugetPackage> Packages { get; set; }
        public BuildContext(ICakeContext context) : base(context)
        {
        }
    }
}
