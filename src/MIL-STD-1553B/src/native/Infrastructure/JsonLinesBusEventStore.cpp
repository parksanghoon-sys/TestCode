#include "Infrastructure/JsonLinesBusEventStore.h"

#include <fstream>
#include <sstream>

namespace MilStd1553::Infrastructure
{
JsonLinesBusEventStore::JsonLinesBusEventStore(std::filesystem::path filePath)
    : filePath_(std::move(filePath))
{
}

void JsonLinesBusEventStore::Append(const Domain::TelemetryEvent& event)
{
    const auto parentPath = filePath_.parent_path();
    if (!parentPath.empty())
    {
        std::filesystem::create_directories(parentPath);
    }

    std::ofstream output(filePath_, std::ios::app | std::ios::binary);
    output << Serialize(event) << '\n';
}

const std::filesystem::path& JsonLinesBusEventStore::GetFilePath() const noexcept
{
    return filePath_;
}

std::string JsonLinesBusEventStore::Serialize(const Domain::TelemetryEvent& event) const
{
    std::ostringstream builder;
    builder
        << "{\"eventType\":\"" << SerializeEventType(event.type) << "\""
        << ",\"timeTagMicros\":" << event.timeTag.count()
        << ",\"activeBus\":\"" << SerializeBusLine(event.activeBus) << "\""
        << ",\"description\":\"" << EscapeJson(event.description) << "\""
        << ",\"messageFrame\":";

    if (!event.messageFrame.has_value())
    {
        builder << "null";
    }
    else
    {
        const auto& frame = event.messageFrame.value();
        builder
            << "{\"commandWordRaw\":" << frame.commandWord.ToRaw()
            << ",\"statusWordRaw\":";

        if (frame.statusWord.has_value())
        {
            builder << frame.statusWord->ToRaw();
        }
        else
        {
            builder << "null";
        }

        builder
            << ",\"dataWords\":" << SerializeDataWords(frame.dataWords)
            << ",\"busLine\":\"" << SerializeBusLine(frame.busLine) << "\""
            << ",\"timeTagMicros\":" << frame.timeTag.count()
            << "}";
    }

    builder << "}";
    return builder.str();
}

std::string JsonLinesBusEventStore::EscapeJson(const std::string_view value)
{
    std::string escaped;
    escaped.reserve(value.size());

    for (const char character : value)
    {
        switch (character)
        {
        case '\\':
            escaped += "\\\\";
            break;
        case '\"':
            escaped += "\\\"";
            break;
        case '\n':
            escaped += "\\n";
            break;
        case '\r':
            escaped += "\\r";
            break;
        case '\t':
            escaped += "\\t";
            break;
        default:
            escaped += character;
            break;
        }
    }

    return escaped;
}

std::string JsonLinesBusEventStore::SerializeBusLine(const Domain::BusLine busLine)
{
    return busLine == Domain::BusLine::A ? "A" : "B";
}

std::string JsonLinesBusEventStore::SerializeEventType(const Domain::TelemetryEventType eventType)
{
    switch (eventType)
    {
    case Domain::TelemetryEventType::MessageFrame:
        return "MessageFrame";
    case Domain::TelemetryEventType::BusSwitch:
        return "BusSwitch";
    case Domain::TelemetryEventType::AutoFailover:
        return "AutoFailover";
    default:
        return "Unknown";
    }
}

std::string JsonLinesBusEventStore::SerializeDataWords(const std::vector<Domain::DataWord>& dataWords)
{
    std::ostringstream builder;
    builder << '[';

    for (std::size_t index = 0; index < dataWords.size(); ++index)
    {
        if (index > 0)
        {
            builder << ',';
        }

        builder << dataWords[index].value;
    }

    builder << ']';
    return builder.str();
}
}
