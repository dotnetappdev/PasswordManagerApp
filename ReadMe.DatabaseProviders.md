# Database Provider Plugin Architecture for PasswordManagerApp

This document describes how to support multiple database providers (MySQL, SQL Server, PostgreSQL, and SQLite) in the PasswordManagerApp using a plugin-like architecture with Entity Framework Core.

## Overview
- The API and App projects will have a configuration option in `appsettings.json` to select the database provider.
- SQLite will always be available for local/offline use.
- Other providers (MySQL, SQL Server, PostgreSQL) are supported via separate context factories and migrations, each in their own class library (DLL).
- The main API loads the correct provider/factory at runtime based on configuration.

## Structure
- `PasswordManager.DAL.SqlServer/` — Contains context factory and migrations for SQL Server
- `PasswordManager.DAL.MySql/` — Contains context factory and migrations for MySQL
- `PasswordManager.DAL.Postgres/` — Contains context factory and migrations for PostgreSQL
- `PasswordManager.DAL.SupaBase/` — Contains context factory and migrations for Supabase (Postgres)
- `PasswordManager.DAL/` — Contains context factory and migrations for SQLite (default)

## Configuration
In both `PasswordManager.API/appsettings.json` and `PasswordManager.App/appsettings.json`:

```json
{
  "DatabaseProvider": "SqlServer", // or "MySql", "Postgres", "Supabase", "Sqlite"
  "ConnectionStrings": {
    "SqlServerConnection": "...",
    "MySqlConnection": "...",
    "PostgresConnection": "...",
    "SqliteConnection": "..."
  },
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "ApiKey": "your-supabase-api-key"
  }
}
```

## Usage
- The API reads `DatabaseProvider` from configuration and loads the corresponding context factory and migrations.
- Each provider DLL is referenced by the API, but only the selected one is used at runtime.
- Migrations are managed per-provider in their respective projects.

## Adding a New Provider
1. Create a new DAL project for the provider (e.g., `PasswordManager.DAL.Oracle`).
2. Implement a context factory and add migrations.
3. Update the API to recognize the new provider in configuration.

## Example
See the main repo for code samples and further details.

---

## Migrations: How To

Each provider has its own migrations. Use the following commands from the root of the repo or the provider's DAL directory:

### SQLite (default)
```
dotnet ef migrations add <MigrationName> --project PasswordManager.DAL --startup-project PasswordManager.API
```

### SQL Server
```
dotnet ef migrations add <MigrationName> --project PasswordManager.DAL.SqlServer --startup-project PasswordManager.API --context PasswordManager.DAL.SqlServer.SqlServerContextFactory
```

### MySQL
```
dotnet ef migrations add <MigrationName> --project PasswordManager.DAL.MySql --startup-project PasswordManager.API --context PasswordManager.DAL.MySql.MySqlContextFactory
```

### PostgreSQL
```
dotnet ef migrations add <MigrationName> --project PasswordManager.DAL.Postgres --startup-project PasswordManager.API --context PasswordManager.DAL.Postgres.PostgresContextFactory
```


### Supabase (Postgres)
```
dotnet ef migrations add <MigrationName> --project PasswordManager.DAL.SupaBase --startup-project PasswordManager.API --context SupabaseDbContext
```

### Applying Migrations
- Migrations are applied automatically at runtime by the API startup code.
- To apply manually, use:
```
dotnet ef database update --project <ProviderProject> --startup-project PasswordManager.API
```
Replace `<ProviderProject>` with the appropriate DAL project (e.g., `PasswordManager.DAL.SqlServer`, `PasswordManager.DAL.SupaBase`).
