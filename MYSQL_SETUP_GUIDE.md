# MySQL Database Setup Guide

## Overview
The Password Manager API now supports MySQL as a database provider alongside SQL Server, PostgreSQL, and SQLite. This guide explains how to configure and use MySQL with the application.

## Prerequisites
- MySQL Server 8.0 or higher
- MySQL client/workbench for database management

## Configuration

### 1. Install MySQL Server
Follow the official MySQL installation guide for your operating system:
- **Windows**: Download from https://dev.mysql.com/downloads/installer/
- **Linux**: `sudo apt-get install mysql-server` (Ubuntu/Debian)
- **macOS**: `brew install mysql`

### 2. Create Database and User
Connect to MySQL as root and run the following commands:

```sql
-- Create database
CREATE DATABASE PasswordManagerDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Create user for the application
CREATE USER 'passwordmgr'@'localhost' IDENTIFIED BY 'your_secure_password';

-- Grant privileges
GRANT ALL PRIVILEGES ON PasswordManagerDB.* TO 'passwordmgr'@'localhost';
FLUSH PRIVILEGES;
```

### 3. Update Configuration Files

#### appsettings.json
```json
{
  "DatabaseProvider": "mysql",
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=PasswordManagerDB;User=passwordmgr;Password=your_secure_password;Port=3306;"
  }
}
```

#### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=PasswordManagerDB_Dev;User=passwordmgr;Password=dev_password;Port=3306;"
  }
}
```

## Connection String Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| Server | MySQL server hostname/IP | `localhost`, `192.168.1.100` |
| Database | Database name | `PasswordManagerDB` |
| User | MySQL username | `passwordmgr` |
| Password | User password | `your_secure_password` |
| Port | MySQL port (default: 3306) | `3306` |
| SslMode | SSL connection mode | `Required`, `Preferred`, `None` |
| CharSet | Character set | `utf8mb4` |

### Advanced Connection String Options
```
Server=localhost;Database=PasswordManagerDB;User=passwordmgr;Password=your_password;Port=3306;SslMode=Required;CharSet=utf8mb4;AllowUserVariables=true;UseAffectedRows=false;
```

## Database Migration

### Initial Setup
```bash
# Navigate to API project
cd PasswordManager.API

# Add migration for MySQL
dotnet ef migrations add InitialCreate --context PasswordManagerDbContext

# Update database
dotnet ef database update
```

### Provider-Specific Migrations
If you need separate migrations for different providers:

```bash
# MySQL-specific migration
dotnet ef migrations add MySqlInitial --context PasswordManagerDbContext --output-dir Migrations/MySQL

# Apply migration
dotnet ef database update --context PasswordManagerDbContext
```

## Performance Optimization

### MySQL Configuration
Add these settings to your MySQL configuration file (`my.cnf` or `my.ini`):

```ini
[mysqld]
# Character set and collation
character-set-server = utf8mb4
collation-server = utf8mb4_unicode_ci

# Performance settings
innodb_buffer_pool_size = 256M
innodb_log_file_size = 64M
max_connections = 200

# Security settings
sql_mode = STRICT_TRANS_TABLES,NO_ZERO_DATE,NO_ZERO_IN_DATE,ERROR_FOR_DIVISION_BY_ZERO
```

### Application-Level Optimizations
The application automatically optimizes queries for MySQL using Entity Framework Core's MySQL provider features.

## Supported Features

✅ **Full Encryption Support**: All password encryption with 600,000 PBKDF2 iterations  
✅ **User Authentication**: Identity management with MySQL backend  
✅ **Database Sync**: Multi-database synchronization including MySQL  
✅ **Migrations**: Automatic schema updates  
✅ **Transactions**: ACID compliance for data integrity  
✅ **Connection Pooling**: Built-in connection management  

## Troubleshooting

### Common Issues

1. **Connection Timeout**
   ```
   Solution: Increase connection timeout in connection string:
   Server=localhost;Database=PasswordManagerDB;User=passwordmgr;Password=password;Port=3306;Connection Timeout=30;
   ```

2. **Character Encoding Issues**
   ```
   Solution: Ensure UTF-8 encoding:
   Server=localhost;Database=PasswordManagerDB;User=passwordmgr;Password=password;Port=3306;CharSet=utf8mb4;
   ```

3. **SSL Connection Errors**
   ```
   Solution: Adjust SSL mode:
   Server=localhost;Database=PasswordManagerDB;User=passwordmgr;Password=password;Port=3306;SslMode=None;
   ```

### Logging
Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

## Security Considerations

1. **Use Strong Passwords**: Ensure MySQL user has a strong password
2. **Network Security**: Use SSL/TLS for production environments
3. **User Privileges**: Grant only necessary privileges to application user
4. **Regular Updates**: Keep MySQL server updated
5. **Backup Strategy**: Implement regular database backups

## Example Docker Setup

```yaml
version: '3.8'
services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: rootpassword
      MYSQL_DATABASE: PasswordManagerDB
      MYSQL_USER: passwordmgr
      MYSQL_PASSWORD: password
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

volumes:
  mysql_data:
```

## Migration from Other Providers

To migrate from SQLite/SQL Server/PostgreSQL to MySQL:

1. Export existing data using the sync feature
2. Set up MySQL database
3. Update configuration to use MySQL
4. Run migrations
5. Import data using sync feature

This ensures all encrypted data remains intact during the migration process.
