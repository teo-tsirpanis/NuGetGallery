using System.Collections.Generic;

namespace SlowEfQuery.Copy
{
    public class Package
    {
        public Package()
        {
            Dependencies = new HashSet<PackageDependency>();
        }

        public PackageRegistration PackageRegistration { get; set; }
        public int PackageRegistrationKey { get; set; }
        public virtual ICollection<PackageDependency> Dependencies { get; set; }
        public string Description { get; set; }
        public bool IsLatestSemVer2 { get; set; }
        public bool IsLatestStableSemVer2 { get; set; }
        public string Version { get; set; }
        public string NormalizedVersion { get; set; }
        public int Key { get; set; }
    }
}
