using Microservice.Doamin;
using Microservice.Doamin.Common;
using Microsoft.EntityFrameworkCore;

namespace ProductService.API.DatabaseContext;

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
    /// <summary>
    /// Do any database initialization required.
    /// </summary>
    /// <returns>A task that completes when the database is initialized</returns>
    public async Task InitializeDatabaseAsync()
    {
        await Database.EnsureCreatedAsync().ConfigureAwait(false);
    }
}
