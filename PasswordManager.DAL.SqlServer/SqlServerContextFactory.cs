using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using PasswordManager.DAL;

namespace PasswordManager.DAL.SqlServer
{
public class SqlServerContextFactory : IDesignTimeDbContextFactory<PasswordManagerDbContextApp>
    {
    public PasswordManagerDbContextApp CreateDbContext(string[] args)
        {
            var config = new  bConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            var connectionString = config.GetConnectionString("SqlServerConnection");
        var optionsBuilder = new DbContextOptionsBuilder<PasswordManagerDbContextApp>();
            optionsBuilder.UseSqlServer(connectionString);
        return new PasswordManagerDbContextApp(optionsBuilder.Options);
        }
    }
}
