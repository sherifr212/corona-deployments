using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.Runner;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.Build
{
    public sealed class DotNetCoreSourceBuilderStrategy : ISourceCodeBuilderStrategy
    {
        public BuildTargetType Type => BuildTargetType.DotNetCore;

        public async Task<BuildStrategyResult> BuildAsync(BuildTarget target, string sourcePath, string outPath, CustomLogger customLogger)
        {
            try
            {
                var arguments = $"publish {sourcePath} -c Release --self-contained -r win-x64 -o {outPath}";

                customLogger.Information(string.Empty);
                customLogger.Information($"dotnet {arguments}");
                customLogger.Information(string.Empty);

                // Use the new secure Shell.Execute method
                var output = await Shell.Execute("dotnet", arguments);

                var isError = string.IsNullOrEmpty(output) || output.Contains(": error");

                return new BuildStrategyResult(output, isError);
            }
            catch (Exception exp)
            {
                customLogger.Error(exp);
                return new BuildStrategyResult(string.Empty, true);
            }
        }
    }
}
