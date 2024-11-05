using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace Microservice.Persistence.DatabaseContext;

public interface IDbProvider
{
    DbContext CreateDbContext(string options);
}
public class MicrosoftDatabaseContextFactory : IDbProvider
{
    private readonly IConfiguration _configuration;

    public MicrosoftDatabaseContextFactory(IConfiguration configuration)        
    {
        _configuration = configuration;
    }

    public DbContext CreateDbContext(string options)
    {
        var db = new DbContextOptionsBuilder();
        switch (options)
        {
            case "Order":
                db = new DbContextOptionsBuilder<OrderDbContext>();
                break;

            case "Product":
                db = new DbContextOptionsBuilder<ProductDbContext>();
                break;
        };
        throw new NotImplementedException();        
    }
}
