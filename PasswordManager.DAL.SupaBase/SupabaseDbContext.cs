using System;
using System.Threading.Tasks;
using Supabase;
using Postgrest;
using PasswordManager.Models;

namespace PasswordManager.DAL.SupaBase
{
    public class SupabaseDbContext
    {
        public Supabase.Client Client { get; }
        public SupabaseDbContext(string url, string apiKey)
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = true
            };
            Client = new Supabase.Client(url, apiKey, options);
            Client.InitializeAsync().Wait();
        }

        // Example: Create a table (Supabase uses PostgREST, so tables are managed via migrations, but you can insert data)
        public async Task InsertPasswordItemAsync(PasswordItem item)
        {
            await Client.From<PasswordItem>().Insert(item);
        }
    }
}
