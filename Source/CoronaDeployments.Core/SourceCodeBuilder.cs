using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.RepositoryImporter;
using CoronaDeployments.Core.Runner;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoronaDeployments.Core
{
    public static class SourceCodeBuilder
    {
        public static async Task<ReadOnlyCollection<BuildResult>> BuildTargetsAsync(string checkOutDirectory, BuildTarget[] targets,
            ReadOnlyCollection<ISourceCodeBuilderStrategy> strategies,
            CustomLogger customLogger)
        {
            var result = new List<BuildResult>();
            foreach (var t in targets)
            {
                var sourcePath = Path.Combine(checkOutDirectory, t.TargetRelativePath);
                var outPath = Path.Combine(checkOutDirectory, t.Name);

                customLogger.Information($"Building Target: {t.Type} {t.Name} {t.TargetRelativePath}");

                BuildStrategyResult currentResult = default;
                var strategy = strategies.FirstOrDefault(x => x.Type == t.Type);

                if (strategy == null)
                {
                    customLogger.Error($"Unknown build target type: {t.Type}");
                    continue;
                }

                currentResult = await strategy.BuildAsync(t, sourcePath, outPath, customLogger);

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

                result.Add(new BuildResult(t, outPath, currentResult.IsError));
            }

            return result.AsReadOnly();
        }
    }
}