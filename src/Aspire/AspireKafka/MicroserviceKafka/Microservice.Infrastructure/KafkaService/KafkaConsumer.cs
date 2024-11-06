using Confluent.Kafka;
using Microservice.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Threading;


namespace Microservice.Infrastructure.KafkaService
{
    public interface IKafkaConsumProvidor<T>
    {
        string Topic { get; set; }
        Action<T> Action { get; set; }
    }
    public class KafkaConsumer: BackgroundService         
    {
        private readonly KafkaConfig _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IKafkaConsumProvidor<int> _kafkaConsumProvidor;
        private readonly ConsumerConfig _kafkaConfig;                

        public KafkaConsumer(IOptions<KafkaConfig> options, 
            IServiceScopeFactory scopeFactory,
            IKafkaConsumProvidor<int> kafkaConsumProvidor)
        {
            _config = options.Value;
            _scopeFactory = scopeFactory;
            _kafkaConsumProvidor = kafkaConsumProvidor;
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
                if (_kafkaConfig is not null)
                {
                    using var consumer = new ConsumerBuilder<string, string>(_kafkaConfig).Build();
                    if(_kafkaConsumProvidor is not null)
                    {
                        consumer.Subscribe(_kafkaConsumProvidor.Topic);

                        while (stoppingToken.IsCancellationRequested == false)
                        {
                            var consumResult = consumer.Consume(stoppingToken);
                            var order = JsonConvert.DeserializeObject<OrderMessage>(consumResult.Message.Value);
                            using var scop = _scopeFactory.CreateScope();

                            _kafkaConsumProvidor.Action?.Invoke(order!.ProductId);
                        }
                    }
                    consumer.Close();                  
                }
                
            },stoppingToken);
        }
        //public async Task ConsumeAsync(string topic, CancellationToken cancellationToken = default)
        //{
        //    if (_kafkaConfig is not null)
        //    {
        //        using var consumer = new ConsumerBuilder<string, string>(_kafkaConfig).Build();
        //        consumer.Subscribe(_topic);

        //        while(cancellationToken.IsCancellationRequested == false)
        //        {
        //            var consumResult = consumer.Consume(cancellationToken);
        //            var product = JsonConvert.DeserializeObject<ProductMessage>(consumResult.Message.Value);
        //            using var scop = _scopeFactory.CreateScope();

        //            var dbContext = scop.ServiceProvider.GetRequiredService<T>();
                    
        //        }
        //    }
            
        //}
    }

}
