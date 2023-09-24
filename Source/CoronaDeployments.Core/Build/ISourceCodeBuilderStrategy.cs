using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.Runner;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.Build
{
    public interface ISourceCodeBuilderStrategy
    {
        BuildTargetType Type { get; }

        Task<BuildStrategyResult> BuildAsync(BuildTarget target, string sourcePath, string outPath, CustomLogger customLogger);
    }

    public record BuildStrategyResult(string Output, bool IsError);
}
