using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.RepositoryImporter;
using CoronaDeployments.Core.Runner;
using Serilog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDeployments.Core
{
    public static class RepositoryManager
    {
        public static async Task<RepositoryImportResult> ImportAsync(Project project, 
            AppConfiguration appConfiguration, 
            IRepositoryAuthenticationInfo authInfo,
            ReadOnlyCollection<IRepositoryImportStrategy> strategies,
            CustomLogger runnerLogger,
            string commitId = null)
        {
            if (authInfo != null)
            {
                if (await authInfo.Validate() == false)
                {
                    runnerLogger.Information($"Validation for {nameof(authInfo)} did not pass.");

                    return new RepositoryImportResult(string.Empty, true);
                }
            }

            var strategy = strategies.FirstOrDefault(x => x.Type == project.RepositoryType);

            if (strategy == null)
            {
                runnerLogger.Error($"Unknown Source Code Import type: {project.RepositoryType}");
                return default;
            }

            var result = await strategy.ImportAsync(project, appConfiguration, authInfo, runnerLogger, commitId);

            return result;
        }

        public static async Task<List<RepositoryCommit>> GetCommitList(Project p, SourceCodeRepositoryType repoType, AppConfiguration appConfiguration, 
            IRepositoryAuthenticationInfo authInfo,
            ReadOnlyCollection<IRepositoryImportStrategy> strategies, 
            CustomLogger runnerLogger,
            int count)
        {
            if (authInfo != null)
            {
                if (await authInfo.Validate() == false)
                {
                    runnerLogger.Information($"Validation for {nameof(authInfo)} did not pass.");

                    return null;
                }
            }

            var strategy = strategies.FirstOrDefault(x => x.Type == repoType);

            if (strategy == null)
            {
                runnerLogger.Error($"Unknown Source Code Import type: {repoType}");
                return null;
            }

            var result = await strategy.GetLastCommitsAsync(p, appConfiguration, authInfo, runnerLogger, count);

            return result;
        }
    }
}
