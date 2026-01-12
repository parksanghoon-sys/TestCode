namespace TcpSimulator.Infrastructure.Protocol;

public static class Crc32Calculator
{
    private static readonly uint[] Table = BuildTable();

    public static uint Calculate(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return 0;

        uint crc = 0xFFFFFFFF;

        for (int i = 0; i < data.Length; i++)
        {
            byte index = (byte)(crc ^ data[i]);
            crc = (crc >> 8) ^ Table[index];
        }

        return ~crc;
    }

    private static uint[] BuildTable()
    {
        const uint polynomial = 0xEDB88320;
        var table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
            {
                crc = (crc & 1) == 1 ? (crc >> 1) ^ polynomial : crc >> 1;
            }
            table[i] = crc;
        }

        return table;
    }
}
