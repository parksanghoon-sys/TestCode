using Confluent.Kafka;

namespace Microservice.Application.Kafka
{
    public interface IKafkaProducer<T, TRESULT>
    {
        Task ProduceAsync(string topic, Message<T, TRESULT> message);
    }
}
