using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.Runner;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.RepositoryImporter
{
    public interface IRepositoryImportStrategy
    {
        SourceCodeRepositoryType Type { get; }

        Task<RepositoryImportResult> ImportAsync(Project entity, AppConfiguration appConfiguration, IRepositoryAuthenticationInfo authInfo, CustomLogger runnerLogger, string commitId = null);

        Task<List<RepositoryCommit>> GetLastCommitsAsync(Project entity, AppConfiguration appConfiguration, IRepositoryAuthenticationInfo authInfo, CustomLogger runnerLogger, int count);
    }

    public record RepositoryImportResult(string CheckOutDirectory, bool HasErrors);

    public class RepositoryCommit
    {
        public RepositoryCommit()
        {
        }

        public RepositoryCommit(string commitId, string commitComment, DateTime commitStamp, string commitExtra)
        {
            this.CommitId = commitId;
            this.CommitComment = commitComment;
            this.CommitStamp = commitStamp;
            this.CommitExtra = commitExtra;
        }

        public string CommitId { get; set; } 
        public string CommitComment { get; set; } 
        public DateTime CommitStamp { get; set; }
        public string CommitExtra { get; set; }
    }
}
