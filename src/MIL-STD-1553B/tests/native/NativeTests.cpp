#include "Application/BusControllerService.h"
#include "Application/BusMonitorService.h"
#include "Domain/CommandWord.h"
#include "Domain/StatusWord.h"
#include "Domain/TransferTypes.h"
#include "Infrastructure/JsonLinesBusEventStore.h"
#include "Infrastructure/SimulatorBusAdapter.h"
#include "Interop/NativeSessionApi.h"

#include <exception>
#include <filesystem>
#include <fstream>
#include <functional>
#include <iostream>
#include <sstream>
#include <stdexcept>
#include <string>
#include <utility>
#include <vector>

namespace
{
using MilStd1553::Application::BusControllerService;
using MilStd1553::Application::BusMonitorService;
using MilStd1553::Application::IBusAdapter;
using MilStd1553::Application::IClock;
using MilStd1553::Domain::BusLine;
using MilStd1553::Domain::CommandWord;
using MilStd1553::Domain::DataWord;
using MilStd1553::Domain::ErrorCode;
using MilStd1553::Domain::HealthSnapshot;
using MilStd1553::Domain::ModeCode;
using MilStd1553::Domain::RtToRtTransferRequest;
using MilStd1553::Domain::StatusWord;
using MilStd1553::Domain::TelemetryEventType;
using MilStd1553::Domain::TransferRequest;
using MilStd1553::Domain::TransferResult;
using MilStd1553::Infrastructure::SimulatorBusAdapter;
using MilStd1553::Infrastructure::JsonLinesBusEventStore;

class FakeClock final : public IClock
{
public:
    explicit FakeClock(const std::chrono::microseconds timeTag)
        : timeTag_(timeTag)
    {
    }

    std::chrono::microseconds GetCurrentTimeTag() const override
    {
        return timeTag_;
    }

private:
    std::chrono::microseconds timeTag_;
};

class FakeAdapter final : public IBusAdapter
{
public:
    explicit FakeAdapter(std::vector<TransferResult> scriptedResults)
        : scriptedResults_(std::move(scriptedResults))
    {
    }

    TransferResult Send(const TransferRequest& request) override
    {
        ++sendCount;
        lastRequest = request;

        if (cursor_ >= scriptedResults_.size())
        {
            return TransferResult{ ErrorCode::Timeout, std::nullopt, {}, std::chrono::microseconds{ 0 } };
        }

        return scriptedResults_[cursor_++];
    }

    void SelectBus(const BusLine busLine) override
    {
        selectedBus = busLine;
    }

    int sendCount{ 0 };
    BusLine selectedBus{ BusLine::A };
    TransferRequest lastRequest{ CommandWord{ 0, false, 1, 1 }, BusLine::A, {} };

private:
    std::vector<TransferResult> scriptedResults_;
    std::size_t cursor_{ 0 };
};

void ExpectTrue(const bool condition, const std::string& message)
{
    if (!condition)
    {
        throw std::runtime_error(message);
    }
}

template <typename T>
void ExpectEqual(const T& actual, const T& expected, const std::string& label)
{
    if (!(actual == expected))
    {
        std::ostringstream builder;
        builder << label << " 기대값과 실제값이 다릅니다.";
        throw std::runtime_error(builder.str());
    }
}

void CommandWordParsesRegularTransfer()
{
    const auto commandWord = CommandWord::FromRaw(0x1C40U);

    ExpectEqual(commandWord.GetTerminalAddress(), static_cast<std::uint8_t>(3U), "RT 주소");
    ExpectTrue(commandWord.IsTransmit(), "T/R 비트가 transmit이어야 합니다.");
    ExpectEqual(commandWord.GetSubAddress(), static_cast<std::uint8_t>(2U), "서브어드레스");
    ExpectEqual(commandWord.GetDataWordCount(), static_cast<std::uint8_t>(32U), "워드 수");
    ExpectEqual(commandWord.ToRaw(), static_cast<std::uint16_t>(0x1C40U), "원본 직렬화");
}

void CommandWordIdentifiesModeCodeWithDataWord()
{
    const auto commandWord = CommandWord::FromRaw(0x3FF0U);

    ExpectTrue(commandWord.IsModeCode(), "mode code 경로여야 합니다.");
    ExpectEqual(commandWord.GetModeCode(), static_cast<std::uint8_t>(16U), "mode code");
    ExpectTrue(commandWord.RequiresModeDataWord(), "단일 데이터 워드가 필요해야 합니다.");
}

void StatusWordParsesRelevantFlags()
{
    const auto statusWord = StatusWord::FromRaw(0x2519U);

    ExpectEqual(statusWord.GetTerminalAddress(), static_cast<std::uint8_t>(4U), "RT 주소");
    ExpectTrue(statusWord.HasMessageError(), "message error 비트가 설정되어야 합니다.");
    ExpectTrue(statusWord.HasServiceRequest(), "service request 비트가 설정되어야 합니다.");
    ExpectTrue(statusWord.HasBroadcastCommandReceived(), "broadcast received 비트가 설정되어야 합니다.");
    ExpectTrue(statusWord.IsBusy(), "busy 비트가 설정되어야 합니다.");
    ExpectTrue(statusWord.HasTerminalFlag(), "terminal flag 비트가 설정되어야 합니다.");
}

void HealthSnapshotTracksTimeoutAndBusSwitch()
{
    auto health = HealthSnapshot::CreateDefault();

    health.RecordTimeout();
    health.RecordRetry();
    health.SwitchActiveBus(BusLine::B);

    ExpectEqual(health.timeoutCount, static_cast<std::uint32_t>(1U), "timeout count");
    ExpectEqual(health.retryCount, static_cast<std::uint32_t>(1U), "retry count");
    ExpectTrue(health.degraded, "degraded 상태여야 합니다.");
    ExpectTrue(health.activeBus == BusLine::B, "활성 버스가 B여야 합니다.");
    ExpectTrue(health.standbyBus == BusLine::A, "대기 버스가 A여야 합니다.");
}

void BusControllerRetriesOnceAfterTimeoutAndPublishesMessage()
{
    FakeAdapter adapter({
        TransferResult{ ErrorCode::Timeout, std::nullopt, {}, std::chrono::microseconds{ 0 } },
        TransferResult{
            ErrorCode::None,
            StatusWord{ 1, false, false, false, false, false, false, false },
            { DataWord{ 0x1001U } },
            std::chrono::microseconds{ 125 } },
    });
    FakeClock clock(std::chrono::microseconds{ 999 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const TransferRequest request{
        CommandWord{ 1, true, 2, 1 },
        BusLine::B,
        {},
    };

    const auto result = controller.Execute(request);

    ExpectTrue(result.errorCode == ErrorCode::None, "재시도 후 성공해야 합니다.");
    ExpectEqual(adapter.sendCount, 2, "전송 횟수");
    ExpectTrue(adapter.lastRequest.busLine == BusLine::A, "활성 버스 A로 요청이 정규화되어야 합니다.");
    ExpectEqual(controller.GetHealthSnapshot().timeoutCount, static_cast<std::uint32_t>(1U), "timeout count");
    ExpectEqual(controller.GetHealthSnapshot().retryCount, static_cast<std::uint32_t>(1U), "retry count");
    ExpectTrue(controller.GetHealthSnapshot().degraded, "degraded 상태가 유지되어야 합니다.");
    ExpectEqual(monitor.GetEvents().size(), static_cast<std::size_t>(1U), "이벤트 수");
    ExpectTrue(monitor.GetEvents().front().type == TelemetryEventType::MessageFrame, "메시지 이벤트여야 합니다.");
    ExpectEqual(
        monitor.GetEvents().front().messageFrame->timeTag,
        std::chrono::microseconds{ 125 },
        "message time-tag");
}

void BusControllerSwitchesBusAndPublishesSwitchEvent()
{
    FakeAdapter adapter({});
    FakeClock clock(std::chrono::microseconds{ 250 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    controller.SwitchBus(BusLine::B);

    ExpectTrue(adapter.selectedBus == BusLine::B, "어댑터도 B 버스로 전환되어야 합니다.");
    ExpectTrue(controller.GetHealthSnapshot().activeBus == BusLine::B, "health active bus가 B여야 합니다.");
    ExpectEqual(monitor.GetEvents().size(), static_cast<std::size_t>(1U), "이벤트 수");
    ExpectTrue(monitor.GetEvents().front().type == TelemetryEventType::BusSwitch, "버스 전환 이벤트여야 합니다.");
    ExpectEqual(monitor.GetEvents().front().timeTag, std::chrono::microseconds{ 250 }, "switch time-tag");
}

void BusMonitorStoresMessageFrameEvent()
{
    BusMonitorService monitor;
    const auto commandWord = CommandWord{ 2, false, 5, 2 };
    const auto statusWord = StatusWord{ 2, false, false, false, false, false, false, false };
    const MilStd1553::Domain::MessageFrame frame{
        commandWord,
        statusWord,
        { DataWord{ 0xAA55U }, DataWord{ 0x55AAU } },
        BusLine::A,
        std::chrono::microseconds{ 400 },
    };

    monitor.Publish(MilStd1553::Domain::TelemetryEvent::CreateMessageEvent(frame));

    ExpectEqual(monitor.GetEvents().size(), static_cast<std::size_t>(1U), "이벤트 수");
    ExpectEqual(
        monitor.GetEvents().front().messageFrame->dataWords.size(),
        static_cast<std::size_t>(2U),
        "데이터 워드 수");
    ExpectEqual(
        monitor.GetEvents().front().messageFrame->timeTag,
        std::chrono::microseconds{ 400 },
        "BM time-tag");
}

void BusMonitorPersistsJsonLinesEventLog()
{
    const auto logPath = std::filesystem::temp_directory_path() / "MilStd1553_BusMonitorPersistsJsonLinesEventLog.jsonl";
    std::filesystem::remove(logPath);

    {
        JsonLinesBusEventStore store(logPath);
        BusMonitorService monitor(&store);

        const auto commandWord = CommandWord{ 3, true, 4, 1 };
        const auto statusWord = StatusWord{ 3, false, false, false, false, false, false, false };
        const MilStd1553::Domain::MessageFrame frame{
            commandWord,
            statusWord,
            { DataWord{ 0x0A01U } },
            BusLine::B,
            std::chrono::microseconds{ 510 },
        };

        monitor.Publish(MilStd1553::Domain::TelemetryEvent::CreateMessageEvent(frame));
        monitor.Publish(MilStd1553::Domain::TelemetryEvent::CreateBusSwitchEvent(BusLine::B, std::chrono::microseconds{ 700 }));
    }

    std::ifstream input(logPath);
    const std::string fileContent(
        (std::istreambuf_iterator<char>(input)),
        std::istreambuf_iterator<char>());
    input.close();

    ExpectTrue(fileContent.find("\"eventType\":\"MessageFrame\"") != std::string::npos, "메시지 이벤트가 JSONL에 기록되어야 합니다.");
    ExpectTrue(fileContent.find("\"commandWordRaw\":") != std::string::npos, "커맨드 워드 raw 값이 포함되어야 합니다.");
    ExpectTrue(fileContent.find("\"eventType\":\"BusSwitch\"") != std::string::npos, "버스 전환 이벤트가 JSONL에 기록되어야 합니다.");
    ExpectTrue(fileContent.find("\"timeTagMicros\":700") != std::string::npos, "버스 전환 time-tag가 기록되어야 합니다.");

    std::filesystem::remove(logPath);
}

void SimulatorAdapterSupportsRtToBcTransfer()
{
    SimulatorBusAdapter adapter;
    adapter.SetSubAddressData(7, 3, { DataWord{ 0x1101U }, DataWord{ 0x2202U } });
    adapter.SetStatusWord(7, StatusWord{ 7, false, true, false, false, false, false, false });

    FakeClock clock(std::chrono::microseconds{ 999 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.Execute(TransferRequest{
        CommandWord{ 7, true, 3, 2 },
        BusLine::A,
        {},
    });

    ExpectTrue(result.errorCode == ErrorCode::None, "RT → BC 전송이 성공해야 합니다.");
    ExpectTrue(result.statusWord.has_value(), "상태 워드가 반환되어야 합니다.");
    ExpectTrue(result.statusWord->HasServiceRequest(), "서비스 요청 비트가 유지되어야 합니다.");
    ExpectEqual(result.dataWords.size(), static_cast<std::size_t>(2U), "반환 데이터 수");
    ExpectEqual(result.dataWords[0].value, static_cast<std::uint16_t>(0x1101U), "첫 번째 데이터 워드");
    ExpectEqual(monitor.GetEvents().size(), static_cast<std::size_t>(1U), "이벤트 수");
}

void SimulatorAdapterSupportsRtToRtTransfer()
{
    SimulatorBusAdapter adapter;
    adapter.SetSubAddressData(4, 5, { DataWord{ 0xA100U }, DataWord{ 0xA200U } });
    adapter.SetStatusWord(4, StatusWord{ 4, false, false, false, false, false, false, false });
    adapter.SetStatusWord(9, StatusWord{ 9, false, false, false, false, false, false, false });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.ExecuteRtToRtTransfer(RtToRtTransferRequest{
        CommandWord{ 9, false, 6, 2 },
        CommandWord{ 4, true, 5, 2 },
        BusLine::A,
    });

    ExpectTrue(result.sourceTransfer.errorCode == ErrorCode::None, "소스 RT transmit가 성공해야 합니다.");
    ExpectTrue(result.destinationTransfer.has_value(), "목적지 RT receive 결과가 있어야 합니다.");
    ExpectTrue(result.destinationTransfer->errorCode == ErrorCode::None, "목적지 RT receive가 성공해야 합니다.");

    const auto storedData = adapter.GetSubAddressData(9, 6);
    ExpectEqual(storedData.size(), static_cast<std::size_t>(2U), "목적지 저장 데이터 수");
    ExpectEqual(storedData[0].value, static_cast<std::uint16_t>(0xA100U), "목적지 첫 번째 데이터");
    ExpectEqual(storedData[1].value, static_cast<std::uint16_t>(0xA200U), "목적지 두 번째 데이터");
    ExpectEqual(monitor.GetEvents().size(), static_cast<std::size_t>(2U), "RT ↔ RT 이벤트 수");
}

void SimulatorAdapterSupportsTransmitStatusWordModeCode()
{
    SimulatorBusAdapter adapter;
    adapter.SetStatusWord(8, StatusWord{ 8, false, true, false, false, false, false, true });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(8, true, ModeCode::TransmitStatusWord),
        BusLine::A,
        {},
    });

    ExpectTrue(result.errorCode == ErrorCode::None, "Transmit Status Word mode code가 성공해야 합니다.");
    ExpectTrue(result.statusWord.has_value(), "상태 워드가 반환되어야 합니다.");
    ExpectTrue(result.statusWord->HasServiceRequest(), "서비스 요청 비트가 유지되어야 합니다.");
    ExpectTrue(result.statusWord->HasTerminalFlag(), "terminal flag 비트가 유지되어야 합니다.");
    ExpectEqual(result.dataWords.size(), static_cast<std::size_t>(0U), "Mode Code 데이터 수");
}

void SimulatorAdapterSupportsTransmitBitWordModeCode()
{
    SimulatorBusAdapter adapter;
    adapter.SetStatusWord(6, StatusWord{ 6, false, false, false, false, false, false, false });
    adapter.SetBitWord(6, DataWord{ 0x1234U });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(6, true, ModeCode::TransmitBitWord),
        BusLine::A,
        {},
    });

    ExpectTrue(result.errorCode == ErrorCode::None, "Transmit BIT Word mode code가 성공해야 합니다.");
    ExpectEqual(result.dataWords.size(), static_cast<std::size_t>(1U), "BIT Word 데이터 수");
    ExpectEqual(result.dataWords.front().value, static_cast<std::uint16_t>(0x1234U), "BIT Word 값");
}

void NativeSessionApiManagesSessionLifecycle()
{
    wchar_t sessionHandleBuffer[64]{};
    wchar_t healthJsonBuffer[256]{};
    int requiredSessionHandleCapacity = 0;
    int requiredHealthJsonCapacity = 0;

    const auto openStatus = MilStd1553_OpenSession(
        LR"({"name":"InteropDraft","channelId":0,"activeBus":"B","bcSchedules":[{"name":"PollRt1","periodMs":20,"rtAddress":1,"subAddress":2,"direction":"Receive"}]})",
        sessionHandleBuffer,
        64,
        &requiredSessionHandleCapacity,
        healthJsonBuffer,
        256,
        &requiredHealthJsonCapacity);

    ExpectEqual(openStatus, 0, "OpenSession status");
    ExpectTrue(std::wstring_view(sessionHandleBuffer).starts_with(L"session-"), "세션 핸들이 생성되어야 합니다.");
    ExpectTrue(std::wstring_view(healthJsonBuffer).find(L"\"activeBus\":\"B\"") != std::wstring_view::npos, "초기 active bus가 B여야 합니다.");

    const auto switchStatus = MilStd1553_SwitchBus(sessionHandleBuffer, 0);
    ExpectEqual(switchStatus, 0, "SwitchBus status");

    wchar_t telemetryJsonBuffer[1024]{};
    int requiredTelemetryJsonCapacity = 0;
    const auto pollStatus = MilStd1553_PollTelemetry(
        sessionHandleBuffer,
        telemetryJsonBuffer,
        1024,
        &requiredTelemetryJsonCapacity);
    ExpectEqual(pollStatus, 0, "PollTelemetry status");
    ExpectTrue(std::wstring_view(telemetryJsonBuffer).find(L"\"type\":\"BusSwitch\"") != std::wstring_view::npos, "버스 전환 이벤트가 반환되어야 합니다.");
    ExpectTrue(std::wstring_view(telemetryJsonBuffer).find(L"\"activeBus\":\"A\"") != std::wstring_view::npos, "전환 후 active bus가 A여야 합니다.");

    wchar_t emptyTelemetryJsonBuffer[256]{};
    const auto emptyPollStatus = MilStd1553_PollTelemetry(
        sessionHandleBuffer,
        emptyTelemetryJsonBuffer,
        256,
        &requiredTelemetryJsonCapacity);
    ExpectEqual(emptyPollStatus, 0, "빈 PollTelemetry status");
    ExpectTrue(std::wstring_view(emptyTelemetryJsonBuffer) == L"[]", "poll 이후 pending event가 비워져야 합니다.");

    wchar_t refreshedHealthBuffer[256]{};
    const auto healthStatus = MilStd1553_GetHealthSnapshot(
        sessionHandleBuffer,
        refreshedHealthBuffer,
        256,
        &requiredHealthJsonCapacity);
    ExpectEqual(healthStatus, 0, "GetHealthSnapshot status");
    ExpectTrue(std::wstring_view(refreshedHealthBuffer).find(L"\"activeBus\":\"A\"") != std::wstring_view::npos, "health snapshot도 A로 갱신되어야 합니다.");

    const auto stopStatus = MilStd1553_StopSession(sessionHandleBuffer);
    ExpectEqual(stopStatus, 0, "StopSession status");

    wchar_t missingHealthBuffer[128]{};
    const auto missingHealthStatus = MilStd1553_GetHealthSnapshot(
        sessionHandleBuffer,
        missingHealthBuffer,
        128,
        &requiredHealthJsonCapacity);
    ExpectEqual(missingHealthStatus, 2, "세션 종료 후 상태 조회는 session not found여야 합니다.");
}

void NativeSessionApiReturnsRequiredCapacitiesWhenOpenSessionBufferIsTooSmall()
{
    wchar_t sessionHandleBuffer[4]{};
    wchar_t healthJsonBuffer[16]{};
    int requiredSessionHandleCapacity = 0;
    int requiredHealthJsonCapacity = 0;

    const auto openStatus = MilStd1553_OpenSession(
        LR"({"name":"InteropDraft","channelId":0,"activeBus":"A","bcSchedules":[{"name":"PollRt1","periodMs":20,"rtAddress":1,"subAddress":2,"direction":"Receive"}]})",
        sessionHandleBuffer,
        4,
        &requiredSessionHandleCapacity,
        healthJsonBuffer,
        16,
        &requiredHealthJsonCapacity);

    ExpectEqual(openStatus, 3, "OpenSession small buffer status");
    ExpectTrue(requiredSessionHandleCapacity > 4, "?몄뀡 ?몃뱾 ?꾩슂 湲몄씠媛 ?붿껌 踰꾪띁蹂대떎 而ㅼ빞 ?⑸땲??");
    ExpectTrue(requiredHealthJsonCapacity > 16, "health JSON ?꾩슂 湲몄씠媛 ?붿껌 踰꾪띁蹂대떎 而ㅼ빞 ?⑸땲??");
    ExpectTrue(sessionHandleBuffer[0] == L'\0', "?ㅽ뙣 ???몄뀡 ?몃뱾 踰꾪띁媛 鍮꾩뼱 ?덉뼱???⑸땲??");
    ExpectTrue(healthJsonBuffer[0] == L'\0', "?ㅽ뙣 ??health JSON 踰꾪띁媛 鍮꾩뼱 ?덉뼱???⑸땲??");
}

void NativeSessionApiKeepsPendingTelemetryWhenPollBufferIsTooSmall()
{
    wchar_t sessionHandleBuffer[64]{};
    wchar_t healthJsonBuffer[256]{};
    int requiredSessionHandleCapacity = 0;
    int requiredHealthJsonCapacity = 0;

    const auto openStatus = MilStd1553_OpenSession(
        LR"({"name":"InteropDraft","channelId":0,"activeBus":"A","bcSchedules":[{"name":"PollRt1","periodMs":20,"rtAddress":1,"subAddress":2,"direction":"Receive"}]})",
        sessionHandleBuffer,
        64,
        &requiredSessionHandleCapacity,
        healthJsonBuffer,
        256,
        &requiredHealthJsonCapacity);
    ExpectEqual(openStatus, 0, "OpenSession status");

    const auto switchStatus = MilStd1553_SwitchBus(sessionHandleBuffer, 1);
    ExpectEqual(switchStatus, 0, "SwitchBus status");

    wchar_t smallTelemetryJsonBuffer[8]{};
    int requiredTelemetryJsonCapacity = 0;
    const auto smallPollStatus = MilStd1553_PollTelemetry(
        sessionHandleBuffer,
        smallTelemetryJsonBuffer,
        8,
        &requiredTelemetryJsonCapacity);

    ExpectEqual(smallPollStatus, 3, "PollTelemetry small buffer status");
    ExpectTrue(requiredTelemetryJsonCapacity > 8, "telemetry ?꾩슂 湲몄씠媛 ?붿껌 踰꾪띁蹂대떎 而ㅼ빞 ?⑸땲??");
    ExpectTrue(smallTelemetryJsonBuffer[0] == L'\0', "?ㅽ뙣 ??telemetry 踰꾪띁媛 鍮꾩뼱 ?덉뼱???⑸땲??");

    std::vector<wchar_t> telemetryJsonBuffer(static_cast<std::size_t>(requiredTelemetryJsonCapacity), L'\0');
    const auto retryPollStatus = MilStd1553_PollTelemetry(
        sessionHandleBuffer,
        telemetryJsonBuffer.data(),
        requiredTelemetryJsonCapacity,
        &requiredTelemetryJsonCapacity);

    ExpectEqual(retryPollStatus, 0, "PollTelemetry retry status");
    ExpectTrue(
        std::wstring_view(telemetryJsonBuffer.data()).find(L"\"type\":\"BusSwitch\"") != std::wstring_view::npos,
        "buffer-too-small ???대깽?멸? ?좏? ???⑸땲??");

    wchar_t emptyTelemetryJsonBuffer[256]{};
    const auto emptyPollStatus = MilStd1553_PollTelemetry(
        sessionHandleBuffer,
        emptyTelemetryJsonBuffer,
        256,
        &requiredTelemetryJsonCapacity);
    ExpectEqual(emptyPollStatus, 0, "empty PollTelemetry status");
    ExpectTrue(std::wstring_view(emptyTelemetryJsonBuffer) == L"[]", "?깃났 ?댄썑 telemetry媛 clear ?섏뼱???⑸땲??");

    const auto stopStatus = MilStd1553_StopSession(sessionHandleBuffer);
    ExpectEqual(stopStatus, 0, "StopSession status");
}

int RunAllTests()
{
    const std::vector<std::pair<std::string, std::function<void()>>> tests = {
        { "CommandWordParsesRegularTransfer", CommandWordParsesRegularTransfer },
        { "CommandWordIdentifiesModeCodeWithDataWord", CommandWordIdentifiesModeCodeWithDataWord },
        { "StatusWordParsesRelevantFlags", StatusWordParsesRelevantFlags },
        { "HealthSnapshotTracksTimeoutAndBusSwitch", HealthSnapshotTracksTimeoutAndBusSwitch },
        { "BusControllerRetriesOnceAfterTimeoutAndPublishesMessage", BusControllerRetriesOnceAfterTimeoutAndPublishesMessage },
        { "BusControllerSwitchesBusAndPublishesSwitchEvent", BusControllerSwitchesBusAndPublishesSwitchEvent },
        { "BusMonitorStoresMessageFrameEvent", BusMonitorStoresMessageFrameEvent },
        { "BusMonitorPersistsJsonLinesEventLog", BusMonitorPersistsJsonLinesEventLog },
        { "SimulatorAdapterSupportsRtToBcTransfer", SimulatorAdapterSupportsRtToBcTransfer },
        { "SimulatorAdapterSupportsRtToRtTransfer", SimulatorAdapterSupportsRtToRtTransfer },
        { "SimulatorAdapterSupportsTransmitStatusWordModeCode", SimulatorAdapterSupportsTransmitStatusWordModeCode },
        { "SimulatorAdapterSupportsTransmitBitWordModeCode", SimulatorAdapterSupportsTransmitBitWordModeCode },
        { "NativeSessionApiManagesSessionLifecycle", NativeSessionApiManagesSessionLifecycle },
        { "NativeSessionApiReturnsRequiredCapacitiesWhenOpenSessionBufferIsTooSmall", NativeSessionApiReturnsRequiredCapacitiesWhenOpenSessionBufferIsTooSmall },
        { "NativeSessionApiKeepsPendingTelemetryWhenPollBufferIsTooSmall", NativeSessionApiKeepsPendingTelemetryWhenPollBufferIsTooSmall },
    };

    for (const auto& [name, test] : tests)
    {
        test();
        std::cout << "[PASS] " << name << '\n';
    }

    std::cout << "전체 네이티브 테스트 통과: " << tests.size() << "건" << '\n';
    return 0;
}
}

int main()
{
    try
    {
        return RunAllTests();
    }
    catch (const std::exception& exception)
    {
        std::cerr << "[FAIL] " << exception.what() << '\n';
        return 1;
    }
}
