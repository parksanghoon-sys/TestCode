namespace Notification_Pattern;

// 사용자 알림 환경설정 정보를 나타내는 클래스
public class UserPreferences
{
    // 사용자 ID
    public string UserId { get; set; } = string.Empty;
    // 알림 유형별 활성화 여부 (예: Email, SMS, Push)
    public Dictionary<NotificationType, bool> EnabledChannels { get; set; }
    // 알림 유형별 채널 주소 (예: 이메일 주소, 전화번호 등)
    public Dictionary<NotificationType, string> ChannelAddress { get; set; }

    // 특정 알림 유형이 활성화되어 있는지 확인
    public bool IsChannelEnabled(NotificationType type)
    {
        return EnabledChannels.GetValueOrDefault(type, false);
    }

    // 특정 알림 유형의 채널 주소 반환
    public string GetChannelAddress(NotificationType type)
    {
        return ChannelAddress.GetValueOrDefault(type, string.Empty);
    }
}
// 사용자 환경설정 정보를 비동기로 가져오는 서비스 인터페이스
public interface IUserPreferenceService
{
    Task<UserPreferences> GetUserPreferencesAsync(string userId);
}
// 템플릿 처리 결과(제목, 본문)를 담는 클래스
public class ProcessedTemplate
{
    public string Subject { get; set; }
    public string Body { get; set; }
}
// 알림 템플릿 정보를 나타내는 클래스
public class NotificationTemplate
{
    // 템플릿 이름
    public string Name { get; set; } = string.Empty;
    // 알림 유형
    public NotificationType Type { get; set; }
    // 템플릿 제목
    public string Subject { get; set; } = string.Empty;
    // 템플릿 본문
    public string Body { get; set; } = string.Empty;

    // 데이터로 템플릿을 처리하여 결과 반환
    public ProcessedTemplate Process(Dictionary<string, object> data)
    {
        return new ProcessedTemplate
        {
            Subject = ProcessTemplate(Subject, data),
            Body = ProcessTemplate(Body, data)
        };
    }

    // 템플릿 문자열 내의 플레이스홀더를 데이터로 치환
    public string ProcessTemplate(string template, Dictionary<string, object> datas)
    {
        var result = template;
        foreach (var kvp in datas)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? string.Empty);
        }
        return result;
    }
}
// 템플릿 정보를 비동기로 가져오는 서비스 인터페이스
public interface ITemplateService
{
    Task<NotificationTemplate> GetTemplateAsync(string name, NotificationType type);
}
