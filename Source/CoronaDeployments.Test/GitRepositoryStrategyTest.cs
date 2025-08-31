using CoronaDeployments.Core;
using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.RepositoryImporter;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace CoronaDeployments.Test
{
    public class GitRepositoryStrategyTest
    {
        private readonly string TestBaseDirectory = Path.Combine(Path.GetTempPath(), "corona-test", Guid.NewGuid().ToString());

        [Fact]
        public async Task GetLastCommits()
        {
            var s = new GitRepositoryStrategy();
            var p = new Project
            {
                Name = "TestProject",
                RepositoryUrl = "https://github.com/SherifRefaat/CoronaDeployments.git",
                BranchName = "main",
            };
            
            // Use test credentials - in real tests, these should be mocked
            var testAuthInfo = new AuthInfo("test-user", "test-password", SourceCodeRepositoryType.Git);
            var testConfig = new Core.AppConfiguration(TestBaseDirectory);

            // Create test directory
            Directory.CreateDirectory(TestBaseDirectory);

            try
            {
                var result = await s.GetLastCommitsAsync(p, testConfig, testAuthInfo, new Core.Runner.CustomLogger(), 10);

                // Note: This test will likely fail without proper credentials or network access
                // In a real scenario, we should mock the repository access
                Assert.NotNull(result);
            }
            finally
            {
                // Clean up test directory
                if (Directory.Exists(TestBaseDirectory))
                {
                    Directory.Delete(TestBaseDirectory, true);
                }
            }
        }
    }
}
