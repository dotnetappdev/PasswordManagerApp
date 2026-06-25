using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using VaultGuard.DAL;

namespace VaultGuard.DAL.SqlServer
{
public class SqlServerContextFactory : IDesignTimeDbContextFactory<VaultGuardDbContextApp>
    {
    public VaultGuardDbContextApp CreateDbContext(string[] args)
        {
            var config = new  ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            var connectionString = config.GetConnectionString("SqlServerConnection");
        var optionsBuilder = new DbContextOptionsBuilder<VaultGuardDbContextApp>();
            optionsBuilder.UseSqlServer(connectionString);
        return new VaultGuardDbContextApp(optionsBuilder.Options);
        }
    }
}
