using Confluent.Kafka;

namespace Microservice.Infrastructure.Models
{
    public class KafkaConfig
    {
        public string GroupId { get; set; }
        public string BootstapSevers { get; set; }
        public AutoOffsetReset AutoOffsetReset { get; set; }

    }

}
