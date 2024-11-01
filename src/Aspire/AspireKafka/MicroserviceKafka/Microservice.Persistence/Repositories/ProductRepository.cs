using Microservice.Application.Repositories;
using Microservice.Doamin;
using Microservice.Persistence.DatabaseContext;

namespace Microservice.Persistence.Repositories
{
    public class ProductRepository : GenericRepository<ProductModel>, IProductRepository
    {
        public ProductRepository(MicrosoftDatabaseContext microsoftDatabaseContext) : base(microsoftDatabaseContext)
        {
        }
    }

}
