using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CoronaDeployments.Core.Runner;
using CoronaDeployments.Core.Repositories;
using System.Collections.Generic;
using System.Linq;
using CoronaDeployments.Core.Models;

namespace CoronaDeployments.Core.HostedServices
{
    public class CoreHostedService : IHostedService
    {
        private Timer timer;
        private readonly IBackgroundTaskQueue taskQueue;
        private readonly TimeSpan periodicInterval;
        private readonly IServiceProvider services;
        private readonly IProjectRepository projectRepo;
        private readonly List<BackgroundRunner> runners;

        public CoreHostedService(IBackgroundTaskQueue taskQueue, IServiceProvider services, IProjectRepository projectRepo)
        {
            this.taskQueue = taskQueue;

            this.periodicInterval = TimeSpan.FromSeconds(5);

            this.services = services;
            this.projectRepo = projectRepo;
            this.runners = new List<BackgroundRunner>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Information($"{nameof(CoreHostedService)} is running.");

            timer = new Timer(DoWork, null, 0, -1); // Start immediatly without periodic signaling.
        }

        private async Task EnsureAllProjectsAreAllocated()
        {
            var projects = await projectRepo.GetAll();
            if (projects != null)
            {
                var notAllocatedProjects = new List<Project>(projects)
                    .Where(x => this.runners.Any(y => y.Name == x.Name) == false)
                    .ToList();
                foreach (var p in notAllocatedProjects)
                {
                    Log.Information($"A new project is discovered named {p.Name}");

                    var newRunner = new BackgroundRunner(p.Name, new BuildAndDeployAction(), new BuildAndDeployActionPayload 
                    { 
                        ProjectId = p.Id,
                        ProjectRepository = projectRepo,
                        ServiceProvider = services,
                    });

                    this.runners.Add(newRunner);
                    newRunner.Start();
                }
            }
        }

        private async void DoWork(object state)
        {
            // Disable timer.
            timer.Change(-1, -1);

            // Inner loop.
            {
                try
                {
                    await EnsureAllProjectsAreAllocated();

                    //var workItem = await _taskQueue.DequeueAsync(CancellationToken.None);
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                }
            }

            // Re-enable the timer.
            timer.Change((int)periodicInterval.TotalMilliseconds, -1);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information($"{nameof(CoreHostedService)} is stopping.");

            runners.ForEach(x => x.Stop());

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }

    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new ConcurrentQueue<Func<CancellationToken, Task>>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }
    }
}