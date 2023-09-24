using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.Runner;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.RepositoryImporter
{
    public sealed class SvnRepositoryStrategy : IRepositoryImportStrategy
    {
        public SourceCodeRepositoryType Type => SourceCodeRepositoryType.Svn;

        public Task<List<RepositoryCommit>> GetLastCommitsAsync(Project entity, AppConfiguration appConfiguration, IRepositoryAuthenticationInfo authInfo, CustomLogger runnerLogger, int count)
        {
            return Task.Run(async () =>
            {
                var info = authInfo as AuthInfo;
                if (info == null)
                {
                    return null;
                }

                try
                {
                    var checkoutResult = await ImportAsync(entity, appConfiguration, authInfo, runnerLogger);
                    if (checkoutResult.HasErrors)
                    {
                        return null;
                    }

                    List<RepositoryCommit> result = null;
                    using (var client = new SharpSvn.SvnClient())
                    {
                        //client.Authentication.DefaultCredentials = new NetworkCredential(info.Username, info.Password);

                        if (client.GetLog(checkoutResult.CheckOutDirectory, new SharpSvn.SvnLogArgs() { Limit = count }, out var items))
                        {
                            result = items
                                .Select(x => new RepositoryCommit(x.Revision.ToString(), x.LogMessage, x.Time.ToUniversalTime(), $"{x.Author}"))
                                .ToList();
                        }
                    }

                    try
                    {
                        // Clean up the directory.
                        Directory.Delete(checkoutResult.CheckOutDirectory, recursive: true);
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

        public Task<RepositoryImportResult> ImportAsync(Project entity, AppConfiguration appConfiguration, IRepositoryAuthenticationInfo authInfo, CustomLogger runnerLogger, string commitId = null)
        {
            return Task.Run(() =>
            {
                var folderName = $"{entity.Name}_{DateTime.UtcNow.Ticks}";
                var path = Path.Combine(appConfiguration.BaseDirectory, folderName);

                var info = authInfo as AuthInfo;
                if (info == null)
                {
                    return new RepositoryImportResult(string.Empty, true);
                }

                try
                {
                    Directory.CreateDirectory(path);

                    using (var client = new SharpSvn.SvnClient())
                    {
                        client.Authentication.DefaultCredentials = new NetworkCredential(info.Username, info.Password);

                        runnerLogger.Information("Checking out...");

                        bool result = false;
                        
                        if (commitId == null)
                            result = client.CheckOut(new SharpSvn.SvnUriTarget(entity.RepositoryUrl), path);
                        else
                            result = client.CheckOut(new SharpSvn.SvnUriTarget(entity.RepositoryUrl), path, new SharpSvn.SvnCheckOutArgs
                            {
                                Revision = new SharpSvn.SvnRevision(int.Parse(commitId))
                            });

                        runnerLogger.Information($"Check out complete. Result = {result}");
                    }

                    runnerLogger.Information(string.Empty);

                    return new RepositoryImportResult(path, false);
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