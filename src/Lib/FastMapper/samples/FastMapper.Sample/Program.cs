using FastMapper.Extensions.DependencyInjection;
using FastMapper.Sample.Models;
using FastMapper.Sample.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FastMapper.Sample;

/// <summary>
/// FastMapper 사용 예시 프로그램
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 FastMapper 사용 예시 프로그램");
        Console.WriteLine("================================");

        // 호스트 빌더 설정
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // FastMapper 등록
                services.AddFastMapper();
                
                // FastMapper 옵션 구성
                services.ConfigureFastMapper(options =>
                {
                    options.EnablePerformanceMonitoring = true;
                    options.EnableValidation = true;
                    options.MaxDepth = 5;
                    options.ThreadSafe = true;
                });

                // 샘플 서비스 등록
                services.AddScoped<IUserService, UserService>();
                services.AddScoped<IOrderService, OrderService>();
                services.AddScoped<IMappingDemoService, MappingDemoService>();
            })
            .ConfigureLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        // 서비스 실행
        using var scope = host.Services.CreateScope();
        var demoService = scope.ServiceProvider.GetRequiredService<IMappingDemoService>();
        
        await demoService.RunDemonstrationAsync();

        Console.WriteLine("\n✅ 프로그램이 완료되었습니다. 아무 키나 누르세요...");
        Console.ReadKey();
    }
}
