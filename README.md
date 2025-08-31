# Corona Deployments 🚀

A comprehensive deployment automation platform built with .NET 8, providing secure CI/CD capabilities for .NET applications with Git and SVN repository support.

*This project was inspired by the creator's journey toward recovery from Corona-19 virus, making versioned deployments behind IIS easy and secure!*

## ⚡ Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (LTS)
- PostgreSQL 12+ 
- Redis 6+ (optional, for session caching)
- Git (for Git repository support)
- SVN client (for SVN repository support)

### Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/sherifr212/corona-deployments.git
   cd corona-deployments
   ```

2. **Configure environment variables** (Recommended for security)
   ```bash
   # Required: Base directory for repository operations
   export CORONA_BASE_DIRECTORY="/var/repository"
   
   # Optional: Repository credentials (use environment variables instead of config files)
   export GIT_USERNAME="your-git-username"
   export GIT_PASSWORD="your-git-token"
   export SVN_USERNAME="your-svn-username" 
   export SVN_PASSWORD="your-svn-password"
   ```

3. **Update connection strings** in `appsettings.json`
   ```json
   {
     "ConnectionStrings": {
       "Postgres": "ApplicationName=corona_deployments;Database=corona_deployments;Server=localhost;Port=5432;User Id=postgres;Password=yourpassword;",
       "Redis": "localhost"
     }
   }
   ```

4. **Build and run**
   ```bash
   cd Source/CoronaDeployments
   dotnet restore
   dotnet build
   dotnet run
   ```

## 🎥 Demo Videos (Release 1)

### Part 1: Basic Setup and Configuration
[![Part 1](https://img.youtube.com/vi/janRNXjJ20g/0.jpg)](https://www.youtube.com/watch?v=janRNXjJ20g)

### Part 2: Deployment Workflow
[![Part 2](https://img.youtube.com/vi/zgRTFhm_7po/0.jpg)](https://www.youtube.com/watch?v=zgRTFhm_7po)

## 🔒 Security Features

### Recent Security Enhancements (2025)

✅ **Command Injection Protection**
- Secure shell execution with input validation
- Executable whitelist (git, svn, dotnet, msbuild, nuget only)
- No more dangerous `cmd.exe /C` patterns

✅ **Credentials Security**
- Environment variable-based credential management
- No plain text passwords in configuration files
- Automatic configuration validation

✅ **Input Validation**
- Path traversal protection
- Dangerous character filtering
- System directory access prevention

### Security Configuration

**Environment Variables** (Recommended)
```bash
# Repository credentials
export GIT_USERNAME="your-username"
export GIT_PASSWORD="your-personal-access-token"
export SVN_USERNAME="your-username"
export SVN_PASSWORD="your-password"

# Application settings
export CORONA_BASE_DIRECTORY="/secure/repository/path"
```

**Configuration Validation**
The application now validates all configuration on startup:
- Path security checks
- Credential format validation  
- Connection string security analysis
- Directory access verification

## 🏗️ Architecture

### Project Structure
```
Source/
├── CoronaDeployments/          # Main web application (.NET 8)
├── CoronaDeployments.Core/     # Business logic & services (.NET 8)  
└── CoronaDeployments.Test/     # Test suite (.NET 8)
```

### Key Components

- **Repository Management**: Git & SVN integration with LibGit2Sharp and SharpSvn
- **Build System**: .NET Core project building with MSBuild
- **Deployment**: IIS deployment automation
- **Security**: Input validation, credential management, audit logging
- **Database**: PostgreSQL with Marten document DB
- **Caching**: Redis for session management

## 🔧 Configuration

### Application Settings

**appsettings.json** (Development)
```json
{
  "ConnectionStrings": {
    "Postgres": "ApplicationName=corona_deployments;Database=corona_deployments;Server=localhost;Port=5432;User Id=postgres;",
    "Redis": "localhost"
  },
  "AppConfiguration": {
    "BaseDirectory": "/var/repository"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

**appsettings.Production.json** (Production)
```json
{
  "ConnectionStrings": {
    "Postgres": "ApplicationName=corona_deployments;Database=corona_deployments;Server=prod-db;Port=5432;User Id=postgres;",
    "Redis": "prod-redis:6379"
  },
  "AppConfiguration": {
    "BaseDirectory": "/var/repository"
  },
  "Security": {
    "Note": "Credentials should be provided via environment variables or Azure Key Vault for security",
    "EnvironmentVariables": {
      "Git": "Set GIT_USERNAME and GIT_PASSWORD environment variables",
      "Svn": "Set SVN_USERNAME and SVN_PASSWORD environment variables"
    }
  }
}
```

### Environment Configuration

**Cross-Platform Paths**
- Windows: `%USERPROFILE%\repository`
- Linux/macOS: `~/repository`
- Custom: Set `CORONA_BASE_DIRECTORY` environment variable

## 🧪 Testing

### Run Tests
```bash
cd Source/CoronaDeployments
dotnet test
```

### Test Structure
- **Unit Tests**: Core business logic testing
- **Integration Tests**: Repository and build system testing
- **Security Tests**: Input validation and security feature testing

**Note**: Some tests require network access and valid repository credentials. Use test doubles or mocks in CI/CD environments.

## 📦 Deployment

### Production Deployment

1. **Build for Production**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Set Environment Variables**
   ```bash
   export ASPNETCORE_ENVIRONMENT=Production
   export CORONA_BASE_DIRECTORY="/var/repository"
   export GIT_USERNAME="production-user"
   export GIT_PASSWORD="production-token"
   ```

3. **Database Setup**
   ```sql
   CREATE DATABASE corona_deployments;
   CREATE USER corona_app WITH PASSWORD 'secure_password';
   GRANT ALL PRIVILEGES ON DATABASE corona_deployments TO corona_app;
   ```

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY ./publish .

# Create repository directory
RUN mkdir -p /var/repository && chmod 755 /var/repository

ENV CORONA_BASE_DIRECTORY=/var/repository
EXPOSE 80
ENTRYPOINT ["dotnet", "CoronaDeployments.dll"]
```

## 🚨 Security Recommendations

### Production Security Checklist

- [ ] Use environment variables for all credentials
- [ ] Enable HTTPS with valid certificates
- [ ] Set up proper firewall rules
- [ ] Use least-privilege database accounts
- [ ] Enable audit logging
- [ ] Regular security updates
- [ ] Monitor for suspicious activities
- [ ] Backup encryption keys securely

### Credential Management

**❌ Never do this:**
```json
{
  "GitAuthInfo": {
    "Username": "admin",
    "Password": "password123"
  }
}
```

**✅ Always do this:**
```bash
export GIT_USERNAME="admin"
export GIT_PASSWORD="ghp_secure_token_here"
```

## 🐛 Troubleshooting

### Common Issues

**Build Errors**
```bash
# Clear and restore packages
dotnet clean
dotnet restore
dotnet build
```

**Repository Access Issues**
- Verify credentials in environment variables
- Check network connectivity to repository
- Ensure repository URLs are accessible

**Database Connection Issues**
- Verify PostgreSQL is running
- Check connection string format
- Ensure database exists and user has permissions

### Logging

Logs are written to:
- Console (development)
- File: `Logs/corona-deployments_log_YYYY-MM-DD.txt`
- Structured logging with Serilog

## 📈 Recent Improvements (2025)

### Framework Modernization
- ✅ Upgraded from .NET Core 3.1 → .NET 8 LTS
- ✅ Updated all NuGet packages to latest stable versions
- ✅ Improved cross-platform compatibility

### Security Enhancements
- ✅ Fixed critical command injection vulnerability
- ✅ Implemented secure credential management
- ✅ Added comprehensive input validation
- ✅ Cross-platform path handling

### Code Quality
- ✅ Fixed broken test suite compilation
- ✅ Improved async/await patterns
- ✅ Added configuration validation
- ✅ Enhanced error handling

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit your changes: `git commit -m 'Add amazing feature'`
4. Push to the branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

### Development Guidelines
- Follow existing code style and patterns
- Add tests for new features
- Update documentation for API changes
- Ensure security best practices

## 💬 Feedback

All feedback and requests are more than welcome at this stage! Please use:
- 🐛 **Issues**: [GitHub Issues](https://github.com/sherifr212/corona-deployments/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/sherifr212/corona-deployments/discussions)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**⚠️ Security Notice**: This application handles sensitive credentials and repository access. Always follow security best practices and keep the application updated.
