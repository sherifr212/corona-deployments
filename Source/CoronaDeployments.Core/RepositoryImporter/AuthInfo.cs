using CoronaDeployments.Core.Build;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.RepositoryImporter
{
    public sealed class AuthInfo : IRepositoryAuthenticationInfo
    {
        public AuthInfo()
        {
        }

        public AuthInfo(string username, string password, SourceCodeRepositoryType type)
        {
            Username = username;
            Password = password;
            Type = type;
        }

        public string Username { get; set; }

        public string Password { get; set; }

        public SourceCodeRepositoryType Type { get; set; }

        public async Task<bool> Validate()
        {
            if (string.IsNullOrWhiteSpace(Username)) return false;

            if (string.IsNullOrWhiteSpace(Password)) return false;

            return true;
        }
    }
}
