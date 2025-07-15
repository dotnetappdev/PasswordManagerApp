using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PasswordManager.DAL.SupaBase
{
    public class SupabaseDbContextFactory : IDesignTimeDbContextFactory<SupabaseDbContext>
    {
        public SupabaseDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var url = config["Supabase:Url"];
            var apiKey = config["Supabase:ApiKey"];
            return new SupabaseDbContext(url, apiKey);
        }
    }
}
