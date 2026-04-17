#include "Interop/NativeSessionApi.h"

#include "Domain/TransferTypes.h"

#include <chrono>
#include <cwchar>
#include <iomanip>
#include <mutex>
#include <optional>
#include <sstream>
#include <string>
#include <string_view>
#include <unordered_map>
#include <vector>

namespace
{
using MilStd1553::Domain::BusLine;
using MilStd1553::Domain::HealthSnapshot;
using MilStd1553::Domain::TelemetryEvent;
using MilStd1553::Domain::TelemetryEventType;
using MilStd1553::Interop::NativeCallStatus;

struct SessionState final
{
    HealthSnapshot health{ HealthSnapshot::CreateDefault() };
    std::vector<TelemetryEvent> pendingEvents;
    std::uint64_t nextTimeTagMicros{ 100 };
};

std::mutex g_sessionMutex;
std::unordered_map<std::wstring, SessionState> g_sessions;
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
    default:
        return L"Unknown";
    }
}

std::wstring SerializeDataWords(const std::vector<MilStd1553::Domain::DataWord>& dataWords)
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
            << L",\"description\":\"" << EscapeJson(std::wstring(event.description.begin(), event.description.end())) << L"\""
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

BusLine ParseActiveBus(const std::wstring_view scenarioJson)
{
    return scenarioJson.find(L"\"activeBus\":\"B\"") != std::wstring_view::npos
        ? BusLine::B
        : BusLine::A;
}

std::wstring BuildSessionHandle(const std::uint64_t sessionId)
{
    std::wostringstream builder;
    builder << L"session-" << std::setw(4) << std::setfill(L'0') << sessionId;
    return builder.str();
}

std::chrono::microseconds NextTimeTag(SessionState& session)
{
    const auto current = session.nextTimeTagMicros;
    session.nextTimeTagMicros += 100;
    return std::chrono::microseconds{ static_cast<std::int64_t>(current) };
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

        SessionState session;
        session.health = HealthSnapshot::CreateDefault();
        session.health.SwitchActiveBus(ParseActiveBus(scenarioJson));

        std::lock_guard lock(g_sessionMutex);
        const std::wstring sessionHandle = BuildSessionHandle(g_nextSessionId);
        const std::wstring healthJson = SerializeHealthSnapshot(session.health);

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

        g_sessions.emplace(sessionHandle, std::move(session));
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

        auto& session = iterator->second;
        const auto nextBus = busLine == 0 ? BusLine::A : BusLine::B;
        session.health.SwitchActiveBus(nextBus);
        session.pendingEvents.push_back(TelemetryEvent::CreateBusSwitchEvent(nextBus, NextTimeTag(session)));

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

        const auto healthJson = SerializeHealthSnapshot(iterator->second.health);
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

        auto& session = iterator->second;
        const auto telemetryJson = SerializeTelemetryEvents(session.pendingEvents);
        const auto status = WriteWideStringToBuffer(
            telemetryJson,
            telemetryJsonBuffer,
            telemetryJsonCapacity,
            requiredTelemetryJsonCapacity);
        if (status == NativeCallStatus::Success)
        {
            session.pendingEvents.clear();
        }

        return static_cast<int>(status);
    }
    catch (...)
    {
        return static_cast<int>(NativeCallStatus::InternalError);
    }
}
}
