#pragma once

#include "Application/Contracts.h"

#include <filesystem>
#include <string>
#include <string_view>

namespace MilStd1553::Infrastructure
{
/// <summary>
/// BM 텔레메트리 이벤트를 JSONL 파일에 append합니다.
/// </summary>
class JsonLinesBusEventStore final : public Application::IBusEventStore
{
public:
    /// <summary>
    /// JSONL 저장소를 생성합니다.
    /// </summary>
    /// <param name="filePath">출력 JSONL 파일 경로입니다.</param>
    explicit JsonLinesBusEventStore(std::filesystem::path filePath);

    /// <summary>
    /// 텔레메트리 이벤트를 JSONL 한 줄로 append합니다.
    /// </summary>
    /// <param name="event">기록할 이벤트입니다.</param>
    void Append(const Domain::TelemetryEvent& event) override;

    /// <summary>
    /// 대상 파일 경로를 반환합니다.
    /// </summary>
    /// <returns>JSONL 파일 경로입니다.</returns>
    [[nodiscard]] const std::filesystem::path& GetFilePath() const noexcept;

private:
    /// <summary>
    /// 텔레메트리 이벤트를 JSON 문자열로 직렬화합니다.
    /// </summary>
    /// <param name="event">직렬화할 이벤트입니다.</param>
    /// <returns>JSON 문자열입니다.</returns>
    [[nodiscard]] std::string Serialize(const Domain::TelemetryEvent& event) const;

    /// <summary>
    /// JSON 문자열 리터럴용으로 문자를 escape합니다.
    /// </summary>
    /// <param name="value">원본 문자열입니다.</param>
    /// <returns>escape된 문자열입니다.</returns>
    [[nodiscard]] static std::string EscapeJson(std::string_view value);

    /// <summary>
    /// 버스 라인을 문자열로 직렬화합니다.
    /// </summary>
    /// <param name="busLine">직렬화할 버스 라인입니다.</param>
    /// <returns>문자열 표현입니다.</returns>
    [[nodiscard]] static std::string SerializeBusLine(Domain::BusLine busLine);

    /// <summary>
    /// 이벤트 유형을 문자열로 직렬화합니다.
    /// </summary>
    /// <param name="eventType">직렬화할 이벤트 유형입니다.</param>
    /// <returns>문자열 표현입니다.</returns>
    [[nodiscard]] static std::string SerializeEventType(Domain::TelemetryEventType eventType);

    /// <summary>
    /// 데이터 워드 배열을 JSON 배열 문자열로 직렬화합니다.
    /// </summary>
    /// <param name="dataWords">직렬화할 데이터 워드 목록입니다.</param>
    /// <returns>JSON 배열 문자열입니다.</returns>
    [[nodiscard]] static std::string SerializeDataWords(const std::vector<Domain::DataWord>& dataWords);

    std::filesystem::path filePath_;
};
}
