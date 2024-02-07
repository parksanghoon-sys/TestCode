using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisSubscriberService.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisSubscriberService.Services
{
    public class RedisPubSub : IPubSub
    {
        private ConnectionMultiplexer _connection;
        private readonly ILogger<RedisPubSub> _logger;
        private readonly IHostApplicationLifetime _lifeTime;
        public RedisPubSub(ILogger<RedisPubSub> logger, IHostApplicationLifetime lifetime)
        {
            _connection = ConnectionMultiplexer.Connect(new ConfigurationOptions { EndPoints = { "localhost:7002" } });
            _logger = logger;
            var db = _connection.GetDatabase();
            var pong = db.Ping();
            Console.WriteLine(pong);
            _lifeTime = lifetime;
        }
        public void Subscribe(string topic)
        {
            var pubsub = _connection.GetSubscriber();
            pubsub.Subscribe(topic, ReceivedMessage);
            //System.Diagnostics.Debugger.Launch();
        }       
        public void Unsubscribe(string topic)
        {
            var pubsub = _connection.GetSubscriber();
            pubsub.Unsubscribe(topic, ReceivedMessage);            
        }
        public Task Publish(string topic, string message)
        {
            var pubsub = _connection.GetSubscriber();
            return pubsub.PublishAsync(topic, message);
        }
        private void ReceivedMessage(RedisChannel channel, RedisValue value)
        {
            var logMessage = $"channel: {channel}, message : {value}";
            _logger.LogInformation(logMessage);

            if (value == "q")
            {
                _lifeTime.StopApplication();
            }
        }
    }
}
