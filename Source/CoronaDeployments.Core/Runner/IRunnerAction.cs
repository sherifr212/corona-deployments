using CoronaDeployments.Core.Repositories;
using System;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.Runner
{
    public interface IRunnerAction
    {
        public Task Implementation(IRunnerActionPayload payload);
    }

    public interface IRunnerActionPayload
    {
        public IServiceProvider ServiceProvider { get; set; }
    }
}
