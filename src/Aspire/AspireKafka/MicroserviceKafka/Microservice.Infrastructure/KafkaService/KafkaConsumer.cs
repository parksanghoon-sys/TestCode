using Confluent.Kafka;
using Microservice.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;


namespace Microservice.Infrastructure.KafkaService
{
    public class KafkaConsumer: BackgroundService
    {
        private readonly KafkaConfig _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConsumerConfig _kafkaConfig;

        public KafkaConsumer(IOptions<KafkaConfig> options, IServiceScopeFactory scopeFactory)
        {
            _config = options.Value;
            _scopeFactory = scopeFactory;

            _kafkaConfig = new ConsumerConfig
            {
                GroupId = _config.GroupId,
                BootstrapServers = _config.BootstapSevers,
                AutoOffsetReset = _config.AutoOffsetReset,
            };
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {

            });
        }
        public async Task ConsumeAsync(string topic, CancellationToken cancellationToken = default)
        {
            if (_kafkaConfig is not null)
            {
                using var consumer = new ConsumerBuilder<string, string>(_kafkaConfig).Build();
                consumer.Subscribe(topic);

                while(cancellationToken.IsCancellationRequested == false)
                {
                    var consumResult = consumer.Consume(cancellationToken);
                    var product = JsonConvert.DeserializeObject<ProductMessage>(consumResult.Message.Value);
                    using var scop = _scopeFactory.CreateScope();

                    //var dbContext = scop.ServiceProvider.GetRequiredService<>
                }
            }
            
        }
    }

}
