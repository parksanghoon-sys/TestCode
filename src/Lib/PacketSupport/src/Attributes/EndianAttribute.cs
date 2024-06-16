using BytePacketSupport.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BytePacketSupport.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EndianAttribute : Attribute
    {
        public EEndian Endian { get; set; } = EEndian.LITTLE;
        public EndianAttribute(EEndian eEndian)
        {
            Endian = eEndian;
        }
    }
}
