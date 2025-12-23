# Password Manager - Docker Setup Guide

This directory contains Docker configuration files for running the Password Manager Web API and SQL Server in containers.

## 📋 Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [HTTPS Certificate Setup](#https-certificate-setup)
- [Container Management](#container-management)
- [Database Management](#database-management)
- [Troubleshooting](#troubleshooting)
- [Advanced Configuration](#advanced-configuration)

## 🔧 Prerequisites

Before you begin, ensure you have the following installed:

- **Docker Desktop** (Windows/Mac) or **Docker Engine** (Linux)
  - Windows: [Download Docker Desktop](https://www.docker.com/products/docker-desktop/)
  - Mac: [Download Docker Desktop](https://www.docker.com/products/docker-desktop/)
  - Linux: [Install Docker Engine](https://docs.docker.com/engine/install/)
- **Docker Compose** (included with Docker Desktop)
- **.NET 9 SDK** (for certificate generation)
  - [Download .NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Verify Installation

```bash
# Check Docker version
docker --version
# Should show: Docker version 20.10.x or higher

# Check Docker Compose version
docker-compose --version
# Should show: Docker Compose version 2.x.x or higher

# Check .NET SDK version
dotnet --version
# Should show: 9.0.x or higher
```

## 🚀 Quick Start

### 1. Generate HTTPS Certificate

Before starting the containers, you need to generate an HTTPS development certificate.

**On Windows:**
```cmd
cd docker
setup-https-cert.bat
```

**On Linux/Mac:**
```bash
cd docker
chmod +x setup-https-cert.sh
./setup-https-cert.sh
```

This will create a self-signed certificate in the `certs` directory.

### 2. Configure Environment Variables (Optional)

Copy the example environment file and customize it:

```bash
cp .env.example .env
```

Edit `.env` file to set your own passwords and configuration:

```env
# SQL Server SA password (must be strong)
SQL_SA_PASSWORD=YourStrong@Password123

# HTTPS Certificate password (leave empty if using dev cert without password)
CERT_PASSWORD=

# Database name
DATABASE_NAME=PasswordManagerDB
```

### 3. Start the Containers

```bash
# Start all services in detached mode
docker-compose up -d

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f api
docker-compose logs -f sqlserver
```

### 4. Access the Application

Once the containers are running:

- **Web API (HTTPS):** https://localhost:51650
- **Web API (HTTP):** http://localhost:51651
- **API Documentation:** https://localhost:51650/swagger
- **Health Check:** http://localhost:51651/health
- **SQL Server:** localhost:1433

## ⚙️ Configuration

### Environment Variables

The following environment variables can be configured in your `.env` file:

| Variable | Default | Description |
|----------|---------|-------------|
| `SQL_SA_PASSWORD` | `YourStrong@Password123` | SQL Server SA account password (must meet complexity requirements) |
| `CERT_PASSWORD` | _(empty)_ | HTTPS certificate password (leave empty for dev cert) |
| `DATABASE_NAME` | `PasswordManagerDB` | Database name |
| `API_HTTPS_PORT` | `51650` | HTTPS port for the API |
| `API_HTTP_PORT` | `51651` | HTTP port for the API |
| `SQL_SERVER_PORT` | `1433` | SQL Server port |

### SQL Server Password Requirements

The SQL Server password must meet the following requirements:
- At least 8 characters long
- Contains uppercase letters (A-Z)
- Contains lowercase letters (a-z)
- Contains digits (0-9)
- Contains special characters (!@#$%^&*)

## 🔒 HTTPS Certificate Setup

### Using Development Certificates

The provided setup scripts create a development certificate using the .NET SDK:

```bash
# Linux/Mac
./setup-https-cert.sh

# Windows
setup-https-cert.bat
```

This creates a certificate at `certs/aspnetapp.pfx` with no password.

### Using Custom Certificates

If you have your own certificate:

1. Place your `.pfx` certificate file in the `certs` directory
2. Rename it to `aspnetapp.pfx` or update the docker-compose.yml
3. Set the `CERT_PASSWORD` in your `.env` file

```bash
# Example with password-protected certificate
CERT_PASSWORD=MySecurePassword123
```

### Trusting the Certificate

To avoid browser warnings on your local machine:

**Windows:**
```cmd
dotnet dev-certs https --trust
```

**Mac:**
```bash
dotnet dev-certs https --trust
```

**Linux:**
```bash
# Linux requires manual certificate import
# The certificate will be at: ~/.dotnet/corefx/cryptography/x509stores/my
# Import it into your browser's certificate store
```

## 🐳 Container Management

### Starting Containers

```bash
# Start all services
docker-compose up -d

# Start specific service
docker-compose up -d api
docker-compose up -d sqlserver
```

### Stopping Containers

```bash
# Stop all services
docker-compose down

# Stop and remove volumes (WARNING: This deletes all data)
docker-compose down -v
```

### Viewing Logs

```bash
# View all logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f api
docker-compose logs -f sqlserver

# View last 100 lines
docker-compose logs --tail=100 api
```

### Restarting Containers

```bash
# Restart all services
docker-compose restart

# Restart specific service
docker-compose restart api
docker-compose restart sqlserver
```

### Rebuilding Containers

```bash
# Rebuild and start all services
docker-compose up -d --build

# Rebuild specific service
docker-compose up -d --build api
```

## 🗄️ Database Management

### Connecting to SQL Server

You can connect to the SQL Server instance using any SQL client:

**Connection String:**
```
Server=localhost,1433;Database=PasswordManagerDB;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=true;
```

**Using SQL Server Management Studio (SSMS):**
- Server: `localhost,1433`
- Authentication: SQL Server Authentication
- Login: `sa`
- Password: Your configured password

**Using Azure Data Studio:**
- Connection type: Microsoft SQL Server
- Server: `localhost,1433`
- Authentication type: SQL Login
- User name: `sa`
- Password: Your configured password

### Database Initialization

The API automatically runs Entity Framework migrations on startup. The database will be created if it doesn't exist.

To manually run migrations:

```bash
# Get a shell in the API container
docker-compose exec api bash

# Run migrations
dotnet ef database update --project /app
```

### Backing Up Data

```bash
# Backup SQL Server data
docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Password123" -C \
  -Q "BACKUP DATABASE [PasswordManagerDB] TO DISK = N'/var/opt/mssql/backup/PasswordManagerDB.bak' WITH NOFORMAT, NOINIT, NAME = 'PasswordManagerDB-full', SKIP, NOREWIND, NOUNLOAD, STATS = 10"

# Copy backup file from container to host
docker cp passwordmanager-sqlserver:/var/opt/mssql/backup/PasswordManagerDB.bak ./backup.bak
```

### Restoring Data

```bash
# Copy backup file from host to container
docker cp ./backup.bak passwordmanager-sqlserver:/var/opt/mssql/backup/

# Restore database
docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Password123" -C \
  -Q "RESTORE DATABASE [PasswordManagerDB] FROM DISK = N'/var/opt/mssql/backup/PasswordManagerDB.bak' WITH FILE = 1, NOUNLOAD, REPLACE, STATS = 5"
```

### Resetting the Database

```bash
# Stop containers
docker-compose down

# Remove volumes (this deletes all data)
docker volume rm docker_sqlserver-data

# Start containers again (fresh database)
docker-compose up -d
```

## 🔍 Troubleshooting

### Container Won't Start

**Check logs:**
```bash
docker-compose logs api
docker-compose logs sqlserver
```

**Common issues:**
1. **Port already in use:** Change ports in `.env` or docker-compose.yml
2. **Certificate not found:** Run the certificate setup script
3. **SQL Server password too weak:** Ensure password meets complexity requirements

### Cannot Connect to API

**Check container status:**
```bash
docker-compose ps
```

**Check health:**
```bash
curl http://localhost:51651/health
```

**Verify certificate:**
```bash
ls -la certs/aspnetapp.pfx
```

### Cannot Connect to Database

**Test SQL Server connection from API container:**
```bash
docker-compose exec api bash
apt-get update && apt-get install -y telnet
telnet sqlserver 1433
```

**Check SQL Server logs:**
```bash
docker-compose logs sqlserver | grep -i error
```

### Certificate Errors in Browser

**Trust the certificate:**
```bash
# Windows/Mac
dotnet dev-certs https --trust

# Or add exception in your browser
```

**Verify certificate is mounted:**
```bash
docker-compose exec api ls -la /https/
```

### Database Connection Errors

**Check connection string in API logs:**
```bash
docker-compose logs api | grep -i "connection"
```

**Verify SQL Server is healthy:**
```bash
docker-compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password123" -Q "SELECT @@VERSION" -C
```

### Slow Container Startup

SQL Server can take 30-60 seconds to fully start. The API will wait for SQL Server to be healthy before accepting connections.

**Watch startup progress:**
```bash
docker-compose logs -f sqlserver
```

## 🎯 Advanced Configuration

### Using Different Database Providers

Edit `docker-compose.yml` to use PostgreSQL or MySQL instead:

**PostgreSQL:**
```yaml
postgres:
  image: postgres:16
  environment:
    - POSTGRES_DB=PasswordManagerDB
    - POSTGRES_USER=postgres
    - POSTGRES_PASSWORD=YourPassword123
  ports:
    - "5432:5432"

api:
  environment:
    - DatabaseProvider=Postgres
    - ConnectionStrings__PostgresConnection=Host=postgres;Database=PasswordManagerDB;Username=postgres;Password=YourPassword123
```

**MySQL:**
```yaml
mysql:
  image: mysql:8
  environment:
    - MYSQL_ROOT_PASSWORD=YourPassword123
    - MYSQL_DATABASE=PasswordManagerDB
  ports:
    - "3306:3306"

api:
  environment:
    - DatabaseProvider=MySql
    - ConnectionStrings__MySqlConnection=Server=mysql;Database=PasswordManagerDB;User=root;Password=YourPassword123
```

### Custom API Configuration

Mount a custom `appsettings.json`:

```yaml
api:
  volumes:
    - ./certs:/https:ro
    - api-logs:/app/logs
    - ./custom-appsettings.json:/app/appsettings.json:ro
```

### Production Deployment

For production deployments:

1. **Use strong passwords:** Never use default passwords
2. **Use proper certificates:** Obtain SSL certificates from a trusted CA
3. **Configure secrets:** Use Docker secrets or environment variable encryption
4. **Set up monitoring:** Configure health checks and alerting
5. **Configure backups:** Set up automated database backups
6. **Use private networks:** Don't expose database ports publicly
7. **Update regularly:** Keep Docker images up to date

Example production configuration:

```yaml
api:
  environment:
    - ASPNETCORE_ENVIRONMENT=Production
  deploy:
    replicas: 2
    restart_policy:
      condition: on-failure
      delay: 5s
      max_attempts: 3
  logging:
    driver: "json-file"
    options:
      max-size: "10m"
      max-file: "3"
```

### Scaling the API

```bash
# Run multiple API instances
docker-compose up -d --scale api=3

# Use a load balancer (nginx, traefik, etc.)
```

## 📚 Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [SQL Server on Docker](https://docs.microsoft.com/en-us/sql/linux/sql-server-linux-docker-container-deployment)
- [ASP.NET Core on Docker](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
- [Main Setup Guide](../SETUP.md)

## 🆘 Support

If you encounter issues:

1. Check the [Troubleshooting](#troubleshooting) section
2. Review container logs: `docker-compose logs`
3. Check [GitHub Issues](https://github.com/dotnetappdev/PasswordManagerApp/issues)
4. Create a new issue with:
   - Docker version
   - Docker Compose version
   - Error logs
   - Steps to reproduce

---

**Note:** SQL Server 2022 Developer Edition is used as it's the latest available version from Microsoft. SQL Server 2022 is fully featured and suitable for both development and production use.
