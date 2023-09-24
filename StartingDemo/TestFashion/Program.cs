using Microsoft.Web.Administration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TestFashion
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // 0. Bootstraping
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var appConfig = await new AppConfigurationProvider().Get();

            var repositoryImportStrategies = new IRepositoryImportStrategy[]
            {
                new SvnRepositoryStrategy()
            };

            var buildStrategies = new ISourceCodeBuilderStrategy[]
            {
                new DotNetCoreSourceBuilderStrategy()
            };

            var deployStrategies = new IDeployStrategy[]
            {
                new InternetInformationServerDeploymentStrategy()
            };

            // 1. Source Repo
            // 1.1 Type (SVN, GIT)
            // 1.2 Central Credentials
            // Moved to settings.

            // 3. Project Name
            // Moved to settings.

            var fetchResult = await RepositoryImporter.ImportAsync(Settings.ProjectName, Settings.SourceRepoUrl, SourceCodeRepositoryType.Svn, appConfig, new SvnAuthInfo(Settings.Username, Settings.Password), 
                new ReadOnlyCollection<IRepositoryImportStrategy>(repositoryImportStrategies));

            if (fetchResult.HasErrors)
                goto end;

            Log.Information(fetchResult.CheckOutDirectory);

            // 4. Targets to build.
            var buildTargets = new[]
            {
                new BuildTarget("Web", @"Trunk\Nrea.Web", BuildTargetType.DotNetCore),
                //new BuildTarget("Migration", @"Trunk\Nrea.Migration", BuildTargetType.DotNetCore)
            };

            var buildResult = await SourceCodeBuilder.BuildTargetsAsync(fetchResult.CheckOutDirectory, buildTargets,
                new ReadOnlyCollection<ISourceCodeBuilderStrategy>(buildStrategies));

            if (buildResult.Any(x => x.HasErrors))
                goto end;

            // 5. Targets to deploy.
            var deployTargets = new[]
            {
                new DeployTarget(buildResult[0], DeployTargetType.IIS, new IISDeployTargetExtraInfo("Nrea", 54007)),
                //new DeployTarget(buildTargets[1], DeployTargetType.DotNetCoreRun, new IISDeployTargetExtraInfo("Nrea"))
            };

            var deployResult = await DeployManager.DeployTargetsAsync(deployTargets, new ReadOnlyCollection<IDeployStrategy>(deployStrategies));
            foreach (var item in deployResult)
            {
                Log.Information($"Deployment result: {item.Target.BuildTarget.Target.Name}, IsError: {item.HasErrors}");
            }

end:
            Log.Information("End.");
            Console.ReadLine();
        }
    }

    public record RepositoryImportResult(string CheckOutDirectory, bool HasErrors);

    public record BuildTarget(string Name, string TargetRelativePath, BuildTargetType Type);

    public record BuildResult(BuildTarget Target, string OutputPath, bool HasErrors);

    public record DeployTarget(BuildResult BuildTarget, DeployTargetType Type, IDeployTargetExtraInfo ExtraInfo);

    public record DeployResult(DeployTarget Target, string Output, bool HasErrors);

    public enum BuildTargetType
    {
        DotNetCore = 1
    }

    public enum DeployTargetType
    {
        IIS = 1,
        DotNetCoreRun = 2
    }

    public enum SourceCodeRepositoryType
    {
        Svn = 1,
        Git = 2
    }

    public interface IDeployTargetExtraInfo
    {
    }

    public record IISDeployTargetExtraInfo(string SiteName, int Port) : IDeployTargetExtraInfo
    {
        public static bool Validate(IISDeployTargetExtraInfo i)
        {
            if (i == default) return false;

            if (string.IsNullOrWhiteSpace(i.SiteName)) return false;

            if (i.Port <= 0) return false;

            return true;
        }
    }

    public class AppConfigurationProvider
    {
        public async Task<AppConfiguration> Get()
        {
            return new AppConfiguration(@"C:\Repository");
        }
    }

    public sealed record AppConfiguration(string BaseDirectory);

    public interface IRepositoryAuthenticationInfo
    {
        Task<bool> Validate();
    }

    public sealed record SvnAuthInfo(string Username, string Password) : IRepositoryAuthenticationInfo
    {
        public async Task<bool> Validate()
        {
            if (string.IsNullOrWhiteSpace(Username)) return false;

            if (string.IsNullOrWhiteSpace(Password)) return false;

            return true;
        }
    }

    public static class RepositoryImporter
    {
        public static async Task<RepositoryImportResult> ImportAsync(string projectName, string sourceRepoUrl, SourceCodeRepositoryType repoType, AppConfiguration appConfiguration, IRepositoryAuthenticationInfo authInfo,
            ReadOnlyCollection<IRepositoryImportStrategy> strategies)
        {
            if (authInfo != null)
            {
                if (await authInfo.Validate())
                {
                    Log.Information($"Validation for {nameof(authInfo)} did not pass.");

                    return new RepositoryImportResult(string.Empty, true);
                }
            }

            var strategy = strategies.FirstOrDefault(x => x.Type == repoType);

            if (strategy == null)
            {
                Log.Error($"Unknown Source Code Import type: {repoType}");
                return default;
            }

            var result = await strategy.ImportAsync(projectName, sourceRepoUrl, appConfiguration, authInfo);

            return result;
        }
    }

    public interface IRepositoryImportStrategy
    {
        SourceCodeRepositoryType Type { get; }

        Task<RepositoryImportResult> ImportAsync(string projectName, string sourceRepoUrl, AppConfiguration appConfiguration, IRepositoryAuthenticationInfo authInfo);
    }

    public sealed class SvnRepositoryStrategy : IRepositoryImportStrategy
    {
        public SourceCodeRepositoryType Type => SourceCodeRepositoryType.Svn;

        public async Task<RepositoryImportResult> ImportAsync(string projectName, string sourceRepoUrl, AppConfiguration appConfiguration, IRepositoryAuthenticationInfo authInfo)
        {
            var folderName = $"{projectName}_{DateTime.UtcNow.Ticks}";
            var path = Path.Combine(appConfiguration.BaseDirectory, folderName);

            var info = authInfo as SvnAuthInfo;
            if (info == null)
            {
                return new RepositoryImportResult(string.Empty, true);
            }

            try
            {
                Directory.CreateDirectory(path);

                using (var client = new SharpSvn.SvnClient())
                {
                    client.Authentication.DefaultCredentials = new NetworkCredential(info.Username, info.Password);

                    Log.Information("Checking out...");

                    var result = client.CheckOut(new SharpSvn.SvnUriTarget(sourceRepoUrl), path);

                    Log.Information($"Check out complete. Result = {result}");
                }

                Log.Information(string.Empty);

                return new RepositoryImportResult(path, false);
            }
            catch (Exception exp)
            {
                Log.Error(exp, string.Empty);

                return new RepositoryImportResult(string.Empty, true);
            }
        }
    }

    public static class SourceCodeBuilder
    {
        public static async Task<ReadOnlyCollection<BuildResult>> BuildTargetsAsync(string checkOutDirectory, BuildTarget[] targets, ReadOnlyCollection<ISourceCodeBuilderStrategy> strategies)
        {
            var result = new List<BuildResult>();
            foreach (var t in targets)
            {
                var sourcePath = Path.Combine(checkOutDirectory, t.TargetRelativePath);
                var outPath = Path.Combine(checkOutDirectory, t.Name);

                Log.Information($"Building Target: {t.Type} {t.Name} {t.TargetRelativePath}");

                BuildStrategyResult currentResult = default;
                var strategy = strategies.FirstOrDefault(x => x.Type == t.Type);

                if (strategy == null)
                {
                    Log.Error($"Unknown build target type: {t.Type}");
                    continue;
                }

                currentResult = await strategy.BuildAsync(t, sourcePath, outPath);

                Log.Information($"Output: IsError: {currentResult.IsError}");
                Log.Information(currentResult.Output);
                Log.Information(string.Empty);

                result.Add(new BuildResult(t, outPath, currentResult.IsError));
            }

            return result.AsReadOnly();
        }
    }

    public record BuildStrategyResult(string Output, bool IsError);

    public static class DeployManager
    {
        public static async Task<ReadOnlyCollection<DeployResult>> DeployTargetsAsync(DeployTarget[] targets, IReadOnlyCollection<IDeployStrategy> strategies)
        {
            var result = new List<DeployResult>();
            foreach (var t in targets)
            {
                Log.Information($"Deploying Target: {t.Type} {t.BuildTarget.Target.Name} {t.BuildTarget.Target.TargetRelativePath}");

                DeployStrategyResult currentResult = default;
                var strategy = strategies.FirstOrDefault(x => x.Type == t.Type);

                if (strategy == null)
                {
                    Log.Error($"Unknown deploy target type: {t.Type}");
                    continue;
                }

                currentResult = await strategy.DeployAsync(t);

                Log.Information($"Output: IsError: {currentResult.IsError}");
                Log.Information(currentResult.Output);
                Log.Information(string.Empty);

                result.Add(new DeployResult(t, currentResult.Output, currentResult.IsError));
            }

            return result.AsReadOnly();
        }
    }

    public interface ISourceCodeBuilderStrategy
    {
        BuildTargetType Type { get; }

        Task<BuildStrategyResult> BuildAsync(BuildTarget target, string sourcePath, string outPath);
    }

    public sealed class DotNetCoreSourceBuilderStrategy : ISourceCodeBuilderStrategy
    {
        public BuildTargetType Type => BuildTargetType.DotNetCore;

        public async Task<BuildStrategyResult> BuildAsync(BuildTarget target, string sourcePath, string outPath)
        {
            try
            {
                var cmd = $"dotnet publish {sourcePath} -c Release --self-contained -r win-x64 -o {outPath}";

                Log.Information(string.Empty);
                Log.Information(cmd);
                Log.Information(string.Empty);

                var output = await Shell.Execute(cmd);

                var isError = string.IsNullOrEmpty(output) || output.Contains(": error");

                return new BuildStrategyResult(output, isError);
            }
            catch (Exception exp)
            {
                Log.Error(exp, string.Empty);
                return new BuildStrategyResult(string.Empty, true);
            }
        }
    }

    public interface IDeployStrategy
    {
        DeployTargetType Type { get; }

        Task<DeployStrategyResult> DeployAsync(DeployTarget target);
    }

    public sealed class InternetInformationServerDeploymentStrategy : IDeployStrategy
    {
        public DeployTargetType Type => DeployTargetType.IIS;

        public async Task<DeployStrategyResult> DeployAsync(DeployTarget target)
        {
            var info = target.ExtraInfo as IISDeployTargetExtraInfo;

            if (IISDeployTargetExtraInfo.Validate(info) == false)
            {
                return new DeployStrategyResult(string.Empty, true);
            }

            return await Task.Run(() =>
            {
                try
                {
                    using (var manager = new ServerManager())
                    {
                        var currentSites = manager.Sites;
                        foreach (var item in currentSites)
                        {
                            Log.Information(item.Name);
                        }

                        // Find out if the site exists already.
                        var site = manager.Sites.FirstOrDefault(x => x.Name == info.SiteName);
                        if (site == null)
                        {
                            site = manager.Sites.Add(info.SiteName, target.BuildTarget.OutputPath, info.Port);
                            manager.CommitChanges();
                        }
                        else
                        {
                            site.Stop();

                            site.Applications["/"].VirtualDirectories["/"].PhysicalPath = target.BuildTarget.OutputPath;
                            //var app = site.Applications.FirstOrDefault();
                            //if (app != null)
                            //{
                            //    app.VirtualDirectories.First(x => x.Path == "/").PhysicalPath = target.BuildTarget.OutputPath;
                            //}

                            manager.CommitChanges();

                            site.Start();
                        }
                    }

                    return new DeployStrategyResult(string.Empty, false);
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return new DeployStrategyResult(string.Empty, true);
                }
            });
        }
    }

    public record DeployStrategyResult(string Output, bool IsError);
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}