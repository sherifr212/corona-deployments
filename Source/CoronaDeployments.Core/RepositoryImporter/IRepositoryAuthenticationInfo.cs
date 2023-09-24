using CoronaDeployments.Core.Build;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.RepositoryImporter
{
    public interface IRepositoryAuthenticationInfo
    {
        Task<bool> Validate();

        SourceCodeRepositoryType Type { get; set; }
    }
}
