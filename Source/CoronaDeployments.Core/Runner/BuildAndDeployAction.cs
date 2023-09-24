using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Deploy;
using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.Repositories;
using CoronaDeployments.Core.RepositoryImporter;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.Runner
{
    public sealed class BuildAndDeployAction : IRunnerAction
    {
        public BuildAndDeployAction()
        {
            Logger = new CustomLogger();
        }

        public CustomLogger Logger { get; private set; }
        public bool HasErrors { get; private set; }

        private BuildAndDeployActionPayload Payload { get; set; }

        public async Task Implementation(IRunnerActionPayload p)
        {
            Logger.Information($"{nameof(BuildAndDeployAction)} Started.");

            var payload = p as BuildAndDeployActionPayload;
            if (payload == null)
            {
                Logger.Error("Payload is null");
                HasErrors = true;
                return;
            }
            else
            {
                Payload = payload;
            }

            try
            {
                var requests = await payload.ProjectRepository.GetBuildAndDeployRequests(payload.ProjectId, BuildAndDeployRequestState.Created, true);
                if (requests != null && requests.Count > 0)
                {
                    // Clear the log.
                    Logger.Clear();

                    foreach (var r in requests.OrderByDescending(x => x.Request.CreatedAtUtc))
                    {
                        var task = DoWork(r);

                        await payload.ProjectRepository.UpdateBuildAndDeployRequest(r.Request.Id, Logger.ToString(), null, DateTime.UtcNow);
                        
                        while (task.IsCompleted == false)
                        {
                            // Persist Log
                            await payload.ProjectRepository.UpdateBuildAndDeployRequest(r.Request.Id, Logger.ToString(), null, null);

                            // Sleep for a while
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }

                        // Mark this request as Completed
                        await payload.ProjectRepository.UpdateBuildAndDeployRequest(r.Request.Id, Logger.ToString(), BuildAndDeployRequestState.Completed, null);

                        // Clear the Log.
                        Logger.Clear();
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp);
            }

            Logger.Information($"{nameof(BuildAndDeployAction)} Done.");
        }

        public async Task DoWork(BuildAndDeployRequestModel request)
        {
            try
            {
                using (var scope = Payload.ServiceProvider.CreateScope())
                {
                    Logger.Information("Start fetching reposiotory...");

                    var config = scope.ServiceProvider.GetRequiredService<AppConfiguration>();
                    var authConfigs = scope.ServiceProvider.GetServices<IRepositoryAuthenticationInfo>();
                    var importStrategies = scope.ServiceProvider.GetServices<IRepositoryImportStrategy>();
                    var buildStrategies = scope.ServiceProvider.GetServices<ISourceCodeBuilderStrategy>();
                    var deployStrategies = scope.ServiceProvider.GetServices<IDeployStrategy>();

                    var importResult = await RepositoryManager.ImportAsync(request.Project,  
                        config, 
                        authConfigs.First(x => x.Type == request.Project.RepositoryType),
                        new ReadOnlyCollection<IRepositoryImportStrategy>(importStrategies.ToList()),
                        Logger,
                        request.Cursor.Info.CommitId);
                    if (importResult.HasErrors)
                    {
                        Logger.Error($"Importing repository did not complete successfully.");
                        return;
                    }

                    Logger.Information($"Project directory: {importResult.CheckOutDirectory}");


                    Logger.Information("Start building reposiotory...");
                    if (request.Project.BuildTargets == null || request.Project.BuildTargets.Count == 0)
                    {
                        Logger.Error("Project doesn't contain BuildTargets.");
                        return;
                    }

                    var buildResults = await SourceCodeBuilder.BuildTargetsAsync(importResult.CheckOutDirectory,
                        request.Project.BuildTargets.ToArray(),
                        new ReadOnlyCollection<ISourceCodeBuilderStrategy>(buildStrategies.ToList()),
                        Logger);

                    if (buildResults.Any(x => x.HasErrors))
                    {
                        Logger.Error("Not all BuildTargets where build successfully. Skipping deployment.");
                        Logger.Information("End.");

                        return;
                    }

                    Logger.Information("Start deploying reposiotory...");
                    var deployResult = await DeployManager.DeployTargetsAsync(buildResults.ToArray(),
                        new ReadOnlyCollection<IDeployStrategy>(deployStrategies.ToList()),
                        Logger);
                    if (deployResult.Any(x => x.HasErrors))
                    {
                        Logger.Error("Not all deployments ran successfully.");
                    }

                    Logger.Information("End.");
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp);
            }
        }
    }

    public sealed class BuildAndDeployActionPayload : IRunnerActionPayload
    {
        public Guid ProjectId { get; set; }

        public IProjectRepository ProjectRepository { get; set; }

        public IServiceProvider ServiceProvider { get; set; }
    }
}