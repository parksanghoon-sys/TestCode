using Confluent.Kafka;
using Microservice.Application.Kafka;
using Microservice.Infrastructure.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Infrastructure.KafkaService
{
    public class KafkaProducer : IKafkaProducer<string, string>
    {
        private readonly IProducer<string, string> _producer;
        private readonly KafkaConfig _config;

        //public KafkaProducer()
        //{
        //    var config = new ConsumerConfig
        //    {
        //        GroupId = "order-group",
        //        BootstrapServers = "localhost:9092",
        //        AutoOffsetReset = AutoOffsetReset.Earliest
        //    };
        //    _producer = new ProducerBuilder<string, string>(config).Build();
        //}
        public KafkaProducer(IOptions<KafkaConfig> options)
        {
            _config = options.Value;
            var config = new ConsumerConfig
            {
                GroupId = _config.GroupId,
                BootstrapServers = _config.BootstapSevers,
                AutoOffsetReset = _config.AutoOffsetReset,
            };
            _producer = new ProducerBuilder<string, string>(config).Build();

        }
        public async Task ProduceAsync(string topic, Message<string, string> message)
        {
            await _producer.ProduceAsync(topic, message);
        }
    }

}
