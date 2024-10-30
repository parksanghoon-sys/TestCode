using Microsoft.EntityFrameworkCore;
using Todo.API.Todo.Model;

namespace Todo.API.Todo
{
    public class TodoDbContext : IdentityDbContext
    {
        public DbSet<TodoItem> TodoItems { get; set; }
        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            :base(options)
        {
        }
        public async Task InitializeDatabaseAsync()
        {
            await Database.EnsureCreatedAsync().ConfigureAwait(false);
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(TodoDbContext).Assembly);
        }
    }
 
}
