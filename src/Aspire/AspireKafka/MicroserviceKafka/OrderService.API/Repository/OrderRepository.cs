using Microservice.Application.Repositories;
using Microservice.Doamin;
using OrderService.API.DatabaseContext;

namespace Microservice.Persistence.Repositories
{
    public class OrderRepository : GenericRepository<OrderModel>, IOrderRepository
    {
        public OrderRepository(OrderDbContext microsoftDatabaseContext) : base(microsoftDatabaseContext)
        {
        }
    }

}
