using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisSubscriberService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace RedisSubscriberService.Services
{
    public class SubscribeService : ISubscribeService, IHostedService, IDisposable
    {
        private readonly IPubSub _pubSub;
        private readonly ILogger<SubscribeService> _logger;
        private string _topic = "WindowsServiceTest";
        public SubscribeService(IPubSub pubSub, ILogger<SubscribeService> logger)
        {
            _pubSub = pubSub;
            _logger = logger;
        }
        public Task Process()
        {
            _pubSub.Subscribe(_topic);
            _logger.LogTrace("Redis topic subscribed : {topic}", _topic);
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            _pubSub.Unsubscribe(_topic);

            _logger.LogTrace("~Redis topic unsubscribed : {topic}", _topic);            
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Process();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();
            return Task.CompletedTask;
        }
    }
}
