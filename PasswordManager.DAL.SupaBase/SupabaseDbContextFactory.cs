using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PasswordManager.DAL.SupaBase;

public class SupabaseDbContextFactory : IDesignTimeDbContextFactory<SupabaseDbContext>
{
    public SupabaseDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("PostgresConnection") ?? config["Supabase:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Supabase/Postgres connection string not found in appsettings.json");

        var optionsBuilder = new DbContextOptionsBuilder<SupabaseDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new SupabaseDbContext(optionsBuilder.Options);
    }
}
