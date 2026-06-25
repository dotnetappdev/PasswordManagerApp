# VaultGuard.DAL.SupaBase

# EF Core Migrations for Supabase (Postgres)

## Prerequisites
- .NET 9 SDK
- [Npgsql.EntityFrameworkCore.PostgreSQL](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/) package installed in VaultGuard.DAL.SupaBase
- Supabase Postgres connection string in `appsettings.json` (or `appsettings.Supabase.json`)

## Example Connection String

```
"ConnectionStrings": {
  "PostgresConnection": "Host=your-project.supabase.co;Database=postgres;Username=postgres;Password=your-db-password;Port=5432"
}
```

## Add a Migration

```
dotnet ef migrations add InitialCreate --project VaultGuard.DAL.SupaBase --startup-project VaultGuard.API --context SupabaseDbContext
```

## Apply Migrations to Supabase

```
dotnet ef database update --project VaultGuard.DAL.SupaBase --startup-project VaultGuard.API --context SupabaseDbContext
```

## Notes
- Ensure your Supabase Postgres instance is accessible from your environment.
- You can use the same workflow as for SQL Server/MySQL, just with the Supabase/Postgres connection string and context.
- For more info: https://supabase.com/docs/guides/database
