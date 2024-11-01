using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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

        app.MapPost("/login", Login);
    }

    private static async Task<AuthResponse> Login(AuthRequest request, [FromServices] IAuthService authService)
    {
        return await authService.LoginAsync(request);
    }

    private static async Task<List<Employee>> GetUsers([FromServices] IAuthService authService)
    {
        return await authService.GetEmployees();
    }
}
