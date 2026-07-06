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

            // This design-time factory only builds a SQLite context (the DAL project references the SQLite
            // provider only). To apply migrations against a SQL Server / Postgres database — including one
            // whose credentials come from Google Secret Manager — run the API instead, which uses the full
            // runtime configuration pipeline:  dotnet run --project VaultGuard.API -- --migrate
            // A SQLite connection can still be overridden here via the ConnectionStrings__SqliteConnection
            // environment variable; otherwise the standard LocalAppData path is used.
            var conn = Environment.GetEnvironmentVariable("ConnectionStrings__SqliteConnection");
            if (string.IsNullOrWhiteSpace(conn))
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dbPath = Path.Combine(appDataPath, "VaultGuard", "data", "passwordmanager.db");
                var dbDirectory = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory!);
                }
                conn = $"Data Source={dbPath}";
            }

            optionsBuilder.UseSqlite(conn);
            return new VaultGuardDbContextApp(optionsBuilder.Options);
        }
    }
}
