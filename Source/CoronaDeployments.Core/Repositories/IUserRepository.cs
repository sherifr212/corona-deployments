using CoronaDeployments.Core.Models;
using CoronaDeployments.Core.Models.Mvc;
using Marten;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.Repositories
{
    public interface IUserRepository
    {
        public Task<string> Create(UserCreateModel model);
    }

    public class UserRepository : IUserRepository
    {
        private IDocumentStore _store;

        public UserRepository(IDocumentStore store)
        {
            _store = store;
        }

        public async Task<string> Create(UserCreateModel model)
        {
            using (var session = _store.OpenSession())
            {
                try
                {
                    var e = ToEntity(model);

                    session.Store(e);

                    await session.SaveChangesAsync();

                    return model.GetPassword();
                }
                catch (Exception exp)
                {
                    Log.Error(exp, string.Empty);
                    return null;
                }
            }
        }

        private User ToEntity(UserCreateModel m)
        {
            Log.Information($"Password for {m.Name} {m.GetPassword()}");

            return new User
            {
                Name = m.Name,
                Username = m.Username.ToLower(),
                PasswordHashed = m.GetPasswordHashed(),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
        }
    }
}
