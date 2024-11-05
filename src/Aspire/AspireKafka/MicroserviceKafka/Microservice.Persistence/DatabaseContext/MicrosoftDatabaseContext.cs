using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace Microservice.Persistence.DatabaseContext;

public interface IDbProvider
{
    //DbContext CreateDbContext(string options);
}
public class MicrosoftDatabaseContextFactory : IDbProvider
{
    private readonly IConfiguration _configuration;

    public MicrosoftDatabaseContextFactory(IConfiguration configuration)        
    {
        _configuration = configuration;
    }

   
}
