using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todo.Application.Identity.Model;

namespace Todo.Application.Identity
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(AuthRequest request);
        Task<RegistrationResponse> Register(RegistrationRequest request);
        Task<List<Employee>> GetEmployees();
    }
}
