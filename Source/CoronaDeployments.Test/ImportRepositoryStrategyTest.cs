using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.RepositoryImporter;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace CoronaDeployments.Test
{
    public class ImportRepositoryStrategyTest
    {
        private readonly string TestBaseDirectory = Path.Combine(Path.GetTempPath(), "corona-test", Guid.NewGuid().ToString());

        [Fact]
        public async Task GitRepositoryStrategy()
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
                var result = await s.ImportAsync(
                    p,
                    testConfig,
                    testAuthInfo,
                    new Core.Runner.CustomLogger());

                // Note: This test will likely fail without proper credentials
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