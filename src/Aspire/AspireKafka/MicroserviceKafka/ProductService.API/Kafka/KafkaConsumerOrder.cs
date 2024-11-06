using Microservice.Application.Repositories;
using Microservice.Infrastructure.KafkaService;
using ProductService.API.DatabaseContext;

namespace ProductService.API.Kafka
{
    public class KafkaConsumerProduct : IKafkaConsumProvidor<int>
    {
        private readonly IServiceProvider _serviceProvider;

        public KafkaConsumerProduct(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string Topic { get => "order-topic"; set => throw new NotImplementedException(); }
        public Action<int> Action
        {
            get => (async (int id) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IProductRepository>();

                var product = await dbContext.GetByIdAsync(id);
                if (product is not null)
                {
                    product.Quantity -= 1;
                    await dbContext.UpdateAsync(product);
                }
            });
            set => throw new NotImplementedException();
        }
    }
}
