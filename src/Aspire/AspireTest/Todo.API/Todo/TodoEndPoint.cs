
using Microsoft.AspNetCore.Http.HttpResults;

namespace Todo.API.Todo
{
    public static class TodoEndPoint
    {
        public static void AddTodoEndpoints(this WebApplication app)
        {
            app.MapGet("/todo", GetTodo)
                .WithName("GetTodo")
                .WithOpenApi(x => new Microsoft.OpenApi.Models.OpenApiOperation(x)
                {
                    Summary = "My Todo List"
                });
        }

        private static async Task<Ok<string>> GetTodo()
        {
            return TypedResults.Ok("Hellow");
        }
    }
}
