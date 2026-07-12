using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using VaultGuard.DAL;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace VaultGuard.DAL.MySql
{
    public class MySqlContextFactory : IDesignTimeDbContextFactory<VaultGuardDbContext>
    {
        public VaultGuardDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            var connectionString = config.GetConnectionString("MySqlConnection");
            var optionsBuilder = new DbContextOptionsBuilder<VaultGuardDbContext>();
            // Use Pomelo.EntityFrameworkCore.MySql (ensure correct version)
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            return new VaultGuardDbContext(optionsBuilder.Options);
        }
    }
}
