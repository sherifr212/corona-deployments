using CoronaDeployments.Core.Models;
using Marten;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.Repositories
{
    public interface ISecurityRepository
    {
        public Task<(string Error, Session)> Login(string username, string password);
    }

    public class SecurityRepository : ISecurityRepository
    {
        private readonly IDocumentStore _store;

        public SecurityRepository(IDocumentStore store)
        {
            _store = store;
        }

        public async Task<(string Error, Session)> Login(string username, string password)
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var lowered = username.ToLower();
                    var user = await session.Query<User>()
                        .Where(x => x.Username == lowered)
                        .FirstOrDefaultAsync();

                    if (user == null)
                    {
                        return ("User is not found.", null);
                    }

                    if (PasswordUtils.Verify(user.PasswordHashed, password) == false)
                    {
                        return ("Authentication faild.", null);
                    }

                    return (null, new Session { User = user });
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return ("Unexpected Error.", null);
                }
            }
        }
    }
}
