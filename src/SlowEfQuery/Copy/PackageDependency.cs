using System.ComponentModel.DataAnnotations;

namespace SlowEfQuery.Copy
{
    public class PackageDependency
    {
        public Package Package { get; set; }
        public int PackageKey { get; set; }

        [StringLength(128)]
        public string Id { get; set; }

        public string VersionSpec { get; set; }
        public string TargetFramework { get; set; }
        public int Key { get; set; }
    }
}
