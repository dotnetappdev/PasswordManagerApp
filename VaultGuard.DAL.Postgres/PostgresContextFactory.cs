using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using VaultGuard.Models;
using System.IO;

namespace VaultGuard.DAL.Postgres
{
public class PostgresContextFactory : IDesignTimeDbContextFactory<VaultGuardDbContextApp>
    {
    public VaultGuardDbContextApp CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            var connectionString = config.GetConnectionString("PostgresConnection");
        var optionsBuilder = new DbContextOptionsBuilder<VaultGuardDbContextApp>();
            optionsBuilder.UseNpgsql(connectionString);
        return new VaultGuardDbContextApp(optionsBuilder.Options);
        }
    }
}
