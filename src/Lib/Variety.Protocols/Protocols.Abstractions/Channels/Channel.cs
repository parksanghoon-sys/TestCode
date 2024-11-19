using Protocols.Abstractions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Abstractions.Channels
{
    public abstract class Channel : IChannel
    {
        public bool IsDisposed { get; protected set; }
        public abstract void Dispose();

        public IChannelLogger Logger { get; set; }

        public abstract string Description { get; }

        public abstract uint BytesToRead { get;  }

        public abstract byte Read(int timeout);        

        public abstract IEnumerable<byte> Read(uint count, int timeout);       

        public abstract IEnumerable<byte> ReadAllRemain();        

        public abstract void Write(byte[] bytes);  
    }
}
