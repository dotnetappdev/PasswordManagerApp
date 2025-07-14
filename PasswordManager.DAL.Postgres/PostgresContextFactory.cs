using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PasswordManager.Models;

namespace PasswordManager.DAL.Postgres
{
    public class PostgresContextFactory : IDesignTimeDbContextFactory<PasswordManagerDbContextApp>
    {
        public PasswordManagerDbContextApp CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            var connectionString = config.GetConnectionString("PostgresConnection");
            var optionsBuilder = new DbContextOptionsBuilder<PasswordManagerDbContextApp>();
            optionsBuilder.UseNpgsql(connectionString);
            return new PasswordManagerDbContextApp(optionsBuilder.Options);
        }
    }
}
