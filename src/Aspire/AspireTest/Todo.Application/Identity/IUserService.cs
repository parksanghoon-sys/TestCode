using Todo.Application.Identity.Model;

namespace Todo.Application.Identity
{
    public interface IUserService
    {
        Task<List<Employee>> GetEmployees();
        Task<Employee> GetEmployee(string userId);
        public string UserId { get; }
    }
}
