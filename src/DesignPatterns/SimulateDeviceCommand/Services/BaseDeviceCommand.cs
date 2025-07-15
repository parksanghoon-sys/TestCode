using SimulateDeviceCommand.Enums;
using SimulateDeviceCommand.Interfaces;
using SimulateDeviceCommand.Models;

namespace SimulateDeviceCommand.Services;

// =============================================
// Template Method Pattern - 기본 명령 클래스
// =============================================

public abstract class BaseDeviceCommand : IDeviceCommand
{
    protected readonly IBinaryMessageSerializer _serializer;
    protected readonly IRetryStrategy _retryStrategy;

    public abstract string Name { get; }
    public CommandState State { get; protected set; } = CommandState.Ready;
    public virtual int MaxRetries => 3;
    public virtual TimeSpan Timeout => TimeSpan.FromSeconds(10);
    public abstract Func<DeviceResponse, bool> ResponseValidator { get; }  // 각 커맨드에서 구현

    protected BaseDeviceCommand(
        IBinaryMessageSerializer serializer = null,
        IRetryStrategy retryStrategy = null)
    {
        _serializer = serializer ?? new BinaryMessageSerializer();
        _retryStrategy = retryStrategy ?? new ExponentialBackoffRetryStrategy(TimeSpan.FromSeconds(1));
    }
    public async Task<bool> ExecuteAsync(IDeviceCommunicator communicator, CancellationToken cancellationToken)
    {
       var retryCount = 0;

        while(retryCount <= MaxRetries)
        {
            try
            {
                State = CommandState.Sending;

                // 1. 바이너리 메시지 준비 및 직렬화
                var message = GetMessage();
                var serializedData = _serializer.Serialize(message);
                Console.WriteLine($"🔄 [{Name}] 명령 전송: CMD=0x{message.Cmd:X2}, Length={message.Length}");

                // 2. 장비로 메시지 전송
                State = CommandState.WaitingResponse;
                var response = await communicator.SendAsync(serializedData, Timeout, cancellationToken);
                // 3. 응답 파싱
                if (response.RawData != null && response.ParsedMessage == null)
                {
                    try
                    {
                        response.ParsedMessage = _serializer.Deserialize(response.RawData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ [{Name}] 응답 파싱 실패: {ex.Message}");
                        response.Type = ResponseType.Error;
                    }
                }
                // 4. 응답 검증 - 커맨드별 검증 함수 사용
                if (ValidateResponse(response))
                {
                    State = CommandState.Success;
                    await OnSuccessAsync(response);
                    return true;
                }

                // 5. 실패 시 재시도 여부 결정
                if (_retryStrategy.ShouldRetry(retryCount, MaxRetries, response))
                {
                    State = CommandState.Retrying;
                    retryCount++;
                    await OnRetryAsync(retryCount, response);
                    await _retryStrategy.DelayAsync(retryCount, cancellationToken);
                }
                else
                {
                    State = CommandState.Failed;
                    await OnFailureAsync(response);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                State = CommandState.Cancelled;
                return false;
            }
            catch (Exception ex)
            {
                State = CommandState.Failed;
                await OnExceptionAsync(ex);
                return false;
            }
        }
        State = CommandState.Failed;
        return false;
    }
    public void Reset()
    {
        State = CommandState.Ready;
    }
    // 추상 메서드 - 구체적인 명령에서 구현
    public abstract DeviceMessage GetMessage();
    // 가상 메서드들 - 필요시 오버라이드
    protected virtual bool ValidateResponse(DeviceResponse response)
    {
        return ResponseValidator?.Invoke(response) ?? false;
    }
    protected virtual Task OnSuccessAsync(DeviceResponse response)
    {
        Console.WriteLine($"✅ [{Name}] 성공: {response.Message}");
        return Task.CompletedTask;
    }

    protected virtual Task OnRetryAsync(int retryCount, DeviceResponse response)
    {
        Console.WriteLine($"🔄 [{Name}] 재시도 {retryCount}/{MaxRetries}: {response.Message}");
        return Task.CompletedTask;
    }

    protected virtual Task OnFailureAsync(DeviceResponse response)
    {
        Console.WriteLine($"❌ [{Name}] 실패: {response.Message}");
        return Task.CompletedTask;
    }

    protected virtual Task OnExceptionAsync(Exception exception)
    {
        Console.WriteLine($"🚨 [{Name}] 예외 발생: {exception.Message}");
        return Task.CompletedTask;
    }
}
// =============================================
// 구체적인 명령 클래스들 (Command Pattern)
// =============================================

public class ConnectCommand : BaseDeviceCommand
{
    public override string Name => "장비 연결";

    private readonly byte _deviceId;

    public ConnectCommand(byte deviceId) : base()
    {
        _deviceId = deviceId;
    }

    // 연결 명령 전용 응답 검증 함수
    public override Func<DeviceResponse, bool> ResponseValidator => (response) =>
    {
        if (response.ParsedMessage == null) return false;

        var message = response.ParsedMessage.Value;

        // 체크섬 검증
        if (!message.IsChecksumValid())
        {
            Console.WriteLine($"🔍 [{Name}] 체크섬 오류 검출");
            return false;
        }

        // 연결 응답 코드 확인
        if (message.Data.Length > 0 && message.Data[0] == ResponseCodes.SUCCESS)
        {
            Console.WriteLine($"🔍 [{Name}] 연결 성공 응답 확인");
            return true;
        }

        Console.WriteLine($"🔍 [{Name}] 연결 실패: 응답코드 = 0x{(message.Data.Length > 0 ? message.Data[0] : 0):X2}");
        return false;
    };

    public override DeviceMessage GetMessage()
    {
        var data = new byte[] { _deviceId }; // 장비 ID를 데이터로 전송
        return new DeviceMessage(CommandCodes.CONNECT, data);
    }
}

public class ConfigureCommand : BaseDeviceCommand
{
    public override string Name => "장비 설정";
    public override int MaxRetries => 5; // 설정은 더 많이 재시도

    private readonly ushort _sampleRate;
    private readonly byte _resolution;
    private readonly byte _mode;

    public ConfigureCommand(ushort sampleRate, byte resolution, byte mode) : base()
    {
        _sampleRate = sampleRate;
        _resolution = resolution;
        _mode = mode;
    }

    // 설정 명령 전용 응답 검증 함수 (재시도에 특화)
    public override Func<DeviceResponse, bool> ResponseValidator => (response) =>
    {
        if (response.ParsedMessage == null) return false;

        var message = response.ParsedMessage.Value;

        // 체크섬 검증
        if (!message.IsChecksumValid())
        {
            Console.WriteLine($"🔍 [{Name}] 체크섬 오류 검출");
            return false;
        }

        if (message.Data.Length > 0)
        {
            var responseCode = message.Data[0];

            if (responseCode == ResponseCodes.SUCCESS)
            {
                Console.WriteLine($"🔍 [{Name}] 설정 성공 (샘플레이트: {_sampleRate}Hz, 해상도: {_resolution}bit)");
                return true;
            }
            else if (responseCode == ResponseCodes.ERROR_DEVICE_BUSY)
            {
                Console.WriteLine($"🔍 [{Name}] 장비 사용 중 - 재시도 필요");
                return false; // 재시도 가능한 오류
            }
            else
            {
                Console.WriteLine($"🔍 [{Name}] 설정 실패: 응답코드 = 0x{responseCode:X2}");
                return false;
            }
        }

        return false;
    };

    public override DeviceMessage GetMessage()
    {
        var data = new byte[4];
        data[0] = (byte)(_sampleRate & 0xFF);        // 샘플레이트 하위
        data[1] = (byte)((_sampleRate >> 8) & 0xFF); // 샘플레이트 상위
        data[2] = _resolution;                       // 해상도
        data[3] = _mode;                            // 모드

        return new DeviceMessage(CommandCodes.CONFIGURE, data);
    }
}

public class StartMeasurementCommand : BaseDeviceCommand
{
    public override string Name => "측정 시작";

    private readonly byte _measurementType;
    private readonly ushort _duration;

    public StartMeasurementCommand(byte measurementType, ushort duration) : base()
    {
        _measurementType = measurementType;
        _duration = duration;
    }

    // 측정 시작 명령 전용 검증 함수
    public override Func<DeviceResponse, bool> ResponseValidator => (response) =>
    {
        if (response.ParsedMessage == null) return false;

        var message = response.ParsedMessage.Value;

        // 체크섬 검증
        if (!message.IsChecksumValid()) return false;

        if (message.Data.Length > 0 && message.Data[0] == ResponseCodes.SUCCESS)
        {
            var measurementTypeName = _measurementType switch
            {
                0x10 => "온도",
                0x20 => "습도",
                0x30 => "압력",
                _ => $"타입{_measurementType:X2}"
            };

            Console.WriteLine($"🔍 [{Name}] {measurementTypeName} 측정 시작됨 (지속시간: {_duration}초)");
            return true;
        }

        return false;
    };

    public override DeviceMessage GetMessage()
    {
        var data = new byte[3];
        data[0] = _measurementType;                 // 측정 타입
        data[1] = (byte)(_duration & 0xFF);         // 지속시간 하위
        data[2] = (byte)((_duration >> 8) & 0xFF);  // 지속시간 상위

        return new DeviceMessage(CommandCodes.START_MEASUREMENT, data);
    }
}

public class GetResultCommand : BaseDeviceCommand
{
    public override string Name => "결과 조회";

    private readonly byte _resultType;
    private double _lastMeasuredValue = 0; // 측정값 저장용

    public GetResultCommand(byte resultType = 0x01) : base()
    {
        _resultType = resultType;
    }

    // 결과 조회 명령 전용 검증 함수 (데이터 파싱 포함)
    public override Func<DeviceResponse, bool> ResponseValidator => (response) =>
    {
        if (response.ParsedMessage == null) return false;

        var message = response.ParsedMessage.Value;

        // 체크섬 검증
        if (!message.IsChecksumValid()) return false;

        if (message.Data.Length >= 3 && message.Data[0] == ResponseCodes.SUCCESS)
        {
            // 온도 데이터 파싱 (Little Endian)
            var rawValue = (short)(message.Data[1] | (message.Data[2] << 8));
            _lastMeasuredValue = rawValue / 10.0;

            Console.WriteLine($"🔍 [{Name}] 측정 결과 파싱 완료: {_lastMeasuredValue:F1}°C");

            // 측정값 범위 검증 (예: -50°C ~ 150°C)
            if (_lastMeasuredValue >= -50.0 && _lastMeasuredValue <= 150.0)
            {
                Console.WriteLine($"🔍 [{Name}] 측정값이 유효 범위 내에 있음");
                return true;
            }
            else
            {
                Console.WriteLine($"🔍 [{Name}] 측정값이 범위를 벗어남: {_lastMeasuredValue:F1}°C");
                return false;
            }
        }

        Console.WriteLine($"🔍 [{Name}] 결과 데이터 형식 오류");
        return false;
    };

    public double GetLastMeasuredValue() => _lastMeasuredValue;

    public override DeviceMessage GetMessage()
    {
        var data = new byte[] { _resultType }; // 결과 타입
        return new DeviceMessage(CommandCodes.GET_RESULT, data);
    }

    protected override Task OnSuccessAsync(DeviceResponse response)
    {
        Console.WriteLine($"🌡️ [{Name}] 최종 측정 결과: {_lastMeasuredValue:F1}°C");
        return base.OnSuccessAsync(response);
    }
}

public class DisconnectCommand : BaseDeviceCommand
{
    public override string Name => "연결 해제";

    // 연결 해제 명령 전용 검증 함수
    public override Func<DeviceResponse, bool> ResponseValidator => (response) =>
    {
        if (response.ParsedMessage == null) return false;

        var message = response.ParsedMessage.Value;

        // 체크섬 검증
        if (!message.IsChecksumValid()) return false;

        if (message.Data.Length > 0 && message.Data[0] == ResponseCodes.SUCCESS)
        {
            Console.WriteLine($"🔍 [{Name}] 연결 해제 완료");
            return true;
        }

        // 연결 해제는 실패해도 크게 문제없으므로 관대하게 처리
        Console.WriteLine($"🔍 [{Name}] 연결 해제 응답 무시 (이미 연결 해제됨)");
        return true;
    };

    public override DeviceMessage GetMessage()
    {
        return new DeviceMessage(CommandCodes.DISCONNECT, new byte[0]); // 데이터 없음
    }
}
