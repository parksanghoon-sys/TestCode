using Microservice.Doamin;
using Microservice.Doamin.Common;
using Microsoft.EntityFrameworkCore;

namespace OrderService.API.DatabaseContext;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        :base(options)
    {
        
    }
    public DbSet<OrderModel> OrderModels { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entity in base.ChangeTracker.Entries<BaseEntity>()
            .Where(q => q.State == EntityState.Added || q.State == EntityState.Modified))
        {
            entity.Entity.DateModified = DateTime.UtcNow;

            if (entity.State == EntityState.Added)
            {
                entity.Entity.DateModified = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
    /// <summary>
    /// Do any database initialization required.
    /// </summary>
    /// <returns>A task that completes when the database is initialized</returns>
    public async Task InitializeDatabaseAsync()
    {
        await Database.EnsureCreatedAsync().ConfigureAwait(false);
    }
}
