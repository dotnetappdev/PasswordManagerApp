using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.DAL.SupaBase;

namespace PasswordManager.API.Extensions
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
