using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VaultGuard.DAL
{
    public class VaultGuardDbContextFactory : IDesignTimeDbContextFactory<VaultGuardDbContextApp>
    {
        public VaultGuardDbContextApp CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<VaultGuardDbContextApp>();
            // For migrations, we'll use a path in AppData folder to match runtime behavior
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbPath = Path.Combine(appDataPath, "VaultGuard", "data", "passwordmanager.db");
            
            // Ensure the directory exists
            var dbDirectory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory!);
            }
            
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            return new VaultGuardDbContextApp(optionsBuilder.Options);
        }
    }
}
