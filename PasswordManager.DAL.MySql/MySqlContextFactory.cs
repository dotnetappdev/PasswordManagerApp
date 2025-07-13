using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PasswordManager.Models;

namespace PasswordManager.DAL.MySql
{
    public class MySqlContextFactory : IDesignTimeDbContextFactory<PasswordManagerDbContext>
    {
        public PasswordManagerDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            var connectionString = config.GetConnectionString("MySqlConnection");
            var optionsBuilder = new DbContextOptionsBuilder<PasswordManagerDbContext>();
            // Use Pomelo.EntityFrameworkCore.MySql (ensure correct version)
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            return new PasswordManagerDbContext(optionsBuilder.Options);
        }
    }
}
