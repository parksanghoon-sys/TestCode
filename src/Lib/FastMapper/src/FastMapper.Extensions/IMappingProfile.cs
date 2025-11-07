namespace FastMapper.Extensions.DependencyInjection;

/// <summary>
/// 매핑 프로필 인터페이스 - 커스텀 매핑 규칙 정의
/// </summary>
public interface IMappingProfile
{
    /// <summary>
    /// 매핑 규칙 구성
    /// </summary>
    /// <param name="configuration">매핑 구성</param>
    void Configure(IMappingConfiguration configuration);
}
