using System.Collections.Generic;

namespace SlowEfQuery.Copy
{
    public class PackageRegistration
    {
        public PackageRegistration()
        {
            Packages = new HashSet<Package>();
        }

        public string Id { get; set; }

        public int DownloadCount { get; set; }

        public bool IsVerified { get; set; }
        public virtual ICollection<Package> Packages { get; set; }

        public int Key { get; set; }
    }
}
