#include "Application/BusControllerService.h"
#include "Application/BusMonitorService.h"
#include "Domain/CommandWord.h"
#include "Domain/StatusWord.h"
#include "Domain/TransferTypes.h"
#include "Infrastructure/JsonLinesBusEventStore.h"
#include "Infrastructure/SimulatorBusAdapter.h"
#include "Infrastructure/SimulatorVendorChannel.h"
#include "Infrastructure/VendorSdkBusAdapter.h"
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
using MilStd1553::Infrastructure::DefaultVendorErrorMapper;
using MilStd1553::Infrastructure::JsonLinesBusEventStore;
using MilStd1553::Infrastructure::IVendorChannel;
using MilStd1553::Infrastructure::SimulatorVendorChannel;
using MilStd1553::Infrastructure::VendorAdapterCapabilities;
using MilStd1553::Infrastructure::VendorChannelConfiguration;
using MilStd1553::Infrastructure::VendorOperationResult;
using MilStd1553::Infrastructure::VendorSdkBusAdapter;
using MilStd1553::Infrastructure::VendorSdkStatusCode;
using MilStd1553::Infrastructure::VendorTransferResult;

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
        requestHistory.push_back(request);

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
    std::vector<TransferRequest> requestHistory;

private:
    std::vector<TransferResult> scriptedResults_;
    std::size_t cursor_{ 0 };
};

class FakeVendorChannel final : public IVendorChannel
{
public:
    VendorAdapterCapabilities QueryCapabilities() const override
    {
        return capabilities;
    }

    VendorOperationResult Open(const VendorChannelConfiguration& configuration) override
    {
        ++openCallCount;
        lastOpenConfiguration = configuration;
        isOpen = openResult.IsSuccess();
        return openResult;
    }

    VendorOperationResult Close() override
    {
        ++closeCallCount;
        isOpen = false;
        return closeResult;
    }

    VendorTransferResult SubmitTransfer(const TransferRequest& request) override
    {
        ++submitTransferCallCount;
        lastTransferRequest = request;
        return transferResult;
    }

    VendorOperationResult SelectBus(const BusLine busLine) override
    {
        ++selectBusCallCount;
        lastSelectedBus = busLine;
        return selectBusResult;
    }

    VendorAdapterCapabilities capabilities{};
    VendorOperationResult openResult{};
    VendorOperationResult closeResult{};
    VendorOperationResult selectBusResult{};
    VendorTransferResult transferResult{};
    bool isOpen{ false };
    int openCallCount{ 0 };
    int closeCallCount{ 0 };
    int submitTransferCallCount{ 0 };
    int selectBusCallCount{ 0 };
    std::optional<VendorChannelConfiguration> lastOpenConfiguration;
    std::optional<TransferRequest> lastTransferRequest;
    BusLine lastSelectedBus{ BusLine::A };
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
    ExpectEqual(health.consecutiveTimeoutCount, static_cast<std::uint32_t>(1U), "consecutive timeout count");
    ExpectEqual(health.autoFailoverCount, static_cast<std::uint32_t>(0U), "auto failover count");
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
    ExpectEqual(controller.GetHealthSnapshot().consecutiveTimeoutCount, static_cast<std::uint32_t>(0U), "consecutive timeout count");
    ExpectEqual(controller.GetHealthSnapshot().autoFailoverCount, static_cast<std::uint32_t>(0U), "auto failover count");
    ExpectTrue(controller.GetHealthSnapshot().degraded, "degraded 상태가 유지되어야 합니다.");
    ExpectEqual(monitor.GetEvents().size(), static_cast<std::size_t>(1U), "이벤트 수");
    ExpectTrue(monitor.GetEvents().front().type == TelemetryEventType::MessageFrame, "메시지 이벤트여야 합니다.");
    ExpectEqual(
        monitor.GetEvents().front().messageFrame->timeTag,
        std::chrono::microseconds{ 125 },
        "message time-tag");
}

void BusControllerPublishesReceivePayloadInMessageFrame()
{
    FakeAdapter adapter({
        TransferResult{
            ErrorCode::None,
            StatusWord{ 2, false, false, false, false, false, false, false },
            {},
            std::chrono::microseconds{ 200 } },
    });
    FakeClock clock(std::chrono::microseconds{ 999 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const TransferRequest request{
        CommandWord{ 2, false, 4, 2 },
        BusLine::A,
        { DataWord{ 0x1201U }, DataWord{ 0x1202U } },
    };

    const auto result = controller.Execute(request);

    ExpectTrue(result.errorCode == ErrorCode::None, "BC -> RT receive가 성공해야 합니다.");
    ExpectEqual(monitor.GetEvents().size(), static_cast<std::size_t>(1U), "receive telemetry event count");
    ExpectTrue(monitor.GetEvents().front().messageFrame.has_value(), "메시지 프레임이 기록되어야 합니다.");
    ExpectEqual(
        monitor.GetEvents().front().messageFrame->dataWords.size(),
        static_cast<std::size_t>(2U),
        "receive message frame data word count");
    ExpectEqual(
        monitor.GetEvents().front().messageFrame->dataWords[0].value,
        static_cast<std::uint16_t>(0x1201U),
        "receive message frame first data word");
    ExpectEqual(
        monitor.GetEvents().front().messageFrame->dataWords[1].value,
        static_cast<std::uint16_t>(0x1202U),
        "receive message frame second data word");
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

void BusControllerAutomaticallyFailsOverAfterConsecutiveTimeouts()
{
    FakeAdapter adapter({
        TransferResult{ ErrorCode::Timeout, std::nullopt, {}, std::chrono::microseconds{ 0 } },
        TransferResult{ ErrorCode::Timeout, std::nullopt, {}, std::chrono::microseconds{ 0 } },
        TransferResult{
            ErrorCode::None,
            StatusWord{ 1, false, false, false, false, false, false, false },
            { DataWord{ 0x2002U } },
            std::chrono::microseconds{ 325 } },
    });
    FakeClock clock(std::chrono::microseconds{ 777 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.Execute(TransferRequest{
        CommandWord{ 1, true, 2, 1 },
        BusLine::A,
        {},
    });

    ExpectTrue(result.errorCode == ErrorCode::None, "자동 failover 이후 재전송이 성공해야 합니다.");
    ExpectEqual(adapter.sendCount, 3, "전송 횟수");
    ExpectTrue(adapter.selectedBus == BusLine::B, "standby bus인 B로 전환되어야 합니다.");
    ExpectTrue(adapter.requestHistory.back().busLine == BusLine::B, "마지막 재전송은 B 버스로 나가야 합니다.");
    ExpectTrue(controller.GetHealthSnapshot().activeBus == BusLine::B, "health active bus가 B여야 합니다.");
    ExpectEqual(controller.GetHealthSnapshot().timeoutCount, static_cast<std::uint32_t>(2U), "timeout count");
    ExpectEqual(controller.GetHealthSnapshot().retryCount, static_cast<std::uint32_t>(1U), "retry count");
    ExpectEqual(controller.GetHealthSnapshot().consecutiveTimeoutCount, static_cast<std::uint32_t>(0U), "consecutive timeout count");
    ExpectEqual(controller.GetHealthSnapshot().autoFailoverCount, static_cast<std::uint32_t>(1U), "auto failover count");
    ExpectEqual(monitor.GetEvents().size(), static_cast<std::size_t>(2U), "이벤트 수");
    ExpectTrue(monitor.GetEvents()[0].type == TelemetryEventType::AutoFailover, "자동 failover 이벤트가 먼저 기록되어야 합니다.");
    ExpectTrue(monitor.GetEvents()[1].type == TelemetryEventType::MessageFrame, "재전송 성공 메시지가 기록되어야 합니다.");
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
    const auto destinationCommand = CommandWord{ 9, false, 6, 2 };
    const auto sourceCommand = CommandWord{ 4, true, 5, 2 };
    adapter.SetSubAddressData(4, 5, { DataWord{ 0xA100U }, DataWord{ 0xA200U } });
    adapter.SetStatusWord(4, StatusWord{ 4, false, false, false, false, false, false, false });
    adapter.SetStatusWord(9, StatusWord{ 9, false, false, false, false, false, false, false });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.ExecuteRtToRtTransfer(RtToRtTransferRequest{
        destinationCommand,
        sourceCommand,
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
    ExpectTrue(monitor.GetEvents()[0].type == TelemetryEventType::MessageFrame, "RT↔RT 첫 번째 이벤트는 메시지 프레임이어야 합니다.");
    ExpectTrue(monitor.GetEvents()[1].type == TelemetryEventType::MessageFrame, "RT↔RT 두 번째 이벤트는 메시지 프레임이어야 합니다.");
    ExpectEqual(monitor.GetEvents()[0].description, std::string("RT↔RT 소스 RT transmit 단계"), "RT↔RT source description");
    ExpectEqual(monitor.GetEvents()[1].description, std::string("RT↔RT 목적지 RT receive 단계"), "RT↔RT destination description");
    ExpectTrue(monitor.GetEvents()[0].messageFrame.has_value(), "RT↔RT source frame이 기록되어야 합니다.");
    ExpectTrue(monitor.GetEvents()[1].messageFrame.has_value(), "RT↔RT destination frame이 기록되어야 합니다.");
    ExpectEqual(
        monitor.GetEvents()[0].messageFrame->commandWord.ToRaw(),
        sourceCommand.ToRaw(),
        "RT↔RT source command word");
    ExpectEqual(
        monitor.GetEvents()[1].messageFrame->commandWord.ToRaw(),
        destinationCommand.ToRaw(),
        "RT↔RT destination command word");
    ExpectEqual(
        monitor.GetEvents()[1].messageFrame->dataWords.size(),
        static_cast<std::size_t>(2U),
        "RT↔RT destination frame data word count");
    ExpectEqual(
        monitor.GetEvents()[1].messageFrame->dataWords[0].value,
        static_cast<std::uint16_t>(0xA100U),
        "RT↔RT destination frame first data word");
    ExpectEqual(
        monitor.GetEvents()[1].messageFrame->dataWords[1].value,
        static_cast<std::uint16_t>(0xA200U),
        "RT↔RT destination frame second data word");
    ExpectEqual(
        result.sourceTransfer.timeTag,
        std::chrono::microseconds{ 100 },
        "RT↔RT source transfer time-tag");
    ExpectEqual(
        result.destinationTransfer->timeTag,
        std::chrono::microseconds{ 140 },
        "RT↔RT destination transfer time-tag");
    ExpectEqual(
        monitor.GetEvents()[0].messageFrame->timeTag,
        std::chrono::microseconds{ 100 },
        "RT↔RT source frame time-tag");
    ExpectEqual(
        monitor.GetEvents()[1].messageFrame->timeTag,
        std::chrono::microseconds{ 140 },
        "RT↔RT destination frame time-tag");
    ExpectTrue(
        monitor.GetEvents()[1].timeTag - monitor.GetEvents()[0].timeTag
            >= std::chrono::microseconds{ 40 },
        "RT↔RT destination 이벤트는 source 이후 최소 gap을 가져야 합니다.");
}

void SimulatorAdapterRejectsInvalidRtToRtTransfer()
{
    SimulatorBusAdapter adapter;
    adapter.SetSubAddressData(4, 5, { DataWord{ 0xA100U }, DataWord{ 0xA200U } });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.ExecuteRtToRtTransfer(RtToRtTransferRequest{
        CommandWord{ 9, false, 6, 1 },
        CommandWord{ 4, true, 5, 2 },
        BusLine::A,
    });

    ExpectTrue(result.sourceTransfer.errorCode == ErrorCode::InvalidWord, "워드 수가 맞지 않는 RT ↔ RT 요청은 거부되어야 합니다.");
    ExpectTrue(!result.destinationTransfer.has_value(), "잘못된 요청은 목적지 receive를 시작하면 안 됩니다.");
    ExpectEqual(monitor.GetEvents().size(), static_cast<std::size_t>(0U), "잘못된 요청은 이벤트를 남기지 않아야 합니다.");
}

void SimulatorAdapterRejectsModeCodeRtToRtTransfer()
{
    SimulatorBusAdapter adapter;
    adapter.SetStatusWord(4, StatusWord{ 4, false, false, false, false, false, false, false });
    adapter.SetStatusWord(9, StatusWord{ 9, false, false, false, false, false, false, false });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.ExecuteRtToRtTransfer(RtToRtTransferRequest{
        CommandWord::CreateModeCommand(9, false, ModeCode::SynchronizeWithDataWord),
        CommandWord{ 4, true, 5, 1 },
        BusLine::A,
    });

    ExpectTrue(result.sourceTransfer.errorCode == ErrorCode::InvalidWord, "Mode Code RT↔RT 요청은 거부되어야 합니다.");
    ExpectTrue(!result.destinationTransfer.has_value(), "거부된 RT↔RT 요청은 목적지 receive를 시작하면 안 됩니다.");
    ExpectEqual(monitor.GetEvents().size(), static_cast<std::size_t>(0U), "거부된 Mode Code RT↔RT 요청은 이벤트를 남기지 않아야 합니다.");
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

void SimulatorAdapterSupportsSynchronizeModeCode()
{
    SimulatorBusAdapter adapter;
    adapter.SetStatusWord(10, StatusWord{ 10, false, false, false, false, false, false, false });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(10, false, ModeCode::Synchronize),
        BusLine::A,
        {},
    });

    ExpectTrue(result.errorCode == ErrorCode::None, "Synchronize mode code가 성공해야 합니다.");
    ExpectEqual(result.dataWords.size(), static_cast<std::size_t>(0U), "Synchronize는 data word를 반환하지 않아야 합니다.");
}

void SimulatorAdapterSupportsSynchronizeWithDataWordModeCode()
{
    SimulatorBusAdapter adapter;
    adapter.SetStatusWord(10, StatusWord{ 10, false, false, false, false, false, false, false });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(10, false, ModeCode::SynchronizeWithDataWord),
        BusLine::A,
        { DataWord{ 0x0F0FU } },
    });

    ExpectTrue(result.errorCode == ErrorCode::None, "Synchronize With Data Word가 성공해야 합니다.");
    const auto lastSyncWord = adapter.GetLastSynchronizationWord(10);
    ExpectTrue(lastSyncWord.has_value(), "마지막 synchronize data word가 저장되어야 합니다.");
    ExpectEqual(lastSyncWord->value, static_cast<std::uint16_t>(0x0F0FU), "synchronize data word");
}

void SimulatorAdapterSupportsTransmitVectorWordModeCode()
{
    SimulatorBusAdapter adapter;
    adapter.SetStatusWord(11, StatusWord{ 11, false, false, false, false, false, false, false });
    adapter.SetVectorWord(11, DataWord{ 0x4321U });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(11, true, ModeCode::TransmitVectorWord),
        BusLine::A,
        {},
    });

    ExpectTrue(result.errorCode == ErrorCode::None, "Transmit Vector Word mode code가 성공해야 합니다.");
    ExpectEqual(result.dataWords.size(), static_cast<std::size_t>(1U), "Vector Word 데이터 수");
    ExpectEqual(result.dataWords.front().value, static_cast<std::uint16_t>(0x4321U), "Vector Word 값");
}

void SimulatorAdapterSupportsTransmitLastCommandWordModeCode()
{
    SimulatorBusAdapter adapter;
    adapter.SetStatusWord(15, StatusWord{ 15, false, false, false, false, false, false, false });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const CommandWord previousCommand{ 15, false, 9, 2 };
    const auto previousResult = controller.Execute(TransferRequest{
        previousCommand,
        BusLine::A,
        { DataWord{ 0x5011U }, DataWord{ 0x5012U } },
    });
    ExpectTrue(previousResult.errorCode == ErrorCode::None, "직전 일반 커맨드가 먼저 성공해야 합니다.");

    const auto result = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(15, true, ModeCode::TransmitLastCommandWord),
        BusLine::A,
        {},
    });

    ExpectTrue(result.errorCode == ErrorCode::None, "Transmit Last Command Word mode code가 성공해야 합니다.");
    ExpectEqual(result.dataWords.size(), static_cast<std::size_t>(1U), "last command word 데이터 수");
    ExpectEqual(
        result.dataWords.front().value,
        previousCommand.ToRaw(),
        "last command word raw 값");
}

void SimulatorAdapterRejectsTransmitLastCommandWordWithoutHistory()
{
    SimulatorBusAdapter adapter;
    adapter.SetStatusWord(16, StatusWord{ 16, false, false, false, false, false, false, false });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(16, true, ModeCode::TransmitLastCommandWord),
        BusLine::A,
        {},
    });

    ExpectTrue(result.errorCode == ErrorCode::InvalidWord, "이전 커맨드가 없으면 Transmit Last Command Word는 실패해야 합니다.");
}

void SimulatorAdapterSupportsInhibitAndOverrideTerminalFlagModeCodes()
{
    SimulatorBusAdapter adapter;
    adapter.SetStatusWord(17, StatusWord{ 17, false, false, false, false, false, false, true });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto inhibitResult = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(17, false, ModeCode::InhibitTerminalFlag),
        BusLine::A,
        {},
    });
    ExpectTrue(inhibitResult.errorCode == ErrorCode::None, "Inhibit Terminal Flag mode code가 성공해야 합니다.");
    ExpectTrue(inhibitResult.statusWord.has_value(), "Inhibit Terminal Flag 응답에도 상태 워드가 있어야 합니다.");
    ExpectTrue(!inhibitResult.statusWord->HasTerminalFlag(), "inhibit 후 응답 status는 terminal flag를 숨겨야 합니다.");

    const auto inhibitedStatusResult = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(17, true, ModeCode::TransmitStatusWord),
        BusLine::A,
        {},
    });
    ExpectTrue(inhibitedStatusResult.errorCode == ErrorCode::None, "inhibit 이후 Transmit Status Word가 성공해야 합니다.");
    ExpectTrue(inhibitedStatusResult.statusWord.has_value(), "상태 워드가 반환되어야 합니다.");
    ExpectTrue(!inhibitedStatusResult.statusWord->HasTerminalFlag(), "terminal flag가 숨겨져야 합니다.");

    const auto overrideResult = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(17, false, ModeCode::OverrideInhibitTerminalFlag),
        BusLine::A,
        {},
    });
    ExpectTrue(overrideResult.errorCode == ErrorCode::None, "Override Inhibit Terminal Flag mode code가 성공해야 합니다.");
    ExpectTrue(overrideResult.statusWord.has_value(), "Override 응답에도 상태 워드가 있어야 합니다.");
    ExpectTrue(overrideResult.statusWord->HasTerminalFlag(), "override 후 응답 status는 terminal flag를 다시 노출해야 합니다.");

    const auto restoredStatusResult = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(17, true, ModeCode::TransmitStatusWord),
        BusLine::A,
        {},
    });
    ExpectTrue(restoredStatusResult.errorCode == ErrorCode::None, "override 이후 Transmit Status Word가 성공해야 합니다.");
    ExpectTrue(restoredStatusResult.statusWord.has_value(), "상태 워드가 반환되어야 합니다.");
    ExpectTrue(restoredStatusResult.statusWord->HasTerminalFlag(), "terminal flag가 다시 보여야 합니다.");
}

void SimulatorAdapterResetsRemoteTerminalState()
{
    SimulatorBusAdapter adapter;
    adapter.SetStatusWord(18, StatusWord{ 18, false, true, false, false, false, false, true });
    adapter.SetSubAddressData(18, 2, { DataWord{ 0x6011U } });
    adapter.SetBitWord(18, DataWord{ 0x6022U });
    adapter.SetVectorWord(18, DataWord{ 0x6033U });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto syncResult = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(18, false, ModeCode::SynchronizeWithDataWord),
        BusLine::A,
        { DataWord{ 0x6044U } },
    });
    ExpectTrue(syncResult.errorCode == ErrorCode::None, "reset 전 synchronize with data가 먼저 성공해야 합니다.");

    const auto inhibitResult = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(18, false, ModeCode::InhibitTerminalFlag),
        BusLine::A,
        {},
    });
    ExpectTrue(inhibitResult.errorCode == ErrorCode::None, "reset 전 inhibit가 먼저 성공해야 합니다.");

    const auto resetResult = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(18, false, ModeCode::ResetRemoteTerminal),
        BusLine::A,
        {},
    });
    ExpectTrue(resetResult.errorCode == ErrorCode::None, "Reset Remote Terminal mode code가 성공해야 합니다.");

    ExpectTrue(adapter.GetSubAddressData(18, 2).empty(), "reset 이후 서브어드레스 데이터가 비워져야 합니다.");
    ExpectTrue(!adapter.GetLastSynchronizationWord(18).has_value(), "reset 이후 마지막 synchronize word가 비워져야 합니다.");

    const auto statusResult = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(18, true, ModeCode::TransmitStatusWord),
        BusLine::A,
        {},
    });
    ExpectTrue(statusResult.errorCode == ErrorCode::None, "reset 이후 Transmit Status Word가 성공해야 합니다.");
    ExpectTrue(statusResult.statusWord.has_value(), "상태 워드가 반환되어야 합니다.");
    ExpectTrue(!statusResult.statusWord->HasServiceRequest(), "reset 이후 service request가 없어야 합니다.");
    ExpectTrue(!statusResult.statusWord->HasTerminalFlag(), "reset 이후 terminal flag가 없어야 합니다.");

    const auto bitResult = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(18, true, ModeCode::TransmitBitWord),
        BusLine::A,
        {},
    });
    ExpectTrue(bitResult.errorCode == ErrorCode::None, "reset 이후 Transmit BIT Word가 성공해야 합니다.");
    ExpectEqual(bitResult.dataWords.size(), static_cast<std::size_t>(1U), "reset 이후 BIT Word 데이터 수");
    ExpectEqual(bitResult.dataWords.front().value, static_cast<std::uint16_t>(0U), "reset 이후 BIT Word 값");
}

void SimulatorAdapterKeepsDynamicBusControlUnsupported()
{
    SimulatorBusAdapter adapter;
    adapter.SetStatusWord(19, StatusWord{ 19, false, false, false, false, false, false, false });

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.Execute(TransferRequest{
        CommandWord::CreateModeCommand(19, false, ModeCode::DynamicBusControl),
        BusLine::A,
        {},
    });

    ExpectTrue(result.errorCode == ErrorCode::UnsupportedModeCode, "2차 묶음인 Dynamic Bus Control은 계속 미지원이어야 합니다.");
}

void SimulatorAdapterRecoversFromLineFaultByFailingOver()
{
    SimulatorBusAdapter adapter;
    adapter.SetSubAddressData(12, 4, { DataWord{ 0x5511U } });
    adapter.SetStatusWord(12, StatusWord{ 12, false, false, false, false, false, false, false });
    adapter.SetLineFault(BusLine::A, true);

    FakeClock clock(std::chrono::microseconds{ 1000 });
    BusMonitorService monitor;
    BusControllerService controller(adapter, clock, monitor, HealthSnapshot::CreateDefault());

    const auto result = controller.Execute(TransferRequest{
        CommandWord{ 12, true, 4, 1 },
        BusLine::A,
        {},
    });

    ExpectTrue(result.errorCode == ErrorCode::None, "line fault 이후 B 버스 재전송이 성공해야 합니다.");
    ExpectTrue(controller.GetHealthSnapshot().activeBus == BusLine::B, "line fault 이후 active bus가 B여야 합니다.");
    ExpectEqual(controller.GetHealthSnapshot().autoFailoverCount, static_cast<std::uint32_t>(1U), "auto failover count");
    ExpectEqual(monitor.GetEvents().size(), static_cast<std::size_t>(2U), "이벤트 수");
    ExpectTrue(monitor.GetEvents()[0].type == TelemetryEventType::AutoFailover, "자동 failover 이벤트가 기록되어야 합니다.");
    ExpectTrue(monitor.GetEvents()[1].type == TelemetryEventType::MessageFrame, "재전송 메시지가 기록되어야 합니다.");
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

    wchar_t telemetryJsonBuffer[1024]{};
    int requiredTelemetryJsonCapacity = 0;
    const auto pollStatus = MilStd1553_PollTelemetry(
        sessionHandleBuffer,
        telemetryJsonBuffer,
        1024,
        &requiredTelemetryJsonCapacity);
    ExpectEqual(pollStatus, 0, "PollTelemetry status");
    ExpectTrue(std::wstring_view(telemetryJsonBuffer).find(L"\"type\":\"MessageFrame\"") != std::wstring_view::npos, "초기 schedule pass의 message frame 이벤트가 반환되어야 합니다.");
    ExpectTrue(std::wstring_view(telemetryJsonBuffer).find(L"\"activeBus\":\"B\"") != std::wstring_view::npos, "초기 message frame은 B bus 기준이어야 합니다.");
    ExpectTrue(std::wstring_view(telemetryJsonBuffer).find(L"\"commandWordRaw\":") != std::wstring_view::npos, "message frame command word가 포함되어야 합니다.");

    const auto switchStatus = MilStd1553_SwitchBus(sessionHandleBuffer, 0);
    ExpectEqual(switchStatus, 0, "SwitchBus status");

    wchar_t switchedTelemetryJsonBuffer[1024]{};
    const auto switchedPollStatus = MilStd1553_PollTelemetry(
        sessionHandleBuffer,
        switchedTelemetryJsonBuffer,
        1024,
        &requiredTelemetryJsonCapacity);
    ExpectEqual(switchedPollStatus, 0, "Switch 이후 PollTelemetry status");
    ExpectTrue(std::wstring_view(switchedTelemetryJsonBuffer).find(L"\"type\":\"BusSwitch\"") != std::wstring_view::npos, "버스 전환 이벤트가 반환되어야 합니다.");
    ExpectTrue(std::wstring_view(switchedTelemetryJsonBuffer).find(L"\"activeBus\":\"A\"") != std::wstring_view::npos, "전환 후 active bus가 A여야 합니다.");

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

void VendorSdkBusAdapterLazilyOpensChannelAndCachesCapabilities()
{
    FakeVendorChannel vendorChannel;
    vendorChannel.capabilities = VendorAdapterCapabilities{
        true,
        true,
        true,
        true,
        true,
        true,
    };
    vendorChannel.transferResult = VendorTransferResult{
        VendorSdkStatusCode::Success,
        StatusWord{ 5, false, false, false, false, false, false, false },
        { DataWord{ 0xABCDU } },
        std::chrono::microseconds{ 910 },
        {},
    };

    const VendorChannelConfiguration configuration{
        7,
        BusLine::A,
        true,
        true,
        true,
        std::optional<std::uint8_t>{ 5U },
        std::chrono::microseconds{ 2500 },
    };
    DefaultVendorErrorMapper errorMapper;
    VendorSdkBusAdapter adapter(vendorChannel, errorMapper, configuration);

    const auto& capabilities = adapter.GetCapabilities();
    ExpectTrue(capabilities.supportsBusController, "BC capability가 유지돼야 합니다.");
    ExpectTrue(capabilities.supportsRemoteTerminal, "RT capability가 유지돼야 합니다.");
    ExpectTrue(capabilities.supportsBusMonitor, "BM capability가 유지돼야 합니다.");
    ExpectTrue(capabilities.supportsAutomaticFailoverAssist, "failover assist capability가 유지돼야 합니다.");
    ExpectTrue(capabilities.supportsTimeTag, "time-tag capability가 유지돼야 합니다.");
    ExpectTrue(capabilities.supportsLineFaultDetection, "line fault capability가 유지돼야 합니다.");
    ExpectTrue(!adapter.IsOpen(), "초기 상태에서는 channel이 열려 있지 않아야 합니다.");

    const auto result = adapter.Send(TransferRequest{
        CommandWord{ 5, true, 3, 1 },
        BusLine::A,
        {},
    });

    ExpectEqual(vendorChannel.openCallCount, 1, "lazy open 호출 횟수");
    ExpectEqual(vendorChannel.submitTransferCallCount, 1, "전송 호출 횟수");
    ExpectTrue(adapter.IsOpen(), "첫 전송 이후 channel이 열린 상태여야 합니다.");
    ExpectTrue(result.errorCode == ErrorCode::None, "lazy open 이후 전송이 성공해야 합니다.");
    ExpectEqual(result.dataWords.size(), static_cast<std::size_t>(1U), "반환 data word 수");
    ExpectEqual(result.dataWords.front().value, static_cast<std::uint16_t>(0xABCDU), "반환 data word");
}

void VendorSdkBusAdapterMapsVendorStatusCodesToDomainErrors()
{
    DefaultVendorErrorMapper errorMapper;

    {
        FakeVendorChannel vendorChannel;
        vendorChannel.transferResult = VendorTransferResult{
            VendorSdkStatusCode::Timeout,
            std::nullopt,
            {},
            std::chrono::microseconds{ 0 },
            {},
        };

        VendorSdkBusAdapter adapter(vendorChannel, errorMapper, VendorChannelConfiguration{});
        const auto result = adapter.Send(TransferRequest{
            CommandWord{ 2, true, 1, 1 },
            BusLine::A,
            {},
        });

        ExpectTrue(result.errorCode == ErrorCode::Timeout, "vendor timeout은 domain timeout으로 매핑돼야 합니다.");
    }

    {
        FakeVendorChannel vendorChannel;
        vendorChannel.transferResult = VendorTransferResult{
            VendorSdkStatusCode::DeviceFailure,
            std::nullopt,
            {},
            std::chrono::microseconds{ 0 },
            {},
        };

        VendorSdkBusAdapter adapter(vendorChannel, errorMapper, VendorChannelConfiguration{});
        const auto result = adapter.Send(TransferRequest{
            CommandWord{ 2, true, 1, 1 },
            BusLine::A,
            {},
        });

        ExpectTrue(result.errorCode == ErrorCode::AdapterFailure, "device failure는 adapter failure로 매핑돼야 합니다.");
    }
}

void VendorSdkBusAdapterAppliesPendingBusSelectionBeforeOpenAndForwardsAfterOpen()
{
    FakeVendorChannel vendorChannel;
    vendorChannel.transferResult = VendorTransferResult{
        VendorSdkStatusCode::Success,
        StatusWord{ 6, false, false, false, false, false, false, false },
        {},
        std::chrono::microseconds{ 120 },
        {},
    };
    DefaultVendorErrorMapper errorMapper;
    VendorSdkBusAdapter adapter(vendorChannel, errorMapper, VendorChannelConfiguration{});

    adapter.SelectBus(BusLine::B);

    ExpectEqual(vendorChannel.selectBusCallCount, 0, "open 이전 select bus 전달 횟수");
    ExpectTrue(!adapter.IsOpen(), "open 이전 select bus는 channel을 열지 않아야 합니다.");

    const auto sendResult = adapter.Send(TransferRequest{
        CommandWord{ 6, true, 4, 0 },
        BusLine::B,
        {},
    });

    ExpectTrue(sendResult.errorCode == ErrorCode::None, "pending bus 적용 후 전송이 성공해야 합니다.");
    ExpectTrue(vendorChannel.lastOpenConfiguration.has_value(), "lazy open 구성 정보가 기록돼야 합니다.");
    ExpectTrue(
        vendorChannel.lastOpenConfiguration->initialBus == BusLine::B,
        "open 이전에 선택한 bus가 open 구성에 반영돼야 합니다.");

    adapter.SelectBus(BusLine::A);

    ExpectEqual(vendorChannel.selectBusCallCount, 1, "open 이후 select bus 전달 횟수");
    ExpectTrue(vendorChannel.lastSelectedBus == BusLine::A, "open 이후 select bus는 vendor channel로 전달돼야 합니다.");
}

void SimulatorVendorChannelRejectsOperationsBeforeOpen()
{
    SimulatorVendorChannel channel;

    const auto transferResult = channel.SubmitTransfer(TransferRequest{
        CommandWord{ 3, true, 1, 1 },
        BusLine::A,
        {},
    });
    const auto selectResult = channel.SelectBus(BusLine::B);

    ExpectTrue(!channel.IsOpen(), "open 이전에는 channel이 닫혀 있어야 합니다.");
    ExpectTrue(transferResult.statusCode == VendorSdkStatusCode::ChannelNotOpen, "open 이전 transfer는 ChannelNotOpen이어야 합니다.");
    ExpectTrue(selectResult.statusCode == VendorSdkStatusCode::ChannelNotOpen, "open 이전 select bus는 ChannelNotOpen이어야 합니다.");
}

void SimulatorVendorChannelAppliesInitialBusAndSelectedBusToTransfers()
{
    SimulatorVendorChannel channel;
    channel.SetSubAddressData(4, 2, { DataWord{ 0x4455U } });
    channel.SetStatusWord(4, StatusWord{ 4, false, false, false, false, false, false, false });
    channel.SetLineFault(BusLine::B, true);

    const auto openResult = channel.Open(VendorChannelConfiguration{
        2,
        BusLine::B,
        true,
        true,
        true,
        std::nullopt,
        std::chrono::microseconds{ 1000 },
    });

    ExpectTrue(openResult.IsSuccess(), "channel open이 성공해야 합니다.");
    ExpectTrue(channel.IsOpen(), "open 이후 channel이 열려 있어야 합니다.");
    ExpectTrue(channel.GetSelectedBus() == BusLine::B, "initial bus가 selected bus에 반영돼야 합니다.");
    ExpectTrue(channel.GetLastConfiguration().has_value(), "open configuration이 저장돼야 합니다.");
    ExpectTrue(channel.GetLastConfiguration()->initialBus == BusLine::B, "저장된 initial bus가 B여야 합니다.");

    const auto lineFaultResult = channel.SubmitTransfer(TransferRequest{
        CommandWord{ 4, true, 2, 1 },
        BusLine::A,
        {},
    });

    ExpectTrue(lineFaultResult.statusCode == VendorSdkStatusCode::LineFault, "selected bus가 B이면 B line fault가 반영돼야 합니다.");

    channel.SetLineFault(BusLine::B, false);
    const auto selectResult = channel.SelectBus(BusLine::A);
    ExpectTrue(selectResult.IsSuccess(), "open 이후 select bus가 성공해야 합니다.");
    ExpectTrue(channel.GetSelectedBus() == BusLine::A, "selected bus가 A로 바뀌어야 합니다.");

    const auto successResult = channel.SubmitTransfer(TransferRequest{
        CommandWord{ 4, true, 2, 1 },
        BusLine::B,
        {},
    });

    ExpectTrue(successResult.statusCode == VendorSdkStatusCode::Success, "selected bus를 바꾼 뒤 transfer가 성공해야 합니다.");
    ExpectEqual(successResult.dataWords.size(), static_cast<std::size_t>(1U), "시뮬레이터 vendor channel data word 수");
    ExpectEqual(successResult.dataWords.front().value, static_cast<std::uint16_t>(0x4455U), "시뮬레이터 vendor channel data word");

    const auto closeResult = channel.Close();
    ExpectTrue(closeResult.IsSuccess(), "channel close가 성공해야 합니다.");
    ExpectTrue(!channel.IsOpen(), "close 이후 channel이 닫혀 있어야 합니다.");
}

void VendorSdkBusAdapterWorksWithSimulatorVendorChannelConcreteBinding()
{
    SimulatorVendorChannel channel;
    channel.SetSubAddressData(9, 5, { DataWord{ 0x9901U }, DataWord{ 0x9902U } });
    channel.SetStatusWord(9, StatusWord{ 9, false, true, false, false, false, false, false });

    DefaultVendorErrorMapper errorMapper;
    VendorSdkBusAdapter adapter(
        channel,
        errorMapper,
        VendorChannelConfiguration{
            0,
            BusLine::A,
            true,
            true,
            true,
            std::optional<std::uint8_t>{ 9U },
            std::chrono::microseconds{ 1500 },
        });

    adapter.SelectBus(BusLine::B);

    const auto result = adapter.Send(TransferRequest{
        CommandWord{ 9, true, 5, 2 },
        BusLine::A,
        {},
    });

    ExpectTrue(adapter.IsOpen(), "concrete binding 경로에서도 lazy open이 동작해야 합니다.");
    ExpectTrue(channel.IsOpen(), "underlying channel이 실제로 열려야 합니다.");
    ExpectTrue(channel.GetSelectedBus() == BusLine::B, "open 이전 선택한 bus가 concrete channel open에 반영돼야 합니다.");
    ExpectTrue(result.errorCode == ErrorCode::None, "concrete binding 경로에서 전송이 성공해야 합니다.");
    ExpectTrue(result.statusWord.has_value(), "status word가 반환돼야 합니다.");
    ExpectTrue(result.statusWord->HasServiceRequest(), "status word 비트가 유지돼야 합니다.");
    ExpectEqual(result.dataWords.size(), static_cast<std::size_t>(2U), "concrete binding data word 수");
    ExpectEqual(result.dataWords[0].value, static_cast<std::uint16_t>(0x9901U), "첫 번째 concrete binding data word");
    ExpectEqual(result.dataWords[1].value, static_cast<std::uint16_t>(0x9902U), "두 번째 concrete binding data word");
}

int RunAllTests()
{
    const std::vector<std::pair<std::string, std::function<void()>>> tests = {
        { "CommandWordParsesRegularTransfer", CommandWordParsesRegularTransfer },
        { "CommandWordIdentifiesModeCodeWithDataWord", CommandWordIdentifiesModeCodeWithDataWord },
        { "StatusWordParsesRelevantFlags", StatusWordParsesRelevantFlags },
        { "HealthSnapshotTracksTimeoutAndBusSwitch", HealthSnapshotTracksTimeoutAndBusSwitch },
        { "BusControllerRetriesOnceAfterTimeoutAndPublishesMessage", BusControllerRetriesOnceAfterTimeoutAndPublishesMessage },
        { "BusControllerPublishesReceivePayloadInMessageFrame", BusControllerPublishesReceivePayloadInMessageFrame },
        { "BusControllerSwitchesBusAndPublishesSwitchEvent", BusControllerSwitchesBusAndPublishesSwitchEvent },
        { "BusControllerAutomaticallyFailsOverAfterConsecutiveTimeouts", BusControllerAutomaticallyFailsOverAfterConsecutiveTimeouts },
        { "BusMonitorStoresMessageFrameEvent", BusMonitorStoresMessageFrameEvent },
        { "BusMonitorPersistsJsonLinesEventLog", BusMonitorPersistsJsonLinesEventLog },
        { "SimulatorAdapterSupportsRtToBcTransfer", SimulatorAdapterSupportsRtToBcTransfer },
        { "SimulatorAdapterSupportsRtToRtTransfer", SimulatorAdapterSupportsRtToRtTransfer },
        { "SimulatorAdapterRejectsInvalidRtToRtTransfer", SimulatorAdapterRejectsInvalidRtToRtTransfer },
        { "SimulatorAdapterRejectsModeCodeRtToRtTransfer", SimulatorAdapterRejectsModeCodeRtToRtTransfer },
        { "SimulatorAdapterSupportsTransmitStatusWordModeCode", SimulatorAdapterSupportsTransmitStatusWordModeCode },
        { "SimulatorAdapterSupportsTransmitBitWordModeCode", SimulatorAdapterSupportsTransmitBitWordModeCode },
        { "SimulatorAdapterSupportsSynchronizeModeCode", SimulatorAdapterSupportsSynchronizeModeCode },
        { "SimulatorAdapterSupportsSynchronizeWithDataWordModeCode", SimulatorAdapterSupportsSynchronizeWithDataWordModeCode },
        { "SimulatorAdapterSupportsTransmitVectorWordModeCode", SimulatorAdapterSupportsTransmitVectorWordModeCode },
        { "SimulatorAdapterSupportsTransmitLastCommandWordModeCode", SimulatorAdapterSupportsTransmitLastCommandWordModeCode },
        { "SimulatorAdapterRejectsTransmitLastCommandWordWithoutHistory", SimulatorAdapterRejectsTransmitLastCommandWordWithoutHistory },
        { "SimulatorAdapterSupportsInhibitAndOverrideTerminalFlagModeCodes", SimulatorAdapterSupportsInhibitAndOverrideTerminalFlagModeCodes },
        { "SimulatorAdapterResetsRemoteTerminalState", SimulatorAdapterResetsRemoteTerminalState },
        { "SimulatorAdapterKeepsDynamicBusControlUnsupported", SimulatorAdapterKeepsDynamicBusControlUnsupported },
        { "SimulatorAdapterRecoversFromLineFaultByFailingOver", SimulatorAdapterRecoversFromLineFaultByFailingOver },
        { "NativeSessionApiManagesSessionLifecycle", NativeSessionApiManagesSessionLifecycle },
        { "NativeSessionApiReturnsRequiredCapacitiesWhenOpenSessionBufferIsTooSmall", NativeSessionApiReturnsRequiredCapacitiesWhenOpenSessionBufferIsTooSmall },
        { "NativeSessionApiKeepsPendingTelemetryWhenPollBufferIsTooSmall", NativeSessionApiKeepsPendingTelemetryWhenPollBufferIsTooSmall },
        { "VendorSdkBusAdapterLazilyOpensChannelAndCachesCapabilities", VendorSdkBusAdapterLazilyOpensChannelAndCachesCapabilities },
        { "VendorSdkBusAdapterMapsVendorStatusCodesToDomainErrors", VendorSdkBusAdapterMapsVendorStatusCodesToDomainErrors },
        { "VendorSdkBusAdapterAppliesPendingBusSelectionBeforeOpenAndForwardsAfterOpen", VendorSdkBusAdapterAppliesPendingBusSelectionBeforeOpenAndForwardsAfterOpen },
        { "SimulatorVendorChannelRejectsOperationsBeforeOpen", SimulatorVendorChannelRejectsOperationsBeforeOpen },
        { "SimulatorVendorChannelAppliesInitialBusAndSelectedBusToTransfers", SimulatorVendorChannelAppliesInitialBusAndSelectedBusToTransfers },
        { "VendorSdkBusAdapterWorksWithSimulatorVendorChannelConcreteBinding", VendorSdkBusAdapterWorksWithSimulatorVendorChannelConcreteBinding },
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
