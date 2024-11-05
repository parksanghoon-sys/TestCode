using Microservice.Application.Repositories;
using Microservice.Doamin;
using Microservice.Persistence.DatabaseContext;
using ProductService.API.DatabaseContext;

namespace Microservice.Persistence.Repositories
{
    public class ProductRepository : GenericRepository<ProductModel>, IProductRepository
    {
        public ProductRepository(ProductDbContext microsoftDatabaseContext) : base(microsoftDatabaseContext)
        {
        }
    }

}
