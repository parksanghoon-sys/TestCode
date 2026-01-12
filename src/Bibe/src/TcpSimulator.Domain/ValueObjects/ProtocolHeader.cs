namespace TcpSimulator.Domain.ValueObjects;

public readonly record struct ProtocolHeader
{
    /// <summary>
    /// Magic number: 0xAA 0x55 in little-endian (0x55AA)
    /// </summary>
    public const ushort MagicNumber = 0x55AA;

    /// <summary>
    /// Header size: 2 (Magic) + 4 (Length) + 1 (Type) = 7 bytes
    /// </summary>
    public const int HeaderSize = 7;

    /// <summary>
    /// Checksum size: 4 bytes (CRC32)
    /// </summary>
    public const int ChecksumSize = 4;

    /// <summary>
    /// Minimum message size: Header + Checksum = 11 bytes
    /// </summary>
    public const int MinMessageSize = HeaderSize + ChecksumSize;

    public ushort Magic { get; init; }
    public int PayloadLength { get; init; }
    public byte Type { get; init; }

    public bool IsValid => Magic == MagicNumber && PayloadLength >= 0;
}
