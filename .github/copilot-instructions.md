# Corona Deployments - Copilot Instructions

Corona Deployments is a .NET Core 3.1 web application for managing versioned deployments behind IIS. It supports Git and SVN repositories, builds .NET Core applications, and deploys them to IIS servers.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- Requires .NET Core 3.1 SDK/Runtime for actual execution (currently EOL)
- Build tooling works with .NET 8.0 SDK (currently installed)
- PostgreSQL database for data persistence (via Marten ORM)
- Redis for caching and session storage
- Git credentials for repository operations
- SVN credentials for repository operations

### Building the Application
**CRITICAL**: All builds complete quickly. NEVER CANCEL build operations.

```bash
# Navigate to main solution
cd Source/CoronaDeployments

# Restore dependencies - takes ~22 seconds, NEVER CANCEL
dotnet restore

# Build core library - takes ~1 second, NEVER CANCEL
dotnet build ../CoronaDeployments.Core/CoronaDeployments.Core.csproj

# Build main web application - takes ~2 seconds, NEVER CANCEL  
dotnet build CoronaDeployments.csproj

# Build test project (will fail due to missing Password.cs)
# This is expected - tests require gitignored credentials file
dotnet build ../CoronaDeployments.Test/CoronaDeployments.Test.csproj
```

### Running Tests
**LIMITATION**: Tests cannot run without creating missing credential files:
- Tests fail due to missing `Source/CoronaDeployments.Test/Password.cs` (gitignored)
- This file should contain `Email` and `Password` static classes with test credentials
- Example structure needed:
  ```csharp
  public static class Email { public static string Value1 = "test@example.com"; public static string Value2 = "test2@example.com"; }
  public static class Password { public static string Value1 = "password1"; public static string Value2 = "password2"; }
  ```

### Running the Application
**CRITICAL LIMITATION**: Cannot run the main application because:
- Application targets .NET Core 3.1 (end-of-life)
- Current environment has .NET 8.0
- .NET Core 3.1 runtime is not available for Ubuntu 24.04

```bash
# This command will fail with runtime not found error
cd Source/CoronaDeployments
dotnet run
```

### Alternative Testing - Demo Projects
Test with the simple demo web application that builds successfully:
```bash
# Build simple test project - takes ~2 seconds, NEVER CANCEL
cd TestProject/TestProject
dotnet build

# This will also fail to run due to .NET Core 3.1 requirement
```

## Validation Scenarios
Since the application cannot run due to .NET Core 3.1 being EOL, validation is limited to:

### Build Validation
1. **Core Library**: `dotnet build Source/CoronaDeployments.Core/CoronaDeployments.Core.csproj` - Should complete in ~1 second
2. **Web Application**: `dotnet build Source/CoronaDeployments/CoronaDeployments.csproj` - Should complete in ~2 seconds  
3. **Demo Project**: `dotnet build TestProject/TestProject/TestProject/TestProject.csproj` - Should complete in ~2 seconds

### Code Structure Validation
- Verify 93 C# files compile without errors (excluding test credential issues)
- Check Views render properly (no compilation errors in Razor views)
- Validate configuration files are properly structured

## Repository Structure

### Key Projects
- **Source/CoronaDeployments**: Main ASP.NET Core web application
- **Source/CoronaDeployments.Core**: Business logic and data access (.NET Standard 2.0)
- **Source/CoronaDeployments.Test**: XUnit test project (requires credential files)
- **TestProject/TestProject**: Simple demo ASP.NET Core application
- **StartingDemo/TestFashion**: Console application demo (requires Settings.cs)

### Configuration Requirements
Application requires configuration in `appsettings.json` or `appsettings.Production.json`:
- PostgreSQL connection string
- Redis connection string
- Git repository credentials (Username/Password)
- SVN repository credentials (Username/Password)
- Base directory for repository operations

### Key Dependencies
- **Marten**: PostgreSQL document database/event store
- **LibGit2Sharp**: Git operations
- **SharpSvn**: SVN operations  
- **Serilog**: Logging
- **Microsoft.Web.Administration**: IIS management

## Common Tasks

### Repository Operations
The application can:
- Clone Git repositories using LibGit2Sharp
- Import SVN repositories using SharpSvn
- Track commit history and branches
- Create "repository cursors" pointing to specific commits

### Build Operations  
The application can:
- Build .NET Core applications using `dotnet publish`
- Target Windows x64 self-contained deployments
- Output built applications to specified directories

### Deployment Operations
The application can:
- Deploy applications to IIS servers
- Manage IIS sites and application pools
- Handle versioned deployments

## Application UI Structure
Based on examination of the Views and Controllers:

### Main Features Available
- **Login System**: Username/password authentication with session management
- **Project Management**: Create and manage deployment projects
- **Build Targets**: Configure build targets for projects (.NET Core applications)
- **Repository Cursors**: Track specific commits for deployment
- **Build & Deploy Requests**: Trigger deployments for specific commits
- **IIS Configuration**: Configure IIS deployment settings (site name, port)

### Key Views
- `Login.cshtml` - User authentication
- `Index.cshtml` - Main dashboard showing projects and build targets
- `CreateProject.cshtml` - Add new projects
- `CreateBuildTarget.cshtml` - Configure build targets
- `CreateRepositoryCursor.cshtml` - Select commits for deployment
- `BuildAndDeployRequest.cshtml` - Monitor deployment progress

### Typical Workflow
1. Login to the system
2. Create a project (specify Git/SVN repository)
3. Add build targets to the project
4. Configure IIS deployment settings
5. Create repository cursors pointing to specific commits
6. Trigger build and deploy requests

## Known Issues and Limitations

### Cannot Run Due to EOL Framework
- **CRITICAL**: Application targets .NET Core 3.1 which is end-of-life
- Modern systems cannot run the application
- Build process works with .NET 8.0 SDK but runtime fails

### Missing Credential Files
- `Source/CoronaDeployments.Test/Password.cs` is gitignored and must be created for tests
- `StartingDemo/TestFashion/Settings.cs` is gitignored and must be created for demo

### Package Compatibility Warnings
- SharpSvn package shows .NET Framework compatibility warnings (expected)
- These warnings do not prevent successful builds

### Windows Dependencies
- Some features like IIS deployment require Windows environment
- Application was designed primarily for Windows hosting scenarios

## Development Workflow

### When Making Changes
1. Always build the core library first: `dotnet build Source/CoronaDeployments.Core/CoronaDeployments.Core.csproj`
2. Then build the main application: `dotnet build Source/CoronaDeployments/CoronaDeployments.csproj`
3. Verify Views compile by checking for Razor compilation errors
4. Check configuration files remain valid JSON

### Checking for Issues
- Look for new compilation errors beyond the expected test/credential issues
- Verify new dependencies are compatible with .NET Core 3.1
- Check that any new features consider the Windows/IIS hosting environment

### Time Expectations
- **Dependency restore**: ~22 seconds (first time), ~1-2 seconds on subsequent runs
- **Core library build**: ~1-2 seconds
- **Main application build**: ~2-4 seconds  
- **Demo project build**: ~2 seconds
- **Test project build**: Will fail due to missing credentials (expected)
- **NEVER CANCEL**: All operations complete quickly, cancellation is never needed

Always ensure build operations complete successfully even if you cannot run the final application due to the runtime limitation.