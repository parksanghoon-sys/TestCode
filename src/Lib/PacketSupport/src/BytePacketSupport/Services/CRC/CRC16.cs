using System.Buffers.Binary;
using System.Buffers;
using BytePacketSupport.Extentions;

namespace BytePacketSupport.BytePacketSupport.Services.CRC
{
    public enum CRC16Type
    {
        Classic,
        Modbus,
        CCITTxModem,
        DNP,
    }
    public class CRC16 : ErrorDetection
    {
        private readonly CRC16Type _type;
        private readonly bool _isLittleEndian;

        private static ushort[] crc_tab16;
        private static ushort[] crc16ModbusTable;
        private static ushort[] crc_tabccitt;
        private static ushort[] crc_tabdnp;
        private static readonly ushort P_16 = 0xA001;
        public CRC16(CRC16Type type = CRC16Type.Classic, bool isLittleEndian = true)
        {
            _type = type;
            _isLittleEndian = isLittleEndian;

            if (_type == CRC16Type.Classic)
            {
                if (crc_tab16 == null)
                    GenerateCRC16Table();
            }
            else if (_type == CRC16Type.Modbus)
            {
                if (crc16ModbusTable == null)
                    GenerateCRC16ModbusTable();
            }
            else if (_type == CRC16Type.CCITTxModem)
            {
                if (crc_tabccitt == null)
                    GenerateCCITTTable();
            }
            else if (_type == CRC16Type.DNP)
            {
                if (crc_tabdnp == null)
                    GenerateDNPTable();
            }

        }             
        public override ReadOnlySpan<byte> Compute(ReadOnlySpan<byte> data)
        {
            ushort crc = 0;
            if (_type == CRC16Type.Classic)
                crc = (ComputeCRC16(source));
            else if (_type == CRC16Type.Modbus)
                crc = (ComputeCRC16Modbus(source));
            else if (_type == CRC16Type.CCITTxModem)
                crc = (ComputeCCITTxModem(source));
            else if (_type == CRC16Type.DNP)
                crc = (ComputeDNP(source));

            ArrayBufferWriter<byte> retData = new ArrayBufferWriter<byte>();
            using var span = retData.Reserve(sizeof(ushort));
            {
                if (_isLittleEndian == true)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(span, crc);
                }
                else
                {
                    BinaryPrimitives.WriteUInt16BigEndian(span, crc);
                }
            }
            span.Dispose();
            return retData.ToArray();
        }

        public override string GetDetectionType()
        {
            return _type.ToString();
        }
        private ushort ComputeCRC16(ReadOnlySpan<byte> data)
        {
            ushort result = 0;
            if (data.Length <= 0)
                return result;

            for (int i = 0; i < data.Length; i++)
            {
                result = UpdateCRC16(result, data[i]);
            }

            return result;
        }
        private ushort UpdateCRC16(ushort crc, byte c)
        {
            var short_c = (ushort)(0x00ff & c);

            var tmp = (ushort)(crc ^ short_c);
            return (ushort)((crc >> 8) ^ crc_tab16[tmp & 0xff]);
        }
        private void GenerateCRC16Table()
        {
            crc_tab16 = new ushort[256];

            for (int i = 0; i < 256; i++)
            {
                ushort crc = 0;
                ushort c = (ushort)i;

                for (int j = 0; j < 8; j++)
                {
                    if (((crc ^ c) & 0x0001) != 0)
                        crc = (ushort)((crc >> 1) ^ P_16);
                    else
                        crc = (ushort)(crc >> 1);

                    c = (ushort)(c >> 1);
                }

                crc_tab16[i] = crc;
            }
        }
        private ushort ComputeCRC16Modbus(ReadOnlySpan<byte> data)
        {
            ushort crc = 0xFFFF;

            for (int i = 0; i < data.Length; i++)
            {
                byte index = (byte)(crc ^ data[i]);
                crc = (ushort)((crc >> 8) ^ crc16ModbusTable[index]);
            }
            return crc;
        }
        private ushort ComputeCCITTxModem(ReadOnlySpan<byte> data)
        {
            ushort result = 0;
            if (data.Length <= 0)
                return result;

            foreach (var d in data)
                result = UpdateCCITTxModem(result, d);

            return result;
        }
        private ushort UpdateCCITTxModem(ushort crc, byte c)
        {
            var short_c = (ushort)(0x00ff & c);

            var tmp = (ushort)(crc >> 8) ^ short_c;
            return (ushort)((crc << 8) ^ crc_tabccitt[tmp]);
        }
        private ushort ComputeDNP(ReadOnlySpan<byte> data)
        {
            ushort result = 0;
            if (data.Length <= 0)
                return result;

            for (int i = 0; i < data.Length; i++)
            {
                byte index = (byte)(result ^ data[i]);
                result = (ushort)((result >> 8) ^ crc_tabdnp[index]);
            }

            result = (ushort)(result ^ 0xffff);
            return (ushort)((ushort)((result & 0xff) << 8) | ((result >> 8) & 0xff));
        }
        private void GenerateCCITTTable()
        {
            crc_tabccitt = new ushort[256];

            for (int i = 0; i < 256; i++)
            {
                ushort crc = 0;
                ushort c = (ushort)(i << 8);

                for (int j = 0; j < 8; j++)
                {
                    if (((crc ^ c) & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc = (ushort)(crc << 1);

                    c = (ushort)(c << 1);
                }
                crc_tabccitt[i] = crc;
            }
        }
        private void GenerateCRC16ModbusTable()
        {
            crc16ModbusTable = new ushort[256];
            const ushort polynomial = 0xA001;

            for (ushort i = 0; i < 256; i++)
            {
                ushort value = i;

                for (int j = 0; j < 8; j++)
                {
                    if ((value & 1) == 1)
                    {
                        value = (ushort)((value >> 1) ^ polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                }

                crc16ModbusTable[i] = value;
            }
        }
        private void GenerateCCITTTable()
        {
            crc_tabccitt = new ushort[256];

            for (int i = 0; i < 256; i++)
            {
                ushort crc = 0;
                ushort c = (ushort)(i << 8);

                for (int j = 0; j < 8; j++)
                {
                    if (((crc ^ c) & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc = (ushort)(crc << 1);

                    c = (ushort)(c << 1);
                }
                crc_tabccitt[i] = crc;
            }
        }
        private void GenerateDNPTable()
        {
            crc_tabdnp = new ushort[256];
            const ushort p_dnp = 0xA6BC;

            for (int i = 0; i < 256; i++)
            {
                ushort crc = 0;
                ushort c = (ushort)i;

                for (int j = 0; j < 8; j++)
                {

                    if (((crc ^ c) & 0x0001) != 0)
                        crc = (ushort)((crc >> 1) ^ p_dnp);
                    else
                        crc = (ushort)(crc >> 1);

                    c = (ushort)(c >> 1);
                }
                crc_tabdnp[i] = crc;
            }
        }
    }
}
