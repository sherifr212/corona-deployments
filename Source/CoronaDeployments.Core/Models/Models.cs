using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Deploy;
using CoronaDeployments.Core.RepositoryImporter;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.Models
{
    public class BuildTarget
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; }
        public string TargetRelativePath { get; set; }
        public BuildTargetType Type { get; set; }
        public DeployTargetType DeploymentType { get; set; }
        public IDeployTargetExtraInfo DeploymentExtraInfo { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid CreatedByUserId { get; set; }
    }

    public class Project 
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string RepositoryUrl { get; set; }
        public string BranchName { get; set; }
        public SourceCodeRepositoryType RepositoryType { get; set; }
        public IReadOnlyList<BuildTarget> BuildTargets { get; set; } = new List<BuildTarget>(0);
        public DateTime CreatedAtUtc { get; set; }
        public Guid CreatedByUserId { get; set; }
        public IReadOnlyList<RepositoryCursor> Cursors { get; set; } = new List<RepositoryCursor>(0);
    }

    public class RepositoryCursor
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid ProjectId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid CreatedByUserId { get; set; }
        public RepositoryCommit Info { get; set; }
    }

    public class BuildAndDeployRequest
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid CursorId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid CreatedByUserId { get; set; }
        public BuildAndDeployRequestState State { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string Log { get; set; }
    }

    public class IISDeployInfo
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid BuildTargetId { get; set; }
        public IISDeployTargetExtraInfo Info { get; set; }
        public Guid CreatedByUserId { get; set; }
    }

    public class BuildAndDeployRequestModel
    {
        public BuildAndDeployRequest Request { get; set; }
        public Project Project { get; set; }
        public RepositoryCursor Cursor { get; set; }
    }

    public enum BuildAndDeployRequestState
    {
        Created = 1,
        Completed = 2,
    }

    public record BuildResult(BuildTarget Target, string OutputPath, bool HasErrors);

    public record DeployResult(BuildResult Target, string Output, bool HasErrors);

    internal interface IProjectOperation
    {
        public Guid Id { get; }
        public DateTime CreatedAtUtc { get; }
        public Guid ProjectId { get; }

        public Task Execute(Project project);
        public DateTime? GetCompletedAtUtc();
    }

    public class Build : IProjectOperation
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid ProjectId { get; set; }
        public BuildResult Result { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }

        public async Task Execute(Project project)
        {
        }

        public DateTime? GetCompletedAtUtc()
        {
            return this.CompletedAtUtc;
        }
    }

    public class Deployment : IProjectOperation
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid ProjectId { get; set; }
        public DeployResult Result { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }

        public async Task Execute(Project project)
        {
        }

        public DateTime? GetCompletedAtUtc()
        {
            return this.CompletedAtUtc;
        }
    }

    public class ProjectOperationJob
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Instance { get; set; }
        public ProjectOperationType OperationType { get; set; }
    }

    public enum ProjectOperationType
    {
        Build = 1,
        Deploy = 2
    }

    public enum ProjectOperationStatus
    {
        Requested = 1,
        Running = 2,
        Failed = 3,
        Success = 4
    }

    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string PasswordHashed { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class Session
    {
        public User User { get; set; }
    }
}