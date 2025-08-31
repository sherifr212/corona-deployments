using System;
using System.IO;
using System.Threading.Tasks;

namespace CoronaDeployments.Core
{
    public class AppConfigurationProvider
    {
        public Task<AppConfiguration> Get()
        {
            // Use environment variable or fallback to cross-platform default
            var baseDirectory = Environment.GetEnvironmentVariable("CORONA_BASE_DIRECTORY") ??
                               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "repository");
            
            return Task.FromResult(new AppConfiguration(baseDirectory));
        }
    }

    public sealed record AppConfiguration(string BaseDirectory);
}
