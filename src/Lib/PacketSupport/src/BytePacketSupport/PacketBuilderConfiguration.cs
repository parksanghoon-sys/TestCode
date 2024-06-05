using BytePacketSupport.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BytePacketSupport
{
    public class PacketBuilderConfiguration
    {
        public EEndian DefaultEndian { get; set; } = EEndian.LITTLE;
    }
}
