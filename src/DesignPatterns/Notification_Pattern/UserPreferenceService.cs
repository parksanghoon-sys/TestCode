
namespace Notification_Pattern
{
    internal class UserPreferenceService : IUserPreferenceService
    {
        // 더미 데이터 저장소
        private static readonly List<UserPreferences> _userPreferences = new()
            {
                new UserPreferences
                {
                    UserId = "user1",
                    EnabledChannels = new Dictionary<NotificationType, bool>
                    {
                        { NotificationType.Email, true },
                        { NotificationType.SMS, false },
                        { NotificationType.Push, true }
                    },
                    ChannelAddress = new Dictionary<NotificationType, string>
                    {
                        { NotificationType.Email, "user1@email.com" },
                        { NotificationType.SMS, "010-1111-1111" },
                        { NotificationType.Push, "push_token_user1" }
                    }
                },
                new UserPreferences
                {
                    UserId = "user2",
                    EnabledChannels = new Dictionary<NotificationType, bool>
                    {
                        { NotificationType.Email, false },
                        { NotificationType.SMS, true },
                        { NotificationType.Push, false }
                    },
                    ChannelAddress = new Dictionary<NotificationType, string>
                    {
                        { NotificationType.Email, "user2@email.com" },
                        { NotificationType.SMS, "010-2222-2222" },
                        { NotificationType.Push, "push_token_user2" }
                    }
                }
            };

        public Task<UserPreferences> GetUserPreferencesAsync(string userId)
        {
            var pref = _userPreferences.FirstOrDefault(u => u.UserId == userId);
            return Task.FromResult(pref);
        }
    }
    internal class TemplateService : ITemplateService
    {
        // 더미 템플릿 저장소
        private static readonly List<NotificationTemplate> _templates = new()
            {
                new NotificationTemplate
                {
                    Name = "welcome",
                    Type = NotificationType.Email,
                    Subject = "환영합니다, {UserName}님!",
                    Body = "안녕하세요 {UserName}님, 저희 서비스를 이용해주셔서 감사합니다."
                },
                new NotificationTemplate
                {
                    Name = "promotion",
                    Type = NotificationType.SMS,
                    Subject = "",
                    Body = "인증번호는 {Code} 입니다."
                },
                new NotificationTemplate
                {
                    Name = "promotion",
                    Type = NotificationType.SMS,
                    Subject = "알림",
                    Body = "{Message}"
                }
            };

        public Task<NotificationTemplate> GetTemplateAsync(string name, NotificationType type)
        {
            var template = _templates.FirstOrDefault(t => t.Name == name && t.Type == type);
            return Task.FromResult(template);
        }
    }
}