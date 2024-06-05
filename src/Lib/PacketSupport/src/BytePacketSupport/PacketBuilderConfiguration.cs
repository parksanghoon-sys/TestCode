using BytePacketSupport.Enums;

namespace BytePacketSupport
{
    public class PacketBuilderConfiguration
    {
        public EEndian DefaultEndian { get; set; } = EEndian.LITTLE;
    }
}
