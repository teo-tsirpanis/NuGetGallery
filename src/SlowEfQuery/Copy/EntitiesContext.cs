using System.Data.Common;
using System.Data.Entity;

namespace SlowEfQuery.Copy
{
    public class EntitiesContext : DbContext
    {
        public EntitiesContext(string connectionString, bool readOnly) : base(connectionString)
        {
        }

        public EntitiesContext(DbConnection existingConnection, bool readOnly) : base(existingConnection, contextOwnsConnection: true)
        {
        }

        public DbSet<Package> Packages { get; set; }
        public DbSet<PackageRegistration> PackageRegistrations { get; set; }
        public DbSet<PackageDependency> PackageDependencies { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PackageRegistration>()
                .HasKey(pr => pr.Key);

            modelBuilder.Entity<PackageRegistration>()
                .HasMany<Package>(pr => pr.Packages)
                .WithRequired(p => p.PackageRegistration)
                .HasForeignKey(p => p.PackageRegistrationKey);

            modelBuilder.Entity<Package>()
                .HasKey(p => p.Key);

            modelBuilder.Entity<Package>()
                .HasMany<PackageDependency>(p => p.Dependencies)
                .WithRequired(pd => pd.Package)
                .HasForeignKey(pd => pd.PackageKey);

            modelBuilder.Entity<PackageDependency>()
                .HasKey(pd => pd.Key);
        }
    }
}
