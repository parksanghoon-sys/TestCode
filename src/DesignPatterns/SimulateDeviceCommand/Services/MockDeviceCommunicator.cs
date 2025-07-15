using SimulateDeviceCommand.Enums;
using SimulateDeviceCommand.Interfaces;
using SimulateDeviceCommand.Models;

namespace SimulateDeviceCommand.Services;

public class MockDeviceCommunicator : IDeviceCommunicator
{
    private readonly Random _random = new Random();
    private readonly IBinaryMessageSerializer _serializer = new BinaryMessageSerializer();
    public bool IsConnected { get; private set; } = false;    

    public async Task<DeviceResponse> SendAsync(byte[] data, TimeSpan timeout, CancellationToken cancellationToken)
    {
        // 실제 통신 시뮬레이션
        await Task.Delay(1000, cancellationToken);
        // 메시지 내용에 따라 다른 응답 (실제로는 장비 프로토콜에 따라 결정)
        try
        {
            // 받은 메시지 파싱
            var receivedMessage = _serializer.Deserialize(data);

            Console.WriteLine($"📨 수신: CMD=0x{receivedMessage.Cmd:X2}, Length={receivedMessage.Length}, Checksum=0x{receivedMessage.Checksum:X4}");

            // 체크섬 검증
            if (!receivedMessage.IsChecksumValid())
            {
                return CreateErrorResponse(receivedMessage.Cmd, ResponseCodes.ERROR_CHECKSUM, "Checksum error");
            }
            // 명령별 응답 생성
            return receivedMessage.Cmd switch
            {
                CommandCodes.CONNECT => CreateSuccessResponse(receivedMessage.Cmd, "Connected"),
                CommandCodes.CONFIGURE => CreateConfigureResponse(receivedMessage.Cmd),
                CommandCodes.START_MEASUREMENT => CreateSuccessResponse(receivedMessage.Cmd, "Measurement started"),
                CommandCodes.GET_RESULT => CreateResultResponse(receivedMessage.Cmd),
                CommandCodes.DISCONNECT => CreateSuccessResponse(receivedMessage.Cmd, "Disconnected"),
                _ => CreateErrorResponse(receivedMessage.Cmd, ResponseCodes.ERROR_INVALID_COMMAND, "Unknown command")
            };
        }
        catch (Exception ex)
        {
            return new DeviceResponse
            {
                Type = ResponseType.Error,
                Message = $"Parse error: {ex.Message}",
                RawData = data,
                Timestamp = DateTime.Now
            };
        }       
    }
    private DeviceResponse CreateSuccessResponse(byte originalCmd, string description)
    {
        var responseData = new byte[] { ResponseCodes.SUCCESS };
        var responseMessage = new DeviceMessage(originalCmd, responseData);
        var serializedResponse = _serializer.Serialize(responseMessage);

        Console.WriteLine($"📤 응답: SUCCESS - {description}");

        return new DeviceResponse
        {
            Type = ResponseType.Success,
            Message = description,
            RawData = serializedResponse,
            ParsedMessage = responseMessage,
            Timestamp = DateTime.Now
        };
    }

    private DeviceResponse CreateConfigureResponse(byte originalCmd)
    {
        // 30% 확률로 실패 (재시도 테스트용)
        var success = _random.NextDouble() > 0.8;
        var responseCode = success ? ResponseCodes.SUCCESS : ResponseCodes.ERROR_DEVICE_BUSY;
        var responseData = new byte[] { responseCode };
        var responseMessage = new DeviceMessage(originalCmd, responseData);
        var serializedResponse = _serializer.Serialize(responseMessage);

        var description = success ? "Configuration successful" : "Device busy, retry needed";
        Console.WriteLine($"📤 응답: {(success ? "SUCCESS" : "ERROR")} - {description}");

        return new DeviceResponse
        {
            Type = success ? ResponseType.Success : ResponseType.Fail,
            Message = description,
            RawData = serializedResponse,
            ParsedMessage = responseMessage,
            Timestamp = DateTime.Now
        };
    }

    private DeviceResponse CreateResultResponse(byte originalCmd)
    {
        // 측정 결과 데이터 시뮬레이션 (온도값 예시)
        var temperature = (short)(250 + _random.Next(-50, 51)); // 25.0°C ± 5.0°C
        var responseData = new byte[3];
        responseData[0] = ResponseCodes.SUCCESS;
        responseData[1] = (byte)(temperature & 0xFF);      // 하위 바이트
        responseData[2] = (byte)((temperature >> 8) & 0xFF); // 상위 바이트

        var responseMessage = new DeviceMessage(originalCmd, responseData);
        var serializedResponse = _serializer.Serialize(responseMessage);

        var tempValue = temperature / 10.0;
        var description = $"Temperature: {tempValue:F1}°C";
        Console.WriteLine($"📤 응답: SUCCESS - {description}");

        return new DeviceResponse
        {
            Type = ResponseType.Success,
            Message = description,
            RawData = serializedResponse,
            ParsedMessage = responseMessage,
            Timestamp = DateTime.Now
        };
    }

    private DeviceResponse CreateErrorResponse(byte originalCmd, byte errorCode, string description)
    {
        var responseData = new byte[] { errorCode };
        var responseMessage = new DeviceMessage(originalCmd, responseData);
        var serializedResponse = _serializer.Serialize(responseMessage);

        Console.WriteLine($"📤 응답: ERROR(0x{errorCode:X2}) - {description}");

        return new DeviceResponse
        {
            Type = ResponseType.Error,
            Message = description,
            RawData = serializedResponse,
            ParsedMessage = responseMessage,
            Timestamp = DateTime.Now
        };
    }
}
