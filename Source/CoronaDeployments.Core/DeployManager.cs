using CoronaDeployments.Core.Deploy;
using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.RepositoryImporter;
using CoronaDeployments.Core.Runner;
using Serilog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CoronaDeployments.Core
{
    public static class DeployManager
    {
        public static async Task<ReadOnlyCollection<DeployResult>> DeployTargetsAsync(BuildResult[] targets, IReadOnlyCollection<IDeployStrategy> strategies, 
            CustomLogger customLogger)
        {
            var result = new List<DeployResult>();
            foreach (var t in targets)
            {
                customLogger.Information($"Deploying Target: {t.Target.Type} {t.Target.Name} {t.Target.TargetRelativePath}");

                DeployStrategyResult currentResult = default;
                var strategy = strategies.FirstOrDefault(x => x.Type == t.Target.DeploymentType);

                if (strategy == null)
                {
                    customLogger.Error($"Unknown deploy target type: {t.Target.Type}");
                    continue;
                }

                currentResult = await strategy.DeployAsync(t, customLogger);

                if (currentResult.IsError)
                {
                    customLogger.Error($"Output: IsError: {currentResult.IsError}");
                    customLogger.Error(currentResult.Output);
                }
                else
                {
                    customLogger.Information($"Output: IsError: {currentResult.IsError}");
                    customLogger.Information(currentResult.Output);
                }

                customLogger.Information(string.Empty);
                
                result.Add(new DeployResult(t, currentResult.Output, currentResult.IsError));
            }

            return result.AsReadOnly();
        }
    }

    public record DeployStrategyResult(string Output, bool IsError);
}
