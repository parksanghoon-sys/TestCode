using WebApplication1.Models;
using WebApplication1.Store;

namespace WebApplication1.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IStore<User> _store;

        public UserRepository(IStore<User> store)
        {
            this._store = store;
        }
        public void Add(User user)
        {
            _store.Add(user);
        }

        public Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default)
        {
            _store.Add(user);
            return Task.FromResult(user);
        }

        public void Delete(int id)
        {
            _store.Delete(id);
        }

        public IEnumerable<User> GetAll()
        {
            return _store.GetAll();
        }

        public User GetById(int id)
        {
            return _store.GetById(id);
        }

        public void Update(User user)
        {
            _store.Update(user);
        }
    }
}
