using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using PasswordManager.DAL;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace PasswordManager.DAL.MySql
{
    public class MySqlContextFactory : IDesignTimeDbContextFactory<PasswordManagerDbContextApp>
    {
        public PasswordManagerDbContextApp CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            var connectionString = config.GetConnectionString("MySqlConnection");
            var optionsBuilder = new DbContextOptionsBuilder<PasswordManagerDbContextApp>();
            // Use Pomelo.EntityFrameworkCore.MySql (ensure correct version)
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            return new PasswordManagerDbContextApp(optionsBuilder.Options);
        }
    }
}
