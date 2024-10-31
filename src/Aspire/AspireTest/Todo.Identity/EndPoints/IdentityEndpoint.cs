using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Todo.Application.Identity;
using Todo.Application.Identity.Model;

namespace Todo.Identity.EndPoints;

public static class IdentityEndpoint
{
    public static void AddIdentityEndpoints(this WebApplication app)
    {
        app.MapGet("/identity", GetUsers)
            .WithName("GetUsers");            
    }

    private static async Task<List<Employee>> GetUsers([FromServices] IAuthService authService)
    {
        return await authService.GetEmployees();
    }
}
