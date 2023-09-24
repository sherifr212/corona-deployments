using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.Runner;
using Microsoft.Web.Administration;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.Deploy
{
    public sealed class InternetInformationServerDeploymentStrategy : IDeployStrategy
    {
        public DeployTargetType Type => DeployTargetType.IIS;

        public async Task<DeployStrategyResult> DeployAsync(BuildResult buildResult, CustomLogger customLogger)
        {
            var info = buildResult.Target.DeploymentExtraInfo as IISDeployTargetExtraInfo;

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
                        customLogger.Information("Current sites installed:");

                        var currentSites = manager.Sites;
                        foreach (var item in currentSites)
                        {
                            customLogger.Information(item.Name);
                        }

                        customLogger.Information(string.Empty);

                        // Find out if the site exists already.
                        var site = manager.Sites.FirstOrDefault(x => x.Name == info.SiteName);
                        if (site == null)
                        {
                            site = manager.Sites.Add(info.SiteName, buildResult.OutputPath, info.Port);
                            manager.CommitChanges();
                        }
                        else
                        {
                            site.Stop();

                            site.Applications["/"].VirtualDirectories["/"].PhysicalPath = buildResult.OutputPath;

                            manager.CommitChanges();

                            site.Start();
                        }
                    }

                    return new DeployStrategyResult(string.Empty, false);
                }
                catch (Exception exp)
                {
                    customLogger.Error(exp);
                    return new DeployStrategyResult(string.Empty, true);
                }
            });
        }
    }
}