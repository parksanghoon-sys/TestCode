using Protocols.Abstractions.Logging;

namespace Protocols.Abstractions.Channels
{
    public interface IChannel : IDisposable
    {
        bool IsDisposed { get; }
        IChannelLogger Logger { get; set; }
        string Description { get; }
        void Write(byte[] bytes);
        byte Read(int timeout);
        IEnumerable<byte> Read(uint count, int timeout);
        IEnumerable<byte> ReadAllRemain();
        uint BytesToRead { get; }
    }
}
