using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.Runner;
using LibGit2Sharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.RepositoryImporter
{
    public class GitRepositoryStrategy : IRepositoryImportStrategy
    {
        public SourceCodeRepositoryType Type => SourceCodeRepositoryType.Git;

        public Task<List<RepositoryCommit>> GetLastCommitsAsync(Project entity, AppConfiguration appConfiguration, IRepositoryAuthenticationInfo authInfo,
            CustomLogger runnerLogger,
            int count)
        {
            return Task.Run(async () =>
            {
                var importResult = await ImportAsync(entity, appConfiguration, authInfo, runnerLogger);
                if (importResult.HasErrors)
                {
                    return null;
                }

                try
                {
                    List<RepositoryCommit> result = new List<RepositoryCommit>(0);

                    using (var repo = new Repository(importResult.CheckOutDirectory))
                    {
                        result = repo.Commits
                            .Take(count)
                            .Select(x => new RepositoryCommit(x.Id.Sha, x.Message, x.Author.When.UtcDateTime, $"{x.Author.Name} - {x.Author.Email}"))
                            .ToList();
                    }

                    // Clean up. We no longer need the pository.
                    try
                    {
                        Directory.Delete(importResult.CheckOutDirectory, true);
                    }
                    catch (Exception exp)
                    {
                        runnerLogger.Error(exp);
                    }

                    return result;
                }
                catch (Exception exp)
                {
                    runnerLogger.Error(exp);

                    return null;
                }
            });
        }

        public Task<RepositoryImportResult> ImportAsync(Project project, AppConfiguration appConfiguration,
            IRepositoryAuthenticationInfo authInfo,
            CustomLogger runnerLogger,
            string commitId = null)
        {
            return Task.Run(() =>
            {
                // Try to get the auth data.
                var info = authInfo as AuthInfo;
                if (info == null)
                {
                    return new RepositoryImportResult(string.Empty, true);
                }

                // Setup storage directory.
                var folderName = $"{project.Name}_{DateTime.UtcNow.Ticks}";
                var path = Path.Combine(appConfiguration.BaseDirectory, folderName);

                try
                {
                    // Create the path before we start.
                    Directory.CreateDirectory(path);

                    // Clone the repository first.
                    var cloneOptions = new CloneOptions
                    {
                        CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                        {
                            Username = info.Username,
                            Password = info.Password
                        }
                    };

                    var cloneResult = Repository.Clone(project.RepositoryUrl, path, cloneOptions);
                    if (string.IsNullOrEmpty(cloneResult))
                    {
                        return new RepositoryImportResult(string.Empty, true);
                    }

                    // Checkout the branch.
                    using (var repo = new Repository(path))
                    {
                        var branch = repo.Branches[project.BranchName ?? "main"];

                        if (branch == null)
                        {
                            new RepositoryImportResult(string.Empty, true);
                        }

                        runnerLogger.Information("Checking out...");

                        Branch currentBranch;

                        if (commitId != null)
                        {
                            var localCommit = repo.Lookup<Commit>(new ObjectId(commitId));
                            if (localCommit == null)
                            {
                                runnerLogger.Error("Could not find branch.");
                                return new RepositoryImportResult(null, true);
                            }

                            currentBranch = Commands.Checkout(repo, localCommit);
                        }
                        else
                        {
                            currentBranch = Commands.Checkout(repo, branch);
                        }

                        runnerLogger.Information($"Check out complete. Result = {currentBranch != null}");

                        if (currentBranch != null)
                        {
                            return new RepositoryImportResult(path, false);
                        }
                        else
                        {
                            return new RepositoryImportResult(path, true);
                        }
                    }
                }
                catch (Exception exp)
                {
                    runnerLogger.Error(exp);

                    return new RepositoryImportResult(string.Empty, true);
                }
            });
        }
    }
}