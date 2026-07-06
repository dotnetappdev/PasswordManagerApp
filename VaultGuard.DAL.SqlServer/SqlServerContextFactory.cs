using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using VaultGuard.DAL;

namespace VaultGuard.DAL.SqlServer
{
    public class SqlServerContextFactory : IDesignTimeDbContextFactory<VaultGuardDbContextApp>
    {
        public VaultGuardDbContextApp CreateDbContext(string[] args)
        {
            // Pick up the environment overlay (defaults to Development for local `dotnet ef`) so the
            // GoogleSecretManager section resolves to the right project id / env label.
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Development";

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Prefer the Google Secret Manager-composed connection string (same db* secrets the API uses);
            // falls back to ConnectionStrings:DefaultConnection, then a design-time placeholder (migrations
            // add/scaffold never open a connection, so the placeholder is only used to satisfy UseSqlServer).
            var connectionString = SecretManagerConnectionResolver.Resolve(config)
                ?? "Server=localhost;Database=VaultGuardDesignTime;Trusted_Connection=True;TrustServerCertificate=True";

            var optionsBuilder = new DbContextOptionsBuilder<VaultGuardDbContextApp>();
            // Keep SQL Server migrations in THIS assembly, separate from the SQLite migrations in VaultGuard.DAL.
            optionsBuilder.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly("VaultGuard.DAL.SqlServer"));
            return new VaultGuardDbContextApp(optionsBuilder.Options);
        }
    }
}
