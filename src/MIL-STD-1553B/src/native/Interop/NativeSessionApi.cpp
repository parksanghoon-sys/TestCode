#include "Interop/NativeSessionApi.h"

#include "Application/BusControllerService.h"
#include "Application/BusMonitorService.h"
#include "Infrastructure/SimulatorVendorChannel.h"
#include "Infrastructure/VendorSdkBusAdapter.h"

#include <chrono>
#include <codecvt>
#include <cwchar>
#include <iomanip>
#include <locale>
#include <memory>
#include <mutex>
#include <optional>
#include <regex>
#include <sstream>
#include <string>
#include <string_view>
#include <unordered_map>
#include <vector>

namespace
{
using MilStd1553::Application::BusControllerService;
using MilStd1553::Application::BusMonitorService;
using MilStd1553::Application::IClock;
using MilStd1553::Domain::BusLine;
using MilStd1553::Domain::CommandWord;
using MilStd1553::Domain::DataWord;
using MilStd1553::Domain::HealthSnapshot;
using MilStd1553::Domain::TelemetryEvent;
using MilStd1553::Domain::TelemetryEventType;
using MilStd1553::Infrastructure::DefaultVendorErrorMapper;
using MilStd1553::Infrastructure::SimulatorVendorChannel;
using MilStd1553::Infrastructure::VendorChannelConfiguration;
using MilStd1553::Infrastructure::VendorSdkBusAdapter;
using MilStd1553::Interop::NativeCallStatus;

struct ParsedScheduleDefinition final
{
    std::wstring name;
    int periodMs{ 0 };
    std::uint8_t rtAddress{ 0 };
    std::uint8_t subAddress{ 1 };
    bool transmit{ false };
};

struct ParsedScenarioDefinition final
{
    std::wstring name;
    int channelId{ 0 };
    BusLine activeBus{ BusLine::A };
    std::vector<ParsedScheduleDefinition> schedules;
};

class SessionClock final : public IClock
{
public:
    /// <summary>
    /// 세션 내부 이벤트에 사용할 다음 time-tag를 반환합니다.
    /// </summary>
    /// <returns>현재 세션 time-tag입니다.</returns>
    std::chrono::microseconds GetCurrentTimeTag() const override
    {
        const auto currentTimeTag = currentTimeTag_;
        currentTimeTag_ += std::chrono::microseconds{ 25 };
        return currentTimeTag;
    }

private:
    mutable std::chrono::microseconds currentTimeTag_{ 1000 };
};

class NativeSessionRuntime final
{
public:
    /// <summary>
    /// 시나리오 정의를 기준으로 실제 네이티브 세션 런타임을 생성합니다.
    /// </summary>
    /// <param name="scenario">실행할 시나리오 정의입니다.</param>
    explicit NativeSessionRuntime(ParsedScenarioDefinition scenario)
        : scenario_(std::move(scenario))
        , vendorConfiguration_(BuildVendorConfiguration(scenario_))
        , adapter_(vendorChannel_, errorMapper_, vendorConfiguration_)
        , controller_(adapter_, clock_, monitor_, BuildInitialHealth(scenario_))
    {
        ConfigureSimulator();
        ExecuteInitialSchedulePass();
    }

    /// <summary>
    /// 현재 세션의 health snapshot을 반환합니다.
    /// </summary>
    /// <returns>현재 health snapshot입니다.</returns>
    [[nodiscard]] const HealthSnapshot& GetHealthSnapshot() const noexcept
    {
        return controller_.GetHealthSnapshot();
    }

    /// <summary>
    /// 현재 세션의 활성 버스를 전환합니다.
    /// </summary>
    /// <param name="busLine">전환할 버스입니다.</param>
    void SwitchBus(const BusLine busLine)
    {
        controller_.SwitchBus(busLine);
    }

    /// <summary>
    /// 아직 Host에 전달되지 않은 pending telemetry를 반환합니다.
    /// </summary>
    /// <returns>pending telemetry 목록입니다.</returns>
    [[nodiscard]] std::vector<TelemetryEvent> GetPendingTelemetry() const
    {
        const auto& events = monitor_.GetEvents();
        if (telemetryCursor_ >= events.size())
        {
            return {};
        }

        return std::vector<TelemetryEvent>(
            events.begin() + static_cast<std::ptrdiff_t>(telemetryCursor_),
            events.end());
    }

    /// <summary>
    /// 현재까지의 telemetry를 Host에 전달 완료한 것으로 표시합니다.
    /// </summary>
    void MarkPendingTelemetryDelivered() noexcept
    {
        telemetryCursor_ = monitor_.GetEvents().size();
    }

private:
    /// <summary>
    /// 시나리오 기준 초기 health snapshot을 생성합니다.
    /// </summary>
    /// <param name="scenario">실행 시나리오입니다.</param>
    /// <returns>초기 health snapshot입니다.</returns>
    [[nodiscard]] static HealthSnapshot BuildInitialHealth(
        const ParsedScenarioDefinition& scenario) noexcept
    {
        auto health = HealthSnapshot::CreateDefault();
        health.SwitchActiveBus(scenario.activeBus);
        return health;
    }

    /// <summary>
    /// 벤더 bridge adapter에 전달할 초기 channel 구성을 만듭니다.
    /// </summary>
    /// <param name="scenario">실행 시나리오입니다.</param>
    /// <returns>초기 channel 구성입니다.</returns>
    [[nodiscard]] static VendorChannelConfiguration BuildVendorConfiguration(
        const ParsedScenarioDefinition& scenario) noexcept
    {
        VendorChannelConfiguration configuration;
        configuration.channelId = scenario.channelId;
        configuration.initialBus = scenario.activeBus;
        configuration.enableBusController = true;
        configuration.enableRemoteTerminal = true;
        configuration.enableBusMonitor = true;
        if (!scenario.schedules.empty())
        {
            configuration.remoteTerminalAddress = scenario.schedules.front().rtAddress;
        }

        configuration.responseTimeout = std::chrono::microseconds{ 1000 };
        return configuration;
    }

    /// <summary>
    /// 시뮬레이터 RT 버퍼에 deterministic 초기 데이터를 채웁니다.
    /// </summary>
    void ConfigureSimulator()
    {
        for (const auto& schedule : scenario_.schedules)
        {
            const auto seedWord = BuildSeedDataWord(schedule);
            vendorChannel_.SetStatusWord(
                schedule.rtAddress,
                MilStd1553::Domain::StatusWord(
                    schedule.rtAddress,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false));
            vendorChannel_.SetSubAddressData(schedule.rtAddress, schedule.subAddress, { seedWord });
            vendorChannel_.SetBitWord(schedule.rtAddress, seedWord);
            vendorChannel_.SetVectorWord(schedule.rtAddress, seedWord);
        }
    }

    /// <summary>
    /// MVP 세션 시작 시 schedule을 한 번 순회하며 초기 telemetry를 생성합니다.
    /// </summary>
    void ExecuteInitialSchedulePass()
    {
        for (const auto& schedule : scenario_.schedules)
        {
            controller_.Execute(BuildTransferRequest(schedule));
        }
    }

    /// <summary>
    /// 시나리오 스케줄을 실제 전송 요청으로 변환합니다.
    /// </summary>
    /// <param name="schedule">변환할 스케줄입니다.</param>
    /// <returns>실행할 전송 요청입니다.</returns>
    [[nodiscard]] MilStd1553::Domain::TransferRequest BuildTransferRequest(
        const ParsedScheduleDefinition& schedule) const
    {
        const auto commandWord = CommandWord(
            schedule.rtAddress,
            schedule.transmit,
            schedule.subAddress,
            static_cast<std::uint8_t>(1U));

        if (schedule.transmit)
        {
            return MilStd1553::Domain::TransferRequest{
                commandWord,
                scenario_.activeBus,
                {},
            };
        }

        return MilStd1553::Domain::TransferRequest{
            commandWord,
            scenario_.activeBus,
            { BuildSeedDataWord(schedule) },
        };
    }

    /// <summary>
    /// RT 데이터를 재현하기 위한 deterministic seed word를 생성합니다.
    /// </summary>
    /// <param name="schedule">대상 스케줄입니다.</param>
    /// <returns>seed 데이터 워드입니다.</returns>
    [[nodiscard]] static DataWord BuildSeedDataWord(
        const ParsedScheduleDefinition& schedule) noexcept
    {
        return DataWord{
            static_cast<std::uint16_t>(
                ((static_cast<std::uint16_t>(schedule.rtAddress) & 0x001FU) << 8U)
                | (static_cast<std::uint16_t>(schedule.subAddress) & 0x001FU)),
        };
    }

    ParsedScenarioDefinition scenario_;
    SessionClock clock_{};
    BusMonitorService monitor_{};
    SimulatorVendorChannel vendorChannel_{};
    DefaultVendorErrorMapper errorMapper_{};
    VendorChannelConfiguration vendorConfiguration_{};
    VendorSdkBusAdapter adapter_;
    BusControllerService controller_;
    std::size_t telemetryCursor_{ 0U };
};

std::mutex g_sessionMutex;
std::unordered_map<std::wstring, std::unique_ptr<NativeSessionRuntime>> g_sessions;
std::uint64_t g_nextSessionId{ 1 };

int GetRequiredCapacity(const std::wstring& value)
{
    return static_cast<int>(value.size()) + 1;
}

void SetRequiredCapacity(int* target, const int requiredCapacity)
{
    if (target != nullptr)
    {
        *target = requiredCapacity;
    }
}

void ClearWideBuffer(wchar_t* buffer, const int capacity)
{
    if (buffer != nullptr && capacity > 0)
    {
        buffer[0] = L'\0';
    }
}

std::wstring ConvertUtf8ToWide(const std::string& value)
{
    std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
    return converter.from_bytes(value);
}

std::wstring EscapeJson(const std::wstring_view value)
{
    std::wstring escaped;
    escaped.reserve(value.size());

    for (const wchar_t character : value)
    {
        switch (character)
        {
        case L'\\':
            escaped += L"\\\\";
            break;
        case L'\"':
            escaped += L"\\\"";
            break;
        case L'\n':
            escaped += L"\\n";
            break;
        case L'\r':
            escaped += L"\\r";
            break;
        case L'\t':
            escaped += L"\\t";
            break;
        default:
            escaped += character;
            break;
        }
    }

    return escaped;
}

std::wstring SerializeBusLine(const BusLine busLine)
{
    return busLine == BusLine::A ? L"A" : L"B";
}

std::wstring SerializeEventType(const TelemetryEventType eventType)
{
    switch (eventType)
    {
    case TelemetryEventType::MessageFrame:
        return L"MessageFrame";
    case TelemetryEventType::BusSwitch:
        return L"BusSwitch";
    case TelemetryEventType::AutoFailover:
        return L"AutoFailover";
    default:
        return L"Unknown";
    }
}

std::wstring SerializeDataWords(const std::vector<DataWord>& dataWords)
{
    std::wostringstream builder;
    builder << L'[';

    for (std::size_t index = 0; index < dataWords.size(); ++index)
    {
        if (index > 0)
        {
            builder << L',';
        }

        builder << dataWords[index].value;
    }

    builder << L']';
    return builder.str();
}

std::wstring SerializeHealthSnapshot(const HealthSnapshot& health)
{
    std::wostringstream builder;
    builder
        << L"{\"activeBus\":\"" << SerializeBusLine(health.activeBus) << L"\""
        << L",\"standbyBus\":\"" << SerializeBusLine(health.standbyBus) << L"\""
        << L",\"timeoutCount\":" << health.timeoutCount
        << L",\"retryCount\":" << health.retryCount
        << L",\"consecutiveTimeoutCount\":" << health.consecutiveTimeoutCount
        << L",\"autoFailoverCount\":" << health.autoFailoverCount
        << L",\"degraded\":" << (health.degraded ? L"true" : L"false")
        << L"}";
    return builder.str();
}

std::wstring SerializeTelemetryEvents(const std::vector<TelemetryEvent>& events)
{
    std::wostringstream builder;
    builder << L'[';

    for (std::size_t index = 0; index < events.size(); ++index)
    {
        if (index > 0)
        {
            builder << L',';
        }

        const auto& event = events[index];
        builder
            << L"{\"type\":\"" << SerializeEventType(event.type) << L"\""
            << L",\"timeTagMicros\":" << event.timeTag.count()
            << L",\"activeBus\":\"" << SerializeBusLine(event.activeBus) << L"\""
            << L",\"description\":\"" << EscapeJson(ConvertUtf8ToWide(event.description)) << L"\""
            << L",\"messageFrame\":";

        if (!event.messageFrame.has_value())
        {
            builder << L"null";
        }
        else
        {
            const auto& frame = event.messageFrame.value();
            builder
                << L"{\"commandWordRaw\":" << frame.commandWord.ToRaw()
                << L",\"statusWordRaw\":";

            if (frame.statusWord.has_value())
            {
                builder << frame.statusWord->ToRaw();
            }
            else
            {
                builder << L"null";
            }

            builder
                << L",\"dataWords\":" << SerializeDataWords(frame.dataWords)
                << L",\"busLine\":\"" << SerializeBusLine(frame.busLine) << L"\""
                << L",\"timeTagMicros\":" << frame.timeTag.count()
                << L"}";
        }

        builder << L'}';
    }

    builder << L']';
    return builder.str();
}

NativeCallStatus WriteWideStringToBuffer(
    const std::wstring& value,
    wchar_t* buffer,
    const int capacity,
    int* requiredCapacity)
{
    const auto requiredBufferCapacity = GetRequiredCapacity(value);
    SetRequiredCapacity(requiredCapacity, requiredBufferCapacity);

    if (buffer == nullptr || capacity <= 0)
    {
        return NativeCallStatus::InvalidArgument;
    }

    if (requiredBufferCapacity > capacity)
    {
        ClearWideBuffer(buffer, capacity);
        return NativeCallStatus::BufferTooSmall;
    }

    std::wmemcpy(buffer, value.c_str(), value.size());
    buffer[value.size()] = L'\0';
    return NativeCallStatus::Success;
}

bool TryExtractStringProperty(
    const std::wstring& source,
    const std::wstring_view propertyName,
    std::wstring* value)
{
    if (value == nullptr)
    {
        return false;
    }

    const std::wstring pattern =
        L"\\\"" + std::wstring(propertyName) + L"\\\"\\s*:\\s*\\\"([^\\\"]*)\\\"";
    const std::wregex regex(pattern);
    std::wsmatch match;
    if (!std::regex_search(source, match, regex) || match.size() < 2)
    {
        return false;
    }

    *value = match[1].str();
    return true;
}

bool TryExtractIntProperty(
    const std::wstring& source,
    const std::wstring_view propertyName,
    int* value)
{
    if (value == nullptr)
    {
        return false;
    }

    const std::wstring pattern =
        L"\\\"" + std::wstring(propertyName) + L"\\\"\\s*:\\s*(-?\\d+)";
    const std::wregex regex(pattern);
    std::wsmatch match;
    if (!std::regex_search(source, match, regex) || match.size() < 2)
    {
        return false;
    }

    *value = std::stoi(match[1].str());
    return true;
}

std::optional<BusLine> TryParseBusLine(const std::wstring_view value)
{
    if (value == L"A" || value == L"a")
    {
        return BusLine::A;
    }

    if (value == L"B" || value == L"b")
    {
        return BusLine::B;
    }

    return std::nullopt;
}

std::optional<std::wstring> TryExtractArrayBody(
    const std::wstring& source,
    const std::wstring_view propertyName)
{
    const auto propertyToken = L"\"" + std::wstring(propertyName) + L"\"";
    const auto propertyPosition = source.find(propertyToken);
    if (propertyPosition == std::wstring::npos)
    {
        return std::nullopt;
    }

    const auto arrayStart = source.find(L'[', propertyPosition + propertyToken.size());
    if (arrayStart == std::wstring::npos)
    {
        return std::nullopt;
    }

    bool insideString = false;
    bool escaping = false;
    int depth = 0;
    std::size_t bodyStart = std::wstring::npos;

    for (std::size_t index = arrayStart; index < source.size(); ++index)
    {
        const auto character = source[index];
        if (escaping)
        {
            escaping = false;
            continue;
        }

        if (insideString && character == L'\\')
        {
            escaping = true;
            continue;
        }

        if (character == L'"')
        {
            insideString = !insideString;
            continue;
        }

        if (insideString)
        {
            continue;
        }

        if (character == L'[')
        {
            ++depth;
            if (depth == 1)
            {
                bodyStart = index + 1;
            }

            continue;
        }

        if (character == L']')
        {
            --depth;
            if (depth == 0 && bodyStart != std::wstring::npos)
            {
                return source.substr(bodyStart, index - bodyStart);
            }
        }
    }

    return std::nullopt;
}

std::vector<std::wstring> SplitTopLevelJsonObjects(const std::wstring& arrayBody)
{
    std::vector<std::wstring> objects;
    bool insideString = false;
    bool escaping = false;
    int depth = 0;
    std::size_t objectStart = std::wstring::npos;

    for (std::size_t index = 0; index < arrayBody.size(); ++index)
    {
        const auto character = arrayBody[index];
        if (escaping)
        {
            escaping = false;
            continue;
        }

        if (insideString && character == L'\\')
        {
            escaping = true;
            continue;
        }

        if (character == L'"')
        {
            insideString = !insideString;
            continue;
        }

        if (insideString)
        {
            continue;
        }

        if (character == L'{')
        {
            if (depth == 0)
            {
                objectStart = index;
            }

            ++depth;
            continue;
        }

        if (character == L'}')
        {
            --depth;
            if (depth == 0 && objectStart != std::wstring::npos)
            {
                objects.emplace_back(arrayBody.substr(objectStart, index - objectStart + 1));
                objectStart = std::wstring::npos;
            }
        }
    }

    return objects;
}

std::optional<ParsedScenarioDefinition> TryParseScenarioDefinition(
    const std::wstring_view scenarioJson)
{
    const std::wstring source(scenarioJson);

    std::wstring activeBusValue;
    if (!TryExtractStringProperty(source, L"activeBus", &activeBusValue))
    {
        return std::nullopt;
    }

    const auto activeBus = TryParseBusLine(activeBusValue);
    if (!activeBus.has_value())
    {
        return std::nullopt;
    }

    int channelId = 0;
    if (!TryExtractIntProperty(source, L"channelId", &channelId) || channelId < 0)
    {
        return std::nullopt;
    }

    auto schedulesBody = TryExtractArrayBody(source, L"bcSchedules");
    if (!schedulesBody.has_value())
    {
        return std::nullopt;
    }

    const auto scheduleObjects = SplitTopLevelJsonObjects(*schedulesBody);
    if (scheduleObjects.empty())
    {
        return std::nullopt;
    }

    std::vector<ParsedScheduleDefinition> schedules;
    schedules.reserve(scheduleObjects.size());
    for (const auto& scheduleObject : scheduleObjects)
    {
        ParsedScheduleDefinition schedule;
        int periodMs = 0;
        int rtAddress = 0;
        int subAddress = 0;
        std::wstring directionValue;

        if (!TryExtractStringProperty(scheduleObject, L"name", &schedule.name)
            || !TryExtractIntProperty(scheduleObject, L"periodMs", &periodMs)
            || !TryExtractIntProperty(scheduleObject, L"rtAddress", &rtAddress)
            || !TryExtractIntProperty(scheduleObject, L"subAddress", &subAddress)
            || !TryExtractStringProperty(scheduleObject, L"direction", &directionValue))
        {
            return std::nullopt;
        }

        if (periodMs <= 0
            || rtAddress < 0 || rtAddress > 30
            || subAddress < 1 || subAddress > 30)
        {
            return std::nullopt;
        }

        if (directionValue == L"Transmit" || directionValue == L"transmit")
        {
            schedule.transmit = true;
        }
        else if (directionValue == L"Receive" || directionValue == L"receive")
        {
            schedule.transmit = false;
        }
        else
        {
            return std::nullopt;
        }

        schedule.periodMs = periodMs;
        schedule.rtAddress = static_cast<std::uint8_t>(rtAddress);
        schedule.subAddress = static_cast<std::uint8_t>(subAddress);
        schedules.push_back(std::move(schedule));
    }

    std::wstring scenarioName;
    if (!TryExtractStringProperty(source, L"name", &scenarioName) || scenarioName.empty())
    {
        scenarioName = L"기본시나리오";
    }

    ParsedScenarioDefinition scenario;
    scenario.name = std::move(scenarioName);
    scenario.channelId = channelId;
    scenario.activeBus = activeBus.value();
    scenario.schedules = std::move(schedules);
    return scenario;
}

std::wstring BuildSessionHandle(const std::uint64_t sessionId)
{
    std::wostringstream builder;
    builder << L"session-" << std::setw(4) << std::setfill(L'0') << sessionId;
    return builder.str();
}
}

extern "C"
{
int MilStd1553_OpenSession(
    const wchar_t* scenarioJson,
    wchar_t* sessionHandleBuffer,
    const int sessionHandleCapacity,
    int* requiredSessionHandleCapacity,
    wchar_t* healthJsonBuffer,
    const int healthJsonCapacity,
    int* requiredHealthJsonCapacity)
{
    try
    {
        if (scenarioJson == nullptr)
        {
            return static_cast<int>(NativeCallStatus::InvalidArgument);
        }

        const auto parsedScenario = TryParseScenarioDefinition(scenarioJson);
        if (!parsedScenario.has_value())
        {
            return static_cast<int>(NativeCallStatus::InvalidArgument);
        }

        auto runtime = std::make_unique<NativeSessionRuntime>(*parsedScenario);

        std::lock_guard lock(g_sessionMutex);
        const std::wstring sessionHandle = BuildSessionHandle(g_nextSessionId);
        const std::wstring healthJson = SerializeHealthSnapshot(runtime->GetHealthSnapshot());

        SetRequiredCapacity(requiredSessionHandleCapacity, GetRequiredCapacity(sessionHandle));
        SetRequiredCapacity(requiredHealthJsonCapacity, GetRequiredCapacity(healthJson));

        if (sessionHandleBuffer == nullptr || sessionHandleCapacity <= 0
            || healthJsonBuffer == nullptr || healthJsonCapacity <= 0)
        {
            return static_cast<int>(NativeCallStatus::InvalidArgument);
        }

        if (GetRequiredCapacity(sessionHandle) > sessionHandleCapacity
            || GetRequiredCapacity(healthJson) > healthJsonCapacity)
        {
            ClearWideBuffer(sessionHandleBuffer, sessionHandleCapacity);
            ClearWideBuffer(healthJsonBuffer, healthJsonCapacity);
            return static_cast<int>(NativeCallStatus::BufferTooSmall);
        }

        if (const auto sessionStatus = WriteWideStringToBuffer(
                sessionHandle,
                sessionHandleBuffer,
                sessionHandleCapacity,
                requiredSessionHandleCapacity);
            sessionStatus != NativeCallStatus::Success)
        {
            return static_cast<int>(sessionStatus);
        }

        if (const auto healthStatus = WriteWideStringToBuffer(
                healthJson,
                healthJsonBuffer,
                healthJsonCapacity,
                requiredHealthJsonCapacity);
            healthStatus != NativeCallStatus::Success)
        {
            return static_cast<int>(healthStatus);
        }

        g_sessions.emplace(sessionHandle, std::move(runtime));
        ++g_nextSessionId;
        return static_cast<int>(NativeCallStatus::Success);
    }
    catch (...)
    {
        return static_cast<int>(NativeCallStatus::InternalError);
    }
}

int MilStd1553_StopSession(const wchar_t* sessionHandle)
{
    try
    {
        if (sessionHandle == nullptr)
        {
            return static_cast<int>(NativeCallStatus::InvalidArgument);
        }

        std::lock_guard lock(g_sessionMutex);
        const auto iterator = g_sessions.find(sessionHandle);
        if (iterator == g_sessions.end())
        {
            return static_cast<int>(NativeCallStatus::SessionNotFound);
        }

        g_sessions.erase(iterator);
        return static_cast<int>(NativeCallStatus::Success);
    }
    catch (...)
    {
        return static_cast<int>(NativeCallStatus::InternalError);
    }
}

int MilStd1553_SwitchBus(const wchar_t* sessionHandle, const int busLine)
{
    try
    {
        if (sessionHandle == nullptr || (busLine != 0 && busLine != 1))
        {
            return static_cast<int>(NativeCallStatus::InvalidArgument);
        }

        std::lock_guard lock(g_sessionMutex);
        const auto iterator = g_sessions.find(sessionHandle);
        if (iterator == g_sessions.end())
        {
            return static_cast<int>(NativeCallStatus::SessionNotFound);
        }

        iterator->second->SwitchBus(busLine == 0 ? BusLine::A : BusLine::B);
        return static_cast<int>(NativeCallStatus::Success);
    }
    catch (...)
    {
        return static_cast<int>(NativeCallStatus::InternalError);
    }
}

int MilStd1553_GetHealthSnapshot(
    const wchar_t* sessionHandle,
    wchar_t* healthJsonBuffer,
    const int healthJsonCapacity,
    int* requiredHealthJsonCapacity)
{
    try
    {
        if (sessionHandle == nullptr)
        {
            return static_cast<int>(NativeCallStatus::InvalidArgument);
        }

        std::lock_guard lock(g_sessionMutex);
        const auto iterator = g_sessions.find(sessionHandle);
        if (iterator == g_sessions.end())
        {
            return static_cast<int>(NativeCallStatus::SessionNotFound);
        }

        const auto healthJson = SerializeHealthSnapshot(iterator->second->GetHealthSnapshot());
        return static_cast<int>(WriteWideStringToBuffer(
            healthJson,
            healthJsonBuffer,
            healthJsonCapacity,
            requiredHealthJsonCapacity));
    }
    catch (...)
    {
        return static_cast<int>(NativeCallStatus::InternalError);
    }
}

int MilStd1553_PollTelemetry(
    const wchar_t* sessionHandle,
    wchar_t* telemetryJsonBuffer,
    const int telemetryJsonCapacity,
    int* requiredTelemetryJsonCapacity)
{
    try
    {
        if (sessionHandle == nullptr)
        {
            return static_cast<int>(NativeCallStatus::InvalidArgument);
        }

        std::lock_guard lock(g_sessionMutex);
        const auto iterator = g_sessions.find(sessionHandle);
        if (iterator == g_sessions.end())
        {
            return static_cast<int>(NativeCallStatus::SessionNotFound);
        }

        const auto telemetryJson = SerializeTelemetryEvents(iterator->second->GetPendingTelemetry());
        const auto status = WriteWideStringToBuffer(
            telemetryJson,
            telemetryJsonBuffer,
            telemetryJsonCapacity,
            requiredTelemetryJsonCapacity);
        if (status == NativeCallStatus::Success)
        {
            iterator->second->MarkPendingTelemetryDelivered();
        }

        return static_cast<int>(status);
    }
    catch (...)
    {
        return static_cast<int>(NativeCallStatus::InternalError);
    }
}
}
