using Microservice.Doamin;
using Microservice.Doamin.Common;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Persistence.DatabaseContext;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {

    }
    public DbSet<ProductModel> ProductModels { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);
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
