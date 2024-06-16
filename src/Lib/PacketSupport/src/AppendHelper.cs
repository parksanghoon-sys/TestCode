using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BytePacketSupport
{
    public static partial class AppendHelper
    {
        public static byte[] AppendByte(this byte b, byte AppendByte) => new byte[] {b, AppendByte };
        public static byte[] AppendByte(this IEnumerable<byte> list, byte AppendByte) => list.Append<byte>(AppendByte).ToArray();
        public static byte[] AppendBytes(this byte b, IEnumerable<byte> AppendBytes)
        {
            byte[] TotalBytes = new byte[1 + AppendBytes.Count()];
            TotalBytes[0] = b;

            Buffer.BlockCopy(AppendBytes.ToArray(),0,TotalBytes,1,AppendBytes.Count());

            return TotalBytes;
        }
        public static byte[] AppendBytes(this IEnumerable<byte> bs, IEnumerable<byte> AppenBytes)
        {
            byte[] ToTalBytes = new byte[bs.Count() + AppenBytes.Count()];

            Buffer.BlockCopy(bs.ToArray(), 0, ToTalBytes, 0, bs.Count());
            Buffer.BlockCopy(AppenBytes.ToArray(), 0, ToTalBytes, bs.Count(), AppenBytes.Count());

            return ToTalBytes;
        }
        public static byte[] AppendBytes(this IEnumerable<byte> bs, IEnumerable<byte> AppenBytes, int offset, int count)
        {
            byte[] ToTalBytes = new byte[bs.Count() + AppenBytes.Count()];

            Buffer.BlockCopy(bs.ToArray(), 0, ToTalBytes, 0, bs.Count());
            Buffer.BlockCopy(AppenBytes.ToArray(), offset, ToTalBytes, bs.Count(), count);

            return ToTalBytes;
        }
    }
}
