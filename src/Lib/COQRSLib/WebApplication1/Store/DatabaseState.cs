using WebApplication1.Models;

namespace WebApplication1.Store
{
    // 데이터베이스 상태 구현체 (실제 DB 연결은 하지 않고 예시로만 구현)
    public class DatabaseState : IStore<User>
    {
        // 실제 구현에서는 DbContext 등을 사용하게 됨
        public User GetById(int id)
        {
            Console.WriteLine($"Getting user with ID {id} from database");
            // 실제 DB 조회 코드가 들어갈 자리
            return null;
        }

        public IEnumerable<User> GetAll()
        {
            Console.WriteLine("Getting all users from database");
            // 실제 DB 조회 코드가 들어갈 자리
            return new List<User>();
        }

        public void Add(User user)
        {
            Console.WriteLine($"Adding user with ID {user.Id} to database");
            // 실제 DB 삽입 코드가 들어갈 자리
        }

        public void Update(User user)
        {
            Console.WriteLine($"Updating user with ID {user.Id} in database");
            // 실제 DB 업데이트 코드가 들어갈 자리
        }

        public void Delete(int id)
        {
            Console.WriteLine($"Deleting user with ID {id} from database");
            // 실제 DB 삭제 코드가 들어갈 자리
        }
    }
}
