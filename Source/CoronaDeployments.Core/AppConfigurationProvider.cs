using System.Threading.Tasks;

namespace CoronaDeployments.Core
{
    public class AppConfigurationProvider
    {
        public async Task<AppConfiguration> Get()
        {
            return new AppConfiguration(@"C:\Repository");
        }
    }

    public sealed record AppConfiguration(string BaseDirectory);
}
