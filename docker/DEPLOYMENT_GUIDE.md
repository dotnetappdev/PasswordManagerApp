# Password Manager - Complete Deployment Guide

This guide provides an overview of all deployment options for the Password Manager application.

## 📋 Deployment Options Overview

| Method | Best For | Complexity | Documentation |
|--------|----------|------------|---------------|
| **Docker** | Production servers, Cloud deployment | Low | [Docker Setup Guide](README.md) |
| **Windows Installer** | Enterprise deployment, End users | Low | [Installer Guide](../installers/README.md) |
| **MSIX Package** | Microsoft Store, Modern Windows | Medium | [MSIX Guide](../installers/MSIX_PACKAGING_GUIDE.md) |
| **Manual Setup** | Development, Custom configuration | High | [Setup Guide](../SETUP.md) |

## 🐳 Docker Deployment (Recommended for Production)

### Quick Start

```bash
# 1. Generate HTTPS certificate
cd docker
./setup-https-cert.sh  # Linux/Mac
# OR
setup-https-cert.bat   # Windows

# 2. Configure environment (optional)
cp .env.example .env
# Edit .env with your settings

# 3. Start containers
docker-compose up -d

# 4. Access the application
# API: https://localhost:51650
# Health: http://localhost:51651/health
```

### What's Included

- ✅ **Web API** - ASP.NET Core API with HTTPS support
- ✅ **SQL Server 2022** - Developer Edition with persistent storage
- ✅ **Automated Setup** - Database migrations run automatically
- ✅ **Health Checks** - Container health monitoring
- ✅ **Logging** - Persistent log volumes
- ✅ **Security** - HTTPS with developer certificates

### Best For

- Production deployments
- Cloud hosting (AWS, Azure, GCP)
- Development teams
- Continuous integration
- Scalable deployments

**Full Documentation:** [docker/README.md](README.md)

---

## 💿 Windows Installer Deployment

### Web API Installer

Professional installer with interactive database configuration wizard.

**Features:**
- ✅ Windows Service installation
- ✅ Database configuration wizard (SQL Server, PostgreSQL, MySQL, SQLite)
- ✅ Automatic firewall rules
- ✅ HTTPS certificate generation
- ✅ Updates appsettings.json automatically

**Quick Start:**
```cmd
# Build the installer
cd installers
build-installers.bat

# Run the installer
output\PasswordManager-API-Setup-1.0.0.exe
```

### WinUI Application Installer

Desktop application installer with API configuration.

**Features:**
- ✅ API connection setup wizard
- ✅ Local database option
- ✅ Desktop shortcuts
- ✅ Auto-start with Windows
- ✅ Import plugin support

**Quick Start:**
```cmd
# Build the installer
cd installers
build-installers.bat

# Run the installer
output\PasswordManager-WinUI-Setup-1.0.0.exe
```

### Best For

- Enterprise deployment
- End-user installations
- IT departments
- Corporate environments
- Offline installations

**Full Documentation:** [installers/README.md](../installers/README.md)

---

## 📦 MSIX Package Deployment

Modern Windows app package format for Microsoft Store and enterprise distribution.

**Features:**
- ✅ Clean installation/uninstallation
- ✅ Automatic updates
- ✅ Sandboxed execution
- ✅ Microsoft Store compatible
- ✅ Enterprise deployment ready

**Quick Start:**
```cmd
# Build MSIX package
cd PasswordManager.WinUi
dotnet publish -c Release -f net9.0-windows10.0.19041.0 -p:Platform=x64 -p:GenerateAppxPackageOnBuild=true

# Package location
# bin\x64\Release\net9.0-windows10.0.19041.0\x64\AppPackages\
```

### Best For

- Microsoft Store distribution
- Modern Windows environments
- Automatic updates
- Enterprise deployment
- Sideloading scenarios

**Full Documentation:** [installers/MSIX_PACKAGING_GUIDE.md](../installers/MSIX_PACKAGING_GUIDE.md)

---

## 🛠️ Manual Setup (Development)

For developers who want full control over the setup process.

### Prerequisites

- .NET 9 SDK
- Database server (SQL Server, PostgreSQL, MySQL, or SQLite)
- Visual Studio 2024 or VS Code

### Quick Start

```bash
# 1. Clone repository
git clone https://github.com/dotnetappdev/PasswordManagerApp.git
cd PasswordManagerApp

# 2. Restore packages
dotnet restore

# 3. Configure database
# Edit PasswordManager.API/appsettings.json

# 4. Run migrations
cd PasswordManager.API
dotnet ef database update

# 5. Start API
dotnet run

# 6. Start WinUI app (separate terminal)
cd ../PasswordManager.WinUi
dotnet run
```

### Best For

- Development
- Custom configurations
- Learning the codebase
- Contributing
- Testing

**Full Documentation:** [SETUP.md](../SETUP.md)

---

## 🌐 Deployment Scenarios

### Scenario 1: Single Server Deployment

**Use Docker:**

```yaml
# docker-compose.yml
services:
  sqlserver:
    # SQL Server container
  api:
    # API container
    # Exposed on ports 51650 (HTTPS) and 51651 (HTTP)
```

**Access:**
- API: `https://your-server:51650`
- Configure WinUI apps to point to this URL

### Scenario 2: Enterprise Deployment

**Step 1: Deploy API**
- Use Docker on a server or
- Use Windows Installer with SQL Server

**Step 2: Distribute Client Apps**
- Use Windows Installer for WinUI app
- Or use MSIX for Microsoft Store
- Configure API URL during installation

### Scenario 3: Cloud Deployment

**AWS/Azure/GCP:**

```bash
# 1. Deploy Docker containers
docker-compose up -d

# 2. Configure load balancer
# Point to API container port

# 3. Set up SSL certificate
# Use Let's Encrypt or cloud provider certificates

# 4. Configure DNS
# api.yourdomain.com → Load balancer
```

### Scenario 4: Development Team

**Each developer:**

```bash
# Option 1: Docker (recommended)
cd docker
docker-compose up -d

# Option 2: Manual setup
dotnet run --project PasswordManager.API
dotnet run --project PasswordManager.WinUi
```

---

## 🔒 Security Considerations

### Production Deployment Checklist

- [ ] **Use strong passwords** - Never use default passwords
- [ ] **Enable HTTPS** - Always use HTTPS in production
- [ ] **Configure SSL certificates** - Use certificates from trusted CA
- [ ] **Secure database** - Don't expose database ports publicly
- [ ] **Use environment variables** - For sensitive configuration
- [ ] **Enable firewall** - Restrict access to necessary ports only
- [ ] **Regular updates** - Keep all components up to date
- [ ] **Monitor logs** - Set up log monitoring and alerting
- [ ] **Backup database** - Regular automated backups
- [ ] **Test disaster recovery** - Verify backup restoration

### Network Security

**Firewall Rules:**
```cmd
# Allow HTTPS
netsh advfirewall firewall add rule name="Password Manager HTTPS" dir=in action=allow protocol=TCP localport=51650

# Allow HTTP (optional, for health checks)
netsh advfirewall firewall add rule name="Password Manager HTTP" dir=in action=allow protocol=TCP localport=51651
```

**Reverse Proxy (nginx example):**
```nginx
server {
    listen 443 ssl;
    server_name api.yourdomain.com;
    
    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;
    
    location / {
        proxy_pass https://localhost:51650;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

---

## 📊 Comparison Matrix

| Feature | Docker | Windows Installer | MSIX | Manual |
|---------|--------|-------------------|------|--------|
| **Ease of Setup** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| **Production Ready** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ |
| **Scalability** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| **Portability** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ |
| **Updates** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ |
| **Customization** | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Enterprise Support** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ |

---

## 🚀 Quick Decision Guide

**Choose Docker if:**
- You're deploying to a server or cloud
- You want easy scalability
- You prefer containerized applications
- You need consistent environments

**Choose Windows Installer if:**
- You're deploying to Windows servers
- You need enterprise deployment tools
- You want a traditional installation experience
- You need Windows service integration

**Choose MSIX if:**
- You're targeting Microsoft Store
- You want modern Windows app packaging
- You need automatic updates
- You're in a modern Windows environment

**Choose Manual Setup if:**
- You're developing the application
- You need custom configuration
- You want full control
- You're learning the codebase

---

## 📚 Additional Resources

### Documentation
- [Main README](../README.md)
- [Setup Guide](../SETUP.md)
- [Getting Started](../GETTING_STARTED.md)
- [Development Guide](../DEVELOPMENT.md)

### Component-Specific
- [Web API Documentation](../PasswordManager.API/README.md)
- [WinUI Documentation](../PasswordManager.WinUi/README.md)
- [Database Providers](../ReadMe.DatabaseProviders.md)

### Guides
- [MySQL Setup](../MYSQL_SETUP_GUIDE.md)
- [Entity Framework Identity](../EF_IDENTITY_SETUP_GUIDE.md)
- [Encryption Implementation](../ENCRYPTION_IMPLEMENTATION.md)

---

## 🆘 Support

### Getting Help

1. **Check Documentation** - Review relevant guides above
2. **Search Issues** - [GitHub Issues](https://github.com/dotnetappdev/PasswordManagerApp/issues)
3. **Ask Questions** - Create a new issue with details

### Reporting Issues

When creating an issue, include:
- Deployment method used
- Operating system and version
- Error messages and logs
- Steps to reproduce
- Expected vs actual behavior

### Contributing

See [DEVELOPMENT.md](../DEVELOPMENT.md) for contribution guidelines.

---

**Note:** For production deployments, always use strong passwords, enable HTTPS, and follow security best practices. The Docker deployment is recommended for most production scenarios due to its ease of use, scalability, and maintainability.
