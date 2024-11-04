using Microservice.Doamin;
using Microservice.Doamin.Common;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Persistence.DatabaseContext;

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
}
