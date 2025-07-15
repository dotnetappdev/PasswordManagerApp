using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.DAL.SupaBase;

namespace PasswordManager.API.Extensions
{
    public static class SupabaseServiceCollectionExtensions
    {
        public static IServiceCollection AddSupabaseDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var url = configuration["Supabase:Url"];
            var apiKey = configuration["Supabase:ApiKey"];
            services.AddSingleton(new SupabaseDbContext(url, apiKey));
            return services;
        }
    }
}
