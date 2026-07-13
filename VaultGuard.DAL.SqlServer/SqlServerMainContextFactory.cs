using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using VaultGuard.DAL;

namespace VaultGuard.DAL.SqlServer
{
    /// <summary>
    /// Design-time factory for the API data context (<see cref="VaultGuardDbContext"/>) on SQL Server.
    /// Mirrors <see cref="SqlServerContextFactory"/> (the Identity context) so `dotnet ef` can generate and
    /// apply SQL-Server-typed migrations for both contexts, kept in this assembly and separate from the
    /// SQLite migrations in VaultGuard.DAL.
    /// </summary>
    public class SqlServerMainContextFactory : IDesignTimeDbContextFactory<VaultGuardDbContext>
    {
        public VaultGuardDbContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Development";

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=VaultGuardDesignTime;Trusted_Connection=True;TrustServerCertificate=True";

            var optionsBuilder = new DbContextOptionsBuilder<VaultGuardDbContext>();
            optionsBuilder.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly("VaultGuard.DAL.SqlServer"));
            return new VaultGuardDbContext(optionsBuilder.Options);
        }
    }
}
