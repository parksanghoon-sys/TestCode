using Microservice.Application.Repositories;
using Microservice.Doamin;
using Microservice.Persistence.DatabaseContext;

namespace Microservice.Persistence.Repositories
{
    public class OrderRepository : GenericRepository<OrderModel>, IOrderRepository
    {
        public OrderRepository(MicrosoftDatabaseContext microsoftDatabaseContext) : base(microsoftDatabaseContext)
        {
        }
    }

}
