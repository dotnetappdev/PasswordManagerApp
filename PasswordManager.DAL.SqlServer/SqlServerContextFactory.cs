using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PasswordManager.Models;
using System.IO;

namespace PasswordManager.DAL.SqlServer
{
    public class SqlServerContextFactory : IDesignTimeDbContextFactory<PasswordManagerDbContext>
    {
        public PasswordManagerDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            var connectionString = config.GetConnectionString("SqlServerConnection");
            var optionsBuilder = new DbContextOptionsBuilder<PasswordManagerDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new PasswordManagerDbContext(optionsBuilder.Options);
        }
    }
}
