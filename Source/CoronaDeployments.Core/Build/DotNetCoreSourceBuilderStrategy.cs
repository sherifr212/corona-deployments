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
                var cmd = $"dotnet publish {sourcePath} -c Release --self-contained -r win-x64 -o {outPath}";

                customLogger.Information(string.Empty);
                customLogger.Information(cmd);
                customLogger.Information(string.Empty);

                var output = await Shell.Execute(cmd);

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
