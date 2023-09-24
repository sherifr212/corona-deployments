using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.Runner;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.Deploy
{
    public interface IDeployStrategy
    {
        DeployTargetType Type { get; }

        Task<DeployStrategyResult> DeployAsync(BuildResult target, CustomLogger customLogger);
    }
}
