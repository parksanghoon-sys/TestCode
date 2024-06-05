using System;
using System.Collections.Generic;
using System.Text;

namespace BytePacketSupport
{
    public partial class PacketBuilder
    {
        private Dictionary<string, (int start, int count)> bytesKeyPoint = new Dictionary<string, (int start, int count)>();
        public PacketBuilder BeginSection(string key)
        {
            bytesKeyPoint.Add(key, (this._packetData.WrittenCount, 0));
            return this;
        }
        public PacketBuilder EndSection(string key)
        {
            if(this._packetData.WrittenCount == 0)
                return this;
            if(bytesKeyPoint.ContainsKey(key) == false)
                return this;

            bytesKeyPoint[key] = (bytesKeyPoint[key].start, this._packetData.WrittenCount  - bytesKeyPoint[key].start);
            return this;
        }
        private byte[] GetBytes(int start)
        {
            return this._packetData.WrittenSpan.Slice(start).ToArray();
        }
        private byte[] GetBytes(int start, int count)
        {
            return this._packetData.WrittenSpan.Slice(start, count).ToArray();
        }
    }
}
