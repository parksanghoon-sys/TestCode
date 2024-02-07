using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisSubscriberService.Interfaces
{
    public interface IPubSub
    {
        void Subscribe(string topic);
        void Unsubscribe(string topic);
        Task Publish(string topic, string message);
    }
}
