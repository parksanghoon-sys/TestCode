using WebApplication1.Models;

namespace WebApplication1.Store
{
    // 인메모리 상태 구현체
    public class InMemoryState : IStore<User>
    {
        private static Dictionary<int, User> _users = new Dictionary<int, User>()
        {
            { 1, new User(1, "김철수", "chulsoo.kim@example.com", true) },
            { 2, new User(2, "이영희", "younghee.lee@example.com", true) },
            { 3, new User(3, "박민준", "minjun.park@example.com", true) },
            { 5, new User(5, "강지훈", "jihoon.kang@example.com", true) },
            { 6, new User(6, "최다은", "daeun.choi@example.com", true) },
            { 7, new User(7, "윤서준", "seojun.yoon@example.com", false) },

        };

        public InMemoryState()
        {        
        }
        public User GetById(int id)
        {
            if (_users.TryGetValue(id, out var user))
            {
                return user;
            }
            return null;
        }

        public IEnumerable<User> GetAll()
        {
            return _users.Values;
        }

        public void Add(User user)
        {
            if (!_users.ContainsKey(user.Id))
            {
                _users.Add(user.Id, user);
            }
            else
            {
                throw new InvalidOperationException($"User with ID {user.Id} already exists.");
            }
        }

        public void Update(User user)
        {
            if (_users.ContainsKey(user.Id))
            {
                _users[user.Id] = user;
            }
            else
            {
                throw new InvalidOperationException($"User with ID {user.Id} not found.");
            }
        }

        public void Delete(int id)
        {
            if (_users.ContainsKey(id))
            {
                _users.Remove(id);
            }
            else
            {
                throw new InvalidOperationException($"User with ID {id} not found.");
            }
        }
    }
}
