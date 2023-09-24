using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.Models.Mvc;
using Marten;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.Repositories
{
    public interface IProjectRepository
    {
        public Task<bool> Create(ProjectCreateModel model);

        public Task<(bool, Guid)> CreateBuildTarget(BuildTargetCreateModel model);

        public Task<IReadOnlyList<Project>> GetAll();

        public Task<Project> Get(Guid id);

        public Task<bool> CreateRepositoryCursor(RepositoryCursorCreateModel model);

        public Task<(bool, Guid)> CreateBuildAndDeployRequest(Guid projectId, Guid cursorId, Guid userId);

        public Task<BuildAndDeployRequestModel> GetBuildAndDeployRequest(Guid id);

        public Task<List<BuildAndDeployRequestModel>> GetBuildAndDeployRequests(Guid projectId, BuildAndDeployRequestState? state, bool includeProject = false);

        public Task UpdateBuildAndDeployRequest(Guid id, string log, BuildAndDeployRequestState? newState, DateTime? startedAt);

        public Task<bool> CreateIISDeployInfo(IISBuildTargetConfigurationCreateModel m);
    }

    public class ProjectRepository : IProjectRepository
    {
        private IDocumentStore _store;

        public ProjectRepository(IDocumentStore store)
        {
            _store = store;
        }

        public async Task<bool> Create(ProjectCreateModel model)
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var e = ToEntity(model);

                    session.Store(e);

                    await session.SaveChangesAsync();

                    return true;
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return false;
                }
            }
        }

        public async Task<(bool, Guid)> CreateBuildTarget(BuildTargetCreateModel model)
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var e = ToEntity(model);

                    session.Store(e);

                    await session.SaveChangesAsync();

                    return (true, e.Id);
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return (false, Guid.Empty);
                }
            }
        }

        public async Task<IReadOnlyList<Project>> GetAll()
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var projects = await session.Query<Project>()
                        .OrderBy(x => x.CreatedAtUtc)
                        .ToListAsync();

                    // Batch in BuildTargets.
                    foreach (var p in projects)
                    {
                        var buildTargets = await session.Query<BuildTarget>()
                            .Where(x => x.ProjectId == p.Id)
                            .OrderBy(x => x.CreatedAtUtc)
                            .ToListAsync();

                        var cursors = await session.Query<RepositoryCursor>()
                            .Where(x => x.ProjectId == p.Id)
                            .OrderByDescending(x => x.CreatedAtUtc)
                            .ToListAsync();

                        p.BuildTargets = buildTargets;
                        p.Cursors = cursors;
                    }

                    return projects;
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return null;
                }
            }
        }

        public async Task<Project> Get(Guid id)
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var project = await session.Query<Project>()
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return project;
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return null;
                }
            }
        }

        public async Task<bool> CreateRepositoryCursor(RepositoryCursorCreateModel model)
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var e = ToEntity(model);

                    session.Store(e);

                    await session.SaveChangesAsync();

                    return true;
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return false;
                }
            }
        }

        public async Task<(bool, Guid)> CreateBuildAndDeployRequest(Guid projectId, Guid cursorId, Guid userId)
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var e = new BuildAndDeployRequest
                    {
                        CreatedAtUtc = DateTime.UtcNow,
                        CreatedByUserId = userId,
                        ProjectId = projectId,
                        CursorId = cursorId,
                        State = BuildAndDeployRequestState.Created
                    };

                    session.Store(e);

                    await session.SaveChangesAsync();

                    return (true, e.Id);
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return (false, Guid.Empty);
                }
            }
        }

        public async Task<List<BuildAndDeployRequestModel>> GetBuildAndDeployRequests(Guid projectId, BuildAndDeployRequestState? state, bool includeProject = false)
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var query = session.Query<BuildAndDeployRequest>()
                        .Where(x => x.ProjectId == projectId);

                    if (state != null)
                    {
                        query = query.Where(x => x.State == state.Value);
                    }

                    var requests = await query.ToListAsync();

                    if (requests.Count == 0)
                    {
                        return new List<BuildAndDeployRequestModel>(0);
                    }

                    Project project = null;

                    if (includeProject)
                    {
                        project = await session.Query<Project>()
                            .FirstOrDefaultAsync(x => x.Id == projectId);

                        project.BuildTargets = await session.Query<BuildTarget>()
                            .Where(x => x.ProjectId == projectId)
                            .ToListAsync();

                        foreach (var bt in project.BuildTargets)
                        {
                            if (bt.DeploymentType == Deploy.DeployTargetType.IIS)
                            {
                                var info = await session.Query<IISDeployInfo>()
                                    .Where(x => x.BuildTargetId == bt.Id)
                                    .FirstOrDefaultAsync();

                                bt.DeploymentExtraInfo = info?.Info;
                            }
                        }
                    }

                    var result = requests
                        .Select(x => new BuildAndDeployRequestModel
                        {
                            Request = x,
                            Project = project
                        })
                        .ToList();

                    foreach (var r in result)
                    {
                        var cursor = await session.Query<RepositoryCursor>()
                            .FirstOrDefaultAsync(x => x.Id == r.Request.CursorId);

                        r.Cursor = cursor;
                    }

                    return result;
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return null;
                }
            }
        }

        public async Task<BuildAndDeployRequestModel> GetBuildAndDeployRequest(Guid id)
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var r = await session.Query<BuildAndDeployRequest>()
                        .FirstOrDefaultAsync(x => x.Id == id);

                    if (r == null)
                    {
                        return null;
                    }

                    var project = await session.Query<Project>()
                        .FirstOrDefaultAsync(x => x.Id == r.ProjectId);

                    var cursor = await session.Query<RepositoryCursor>()
                        .FirstOrDefaultAsync(x => x.Id == r.CursorId);

                    return new BuildAndDeployRequestModel
                    {
                        Request = r,
                        Project = project,
                        Cursor = cursor,
                    };
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return null;
                }
            }
        }

        public async Task UpdateBuildAndDeployRequest(Guid id, string log, BuildAndDeployRequestState? newState, DateTime? startedAt)
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var r = await session.Query<BuildAndDeployRequest>()
                        .FirstOrDefaultAsync(x => x.Id == id);

                    if (string.IsNullOrEmpty(log) == false)
                    {
                        r.Log = log;
                    }

                    if (startedAt.HasValue)
                    {
                        r.StartedAtUtc = startedAt;
                    }

                    if (newState.HasValue)
                    {
                        r.State = newState.Value;

                        if (newState == BuildAndDeployRequestState.Completed)
                        {
                            r.CompletedAtUtc = DateTime.UtcNow;
                        }
                    }

                    session.Update(r);

                    await session.SaveChangesAsync();
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                }
            }
        }

        public async Task<bool> CreateIISDeployInfo(IISBuildTargetConfigurationCreateModel m)
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var e = ToEntity(m);

                    session.Store(e);

                    await session.SaveChangesAsync();

                    return true;
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return false;
                }
            }
        }

        public Project ToEntity(ProjectCreateModel m)
        {
            return new Project
            {
                Name = m.Name,
                BranchName = m.BranchName,
                RepositoryType = m.RepositoryType,
                RepositoryUrl = m.RepositoryUrl,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = m.CreatedByUserId,
            };
        }

        public BuildTarget ToEntity(BuildTargetCreateModel m)
        {
            return new BuildTarget
            {
                Name = m.Name,
                Type = m.Type,
                DeploymentType = m.DeploymentType,
                TargetRelativePath = m.TargetRelativePath,
                ProjectId = m.ProjectId,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = m.CreatedByUserId
            };
        }

        public RepositoryCursor ToEntity(RepositoryCursorCreateModel m)
        {
            return new RepositoryCursor
            {
                ProjectId = m.ProjectId,
                CreatedAtUtc = DateTime.UtcNow,
                Info = m.Selected,
                Name = m.Name,
                CreatedByUserId = m.CreatedByUserId,
            };
        }

        public IISDeployInfo ToEntity(IISBuildTargetConfigurationCreateModel m)
        {
            return new IISDeployInfo
            {
                ProjectId = m.ProjectId,
                BuildTargetId = m.BuildTargetId,
                Info = m.Configuration,
                CreatedByUserId = m.CreatedByUserId,
            };
        }
    }
}