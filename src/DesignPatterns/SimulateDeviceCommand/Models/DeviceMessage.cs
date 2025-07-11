using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateDeviceCommand.Models;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
public struct DeviceMessage
{
    public byte Cmd;           // 명령 코드
    public byte Length;        // 데이터 길이
    public byte[] Data;        // 실제 데이터 (가변 길이)
    public short Checksum;     // 체크섬

    public DeviceMessage(byte cmd, byte[] data = null)
    {
        Cmd = cmd;
        Data = data ?? new byte[0];
        Length = (byte)Data.Length;
        Checksum = CalculateChecksum(cmd, Length, Data);
    }

    // 체크섬 계산 (CRC16 간단 버전)
    private static short CalculateChecksum(byte cmd, byte length, byte[] data)
    {
        int checksum = cmd + length;
        foreach (byte b in data)
        {
            checksum += b;
        }
        return (short)(checksum & 0xFFFF);
    }

    // 체크섬 검증
    public bool IsChecksumValid()
    {
        return Checksum == CalculateChecksum(Cmd, Length, Data);
    }
}

// 명령 코드 정의
public static class CommandCodes
{
    public const byte CONNECT = 0x01;
    public const byte DISCONNECT = 0x02;
    public const byte CONFIGURE = 0x10;
    public const byte START_MEASUREMENT = 0x20;
    public const byte STOP_MEASUREMENT = 0x21;
    public const byte GET_RESULT = 0x30;
    public const byte GET_STATUS = 0x31;
    public const byte RESET = 0xFF;
}

// 응답 코드 정의
public static class ResponseCodes
{
    public const byte SUCCESS = 0x00;
    public const byte ERROR_INVALID_COMMAND = 0x01;
    public const byte ERROR_INVALID_PARAMETER = 0x02;
    public const byte ERROR_DEVICE_BUSY = 0x03;
    public const byte ERROR_TIMEOUT = 0x04;
    public const byte ERROR_CHECKSUM = 0x05;
}
