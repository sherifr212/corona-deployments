using CoronaDeployments.Core;
using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.RepositoryImporter;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CoronaDeployments.Test
{
    public class GitRepositoryStrategyTest
    {
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
            var authInfo = new AuthInfo(Email.Value1, Password.Value1, SourceCodeRepositoryType.Git);

            var config = new Core.AppConfiguration(@"C:\Repository\TestOldFashion");

            var result = await s.GetLastCommitsAsync(p, config, authInfo, new Core.Runner.CustomLogger(), 10);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(10, result.Count);
        }
    }
}
