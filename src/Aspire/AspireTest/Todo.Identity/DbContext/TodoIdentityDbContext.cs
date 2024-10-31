using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todo.Domain;

namespace Todo.Identity.DbContext
{
    public class TodoIdentityDbContext : IdentityDbContext<ApplicationUser>
    {
        public TodoIdentityDbContext(DbContextOptions<TodoIdentityDbContext> option)
            :base(option) 
        {
            
        }
        public async Task InitializeDatabaseAsync()
        {
            await Database.EnsureCreatedAsync().ConfigureAwait(false);
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(TodoIdentityDbContext).Assembly);
        }
    }

}
