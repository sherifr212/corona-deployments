using Serilog;
using System;
using System.Threading;

namespace CoronaDeployments.Core.Runner
{
    public class BackgroundRunner : IDisposable
    {
        private readonly Timer timer;
        private readonly TimeSpan periodicTime = TimeSpan.FromSeconds(5);

        public BackgroundRunner(string name, IRunnerAction impl, IRunnerActionPayload payload)
        {
            Name = name;
            ActionImplementation = impl;
            ActionImplementationParameters = payload;

            timer = new Timer(RunImpl, null, Timeout.Infinite, Timeout.Infinite);
            IsStarted = false;
        }

        public string Name { get; }
        public IRunnerAction ActionImplementation { get; }
        public IRunnerActionPayload ActionImplementationParameters { get; }
        public bool IsStarted { get; private set; }
        public bool IsRunning { get; private set; } = false;


        public void Start()
        {
            if (IsStarted == false)
            {
                // Start immediatly
                timer.Change(0, Timeout.Infinite);
                IsStarted = true;
            }
        }

        private async void RunImpl(object state)
        {
            Log.Information($"Runner {Name} is running...");

            // Do our work here.
            try
            {
                IsRunning = true;

                await ActionImplementation.Implementation(ActionImplementationParameters);
            }
            catch (Exception exp)
            {
                Log.Error(exp, string.Empty);
            }
            finally
            {
                IsRunning = false;
            }

            Log.Information($"Runner {Name} is taking a break...");

            timer.Change((int)periodicTime.TotalMilliseconds, Timeout.Infinite);
        }

        public void Stop()
        {
            if (IsStarted)
            {
                timer?.Change(Timeout.Infinite, Timeout.Infinite);
                timer?.Dispose();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}