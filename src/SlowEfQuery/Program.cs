using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using NuGet.Services.KeyVault;
using NuGet.Services.Sql;
using SlowEfQuery.Copy;

namespace SlowEfQuery
{
    public class PackageDependent
    {
        public string Id { get; set; }
        public int DownloadCount { get; set; }
        public string Description { get; set; }
        public bool IsVerified { get; set; }
    }

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Provide a package ID, vault name, and connection string.");
                return 1;
            }

            string id = args[0];
            string vaultName = args[1];
            string connectionString = args[2];
            Console.WriteLine($"Starting '{id}'...");
            Console.WriteLine($"Vault name: {vaultName}");
            Console.WriteLine($"Connection string: {connectionString}");
            Console.WriteLine();

            var secretReader = new KeyVaultReader(new KeyVaultConfiguration(vaultName));
            var secretInjector = new SecretInjector(secretReader);

            var connStrBuilder = new AzureSqlConnectionStringBuilder(connectionString);
            var connFactory = new AzureSqlConnectionFactory(connStrBuilder, secretInjector);

            using (var connection = connFactory.CreateAsync().Result)
            using (var entitiesContext = new EntitiesContext(connection, readOnly: true))
            {
                var stopwatch = Stopwatch.StartNew();
                Output(WithRawSql(id, entitiesContext, parameterSize: 128));
                Console.WriteLine($"Raw SQL - NVARCHAR(128): {stopwatch.Elapsed}");
                Console.WriteLine();

                stopwatch.Restart();
                Output(WithRawSql(id, entitiesContext, parameterSize: 4000));
                Console.WriteLine($"Raw SQL - NVARCHAR(4000): {stopwatch.Elapsed}");
                Console.WriteLine();

                stopwatch.Restart();
                Output(WithEntityFramework(id, entitiesContext));
                Console.WriteLine($"LINQ: {stopwatch.Elapsed}");
                Console.WriteLine();

                stopwatch.Restart();
                Output(WithEntityFramework(id, entitiesContext));
                Console.WriteLine($"LINQ: {stopwatch.Elapsed}");
                Console.WriteLine();
            }

            return 0;
        }

        public static void Output(List<PackageDependent> packageDependents)
        {
            foreach (var pd in packageDependents)
            {
                Console.WriteLine($"{pd.Id} - description is {pd.Description.Length} characters - {pd.DownloadCount} downloads - {(pd.IsVerified ? "verified" : "not verified")}");
            }
        }

        private static List<PackageDependent> WithRawSql(string id, EntitiesContext entitiesContext, int parameterSize)
        {
            var connection = entitiesContext.Database.Connection;
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var packageDependentsList = new List<PackageDependent>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT TOP (5) 
    [Project2].[DownloadCount] AS [DownloadCount], 
    [Project2].[Id] AS [Id], 
    [Project2].[IsVerified] AS [IsVerified], 
    [Project2].[Description] AS [Description]
    FROM ( SELECT 
        [Distinct1].[Description] AS [Description], 
        [Distinct1].[Id] AS [Id], 
        [Distinct1].[DownloadCount] AS [DownloadCount], 
        [Distinct1].[IsVerified] AS [IsVerified]
        FROM ( SELECT DISTINCT 
            [Filter1].[Description] AS [Description], 
            [Extent3].[Id] AS [Id], 
            [Extent3].[DownloadCount] AS [DownloadCount], 
            [Extent3].[IsVerified] AS [IsVerified]
            FROM   (SELECT [Extent1].[Id] AS [Id], [Extent2].[PackageRegistrationKey] AS [PackageRegistrationKey], [Extent2].[Description] AS [Description]
                FROM  [dbo].[PackageDependencies] AS [Extent1]
                INNER JOIN [dbo].[Packages] AS [Extent2] ON [Extent1].[PackageKey] = [Extent2].[Key]
                WHERE [Extent2].[IsLatestSemVer2] = 1 ) AS [Filter1]
            INNER JOIN [dbo].[PackageRegistrations] AS [Extent3] ON [Filter1].[PackageRegistrationKey] = [Extent3].[Key]
            WHERE ([Filter1].[Id] = @p__linq__0) OR (([Filter1].[Id] IS NULL) AND (@p__linq__0 IS NULL))
        )  AS [Distinct1]
    )  AS [Project2]
    ORDER BY [Project2].[DownloadCount] DESC";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@p__linq__0";
                parameter.Value = id;
                parameter.Size = parameterSize;
                command.Parameters.Add(parameter);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var dependent = new PackageDependent();
                        dependent.Id = (string)reader["id"];
                        dependent.DownloadCount = (int)reader["DownloadCount"];
                        dependent.Description = (string)reader["Description"];
                        dependent.IsVerified = (bool)reader["IsVerified"];
                        packageDependentsList.Add(dependent);
                    }
                }

                return packageDependentsList;
            }
        }

        private static List<PackageDependent> WithEntityFramework(string id, EntitiesContext entitiesContext)
        {
            int packagesDisplayed = 5;
            var listPackages = (from pd in entitiesContext.PackageDependencies
                                join p in entitiesContext.Packages on pd.PackageKey equals p.Key
                                join pr in entitiesContext.PackageRegistrations on p.PackageRegistrationKey equals pr.Key
                                where p.IsLatestSemVer2 && pd.Id == id
                                group 1 by new { pr.Id, pr.DownloadCount, pr.IsVerified, p.Description } into ng
                                orderby ng.Key.DownloadCount descending
                                select new PackageDependent { Id = ng.Key.Id, DownloadCount = ng.Key.DownloadCount, IsVerified = ng.Key.IsVerified, Description = ng.Key.Description }
                                ).Take(packagesDisplayed).ToList();

            return listPackages;
        }
    }
}
