using MilStd1553.Host.Models;

namespace MilStd1553.Host.Contracts;

/// <summary>
/// Host 실행 결과를 외부 포맷으로 내보내는 계약입니다.
/// </summary>
public interface IReportExporter
{
    /// <summary>
    /// 실행 결과를 문자열 보고서로 직렬화합니다.
    /// </summary>
    /// <param name="report">직렬화할 실행 결과입니다.</param>
    /// <returns>직렬화된 보고서 문자열입니다.</returns>
    string Export(SessionExecutionReport report);
}
