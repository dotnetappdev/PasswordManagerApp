using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PasswordManager.Models;

namespace PasswordManager.DAL.Postgres
{
    public class PostgresContextFactory : IDesignTimeDbContextFactory<PasswordManagerDbContext>
    {
        public PasswordManagerDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            var connectionString = config.GetConnectionString("PostgresConnection");
            var optionsBuilder = new DbContextOptionsBuilder<PasswordManagerDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new PasswordManagerDbContext(optionsBuilder.Options);
        }
    }
}
