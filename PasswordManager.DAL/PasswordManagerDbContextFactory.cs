using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PasswordManager.DAL
{
    public class PasswordManagerDbContextFactory : IDesignTimeDbContextFactory<PasswordManagerDbContext>
    {
        public PasswordManagerDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PasswordManagerDbContext>();
            // For migrations, we'll use a path in AppData folder to match runtime behavior
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbPath = Path.Combine(appDataPath, "PasswordManager", "data", "passwordmanager.db");
            
            // Ensure the directory exists
            var dbDirectory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory!);
            }
            
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            return new PasswordManagerDbContext(optionsBuilder.Options);
        }
    }
}
