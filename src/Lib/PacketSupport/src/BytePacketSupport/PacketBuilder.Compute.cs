using BytePacketSupport.BytePacketSupport.Services;
using BytePacketSupport.BytePacketSupport.Services.Checksum;
using BytePacketSupport.BytePacketSupport.Services.CheckSum;
using BytePacketSupport.BytePacketSupport.Services.CRC;
using BytePacketSupport.Extentions;

namespace BytePacketSupport
{
    public partial class PacketBuilder
    {
        private void Compute(ErrorDetection detection, byte[] data)
        {
            byte[] errorcheck = detection.Compute(data).ToArray();
            this.AppendBytes(errorcheck);
        }
        public PacketBuilder Compute(CheckSum8Type type)
        {
            Compute(new CheckSum8(type), this._packetData.ToArray());
            return this;
        }
        public PacketBuilder Compute(CheckSum16Type type)
        {
            bool isLittleEndia = GetendianType();
            Compute(new CheckSum16(type), this._packetData.ToArray());
            return this;
        }
        public PacketBuilder Compute(CheckSum16Type type, bool isEndian)
        {            
            Compute(new CheckSum16(type, isEndian), this._packetData.ToArray());
            return this;
        }

        public PacketBuilder Compute(CheckSum8Type type, int start)
        {
            Compute(new CheckSum8(type), GetBytes(start));
            return this;
        }
        public PacketBuilder Compute(CheckSum8Type type, int start, int count)
        {
            Compute(new CheckSum8(type), GetBytes(start, count));
            return this;
        }
        public PacketBuilder Compute(CRC8Type type)
        {
            Compute(new CRC8(type), this._packetData.ToArray());
            return this;
        }

        public PacketBuilder Compute(CRC8Type type, int start)
        {
            Compute(new CRC8(type), GetBytes(start));
            return this;
        }
        public PacketBuilder Compute(CRC8Type type, int start, int count)
        {
            Compute(new CRC8(type), GetBytes(start, count));
            return this;
        }
   
        public PacketBuilder Compute(CRC16Type type)
        {
            bool isLittleEndia = GetendianType();
            Compute(new CRC16(type, isLittleEndia), this._packetData.ToArray());
            return this;
        }

        public PacketBuilder Compute(CRC16Type type, int start)
        {
            bool isLittleEndia = GetendianType();

            Compute(new CRC16(type, isLittleEndia), GetBytes(start));
            return this;
        }

        public PacketBuilder Compute(CRC16Type type, int start, int count)
        {
            bool isLittleEndia = GetendianType();

            Compute(new CRC16(type, isLittleEndia), GetBytes(start, count));

            return this;
        }

        public PacketBuilder Compute(CRC32Type type)
        {
            bool isLittleEndia = GetendianType();

            Compute(new CRC32(type, isLittleEndia), this._packetData.ToArray());
            return this;
        }

        public PacketBuilder Compute(CRC32Type type, int start)
        {
            bool isLittleEndia = GetendianType();

            Compute(new CRC32(type, isLittleEndia), GetBytes(start));
            return this;
        }

        public PacketBuilder Compute(CRC32Type type, int start, int count)
        {
            bool isLittleEndia = GetendianType();

            Compute(new CRC32(type, isLittleEndia), GetBytes(start, count));
            return this;
        }
        public PacketBuilder Compute(string key, CheckSum8Type type)
        {
            if (_packetData.WrittenCount == 0)
                return this;
            if (bytesKeyPoint.ContainsKey(key) == false)
                return this;

            Compute(type, bytesKeyPoint[key].start, bytesKeyPoint[key].count);
            return this;
        }
        public PacketBuilder Compute(string key, CRC8Type type)
        {
            if (this._packetData.WrittenCount == 0)
                return this;

            if (bytesKeyPoint.ContainsKey(key) == false)
                return this;

            this.Compute(type, bytesKeyPoint[key].start, bytesKeyPoint[key].count);
            return this;
        }
        public PacketBuilder Compute(string key, CRC16Type type)
        {
            if (this._packetData.WrittenCount == 0)
                return this;
            if (bytesKeyPoint.ContainsKey(key) == false)
                return this;

            this.Compute(type, bytesKeyPoint[key].start, bytesKeyPoint[key].count);
            return this;
        }

        public PacketBuilder Compute(string key, CRC32Type type)
        {
            if (this._packetData.WrittenCount == 0)
                return this;
            if (bytesKeyPoint.ContainsKey(key) == false)
                return this;

            this.Compute(type, bytesKeyPoint[key].Item1, bytesKeyPoint[key].count);
            return this;
        }
        private bool GetendianType()
        {
            if (BitConverter.IsLittleEndian == true)
            {
                if (_endianType == Enums.EEndian.BIG || _endianType == Enums.EEndian.LITTLEBYTESWAP)
                    return true;
            }
            else
            {
                if (_endianType == Enums.EEndian.LITTLE || _endianType == Enums.EEndian.BIGBYTESWAP)
                    return true;
            }
            return false;
        }
    }
}
