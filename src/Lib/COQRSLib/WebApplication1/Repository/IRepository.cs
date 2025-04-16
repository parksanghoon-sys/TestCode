using WebApplication1.Models;

namespace WebApplication1.Repository
{
    public interface IRepository<T>
    {
        T GetById(int id);
        IEnumerable<T> GetAll();
        void Add(T user);
        void Update(T user);
        void Delete(int id);

        Task<T> CreateUserAsync(T user, CancellationToken cancellationToken = default);
    }
    public interface IUserRepository : IRepository<User>;    
}
