using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.DAL.SupaBase;

namespace VaultGuard.API.Extensions
{
    public static class SupabaseServiceCollectionExtensions
    {
        public static IServiceCollection AddSupabaseDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("PostgresConnection") ?? configuration["Supabase:ConnectionString"];
            
            services.AddDbContext<SupabaseDbContext>(options =>
                options.UseNpgsql(connectionString));
            
            return services;
        }
    }
}
