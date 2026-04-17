using Mes.Client.Wpf.Configuration;
using Mes.Client.Wpf.OperatorExecution;
using Mes.Client.Wpf.Shell;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WpfApplication = System.Windows.Application;
using ExitEventArgs = System.Windows.ExitEventArgs;
using StartupEventArgs = System.Windows.StartupEventArgs;

namespace Mes.Client.Wpf;

/// <summary>
/// WPF 스테이션 셸의 호스트 수명주기와 DI 구성을 담당합니다.
/// </summary>
public partial class App : WpfApplication
{
    private IHost? _host;

    /// <summary>
    /// 애플리케이션 시작 시 Generic Host를 구성하고 메인 창을 표시합니다.
    /// </summary>
    /// <param name="e">시작 인자입니다.</param>
    protected override async void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnStartup(e);

        _host = CreateHostBuilder().Build();
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    /// <summary>
    /// 애플리케이션 종료 시 Host를 정상 종료하고 리소스를 정리합니다.
    /// </summary>
    /// <param name="e">종료 인자입니다.</param>
    protected override async void OnExit(ExitEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        try
        {
            if (_host is not null)
            {
                await _host.StopAsync();
                _host.Dispose();
                _host = null;
            }
        }
        finally
        {
            base.OnExit(e);
        }
    }

    /// <summary>
    /// 스테이션 셸이 사용할 Generic Host 빌더를 생성합니다.
    /// </summary>
    /// <returns>구성된 호스트 빌더입니다.</returns>
    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<OperatorExecutionStationClientOptions>(
                    context.Configuration.GetSection("Mes:OperatorExecutionBff"));
                services.AddSingleton(TimeProvider.System);
                services.AddSingleton<OperatorExecutionProblemDisplayPolicy>();
                services.AddSingleton<StationCommandContextFactory>();
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<MainWindow>();
                services.AddHttpClient<IOperatorExecutionStationClient, OperatorExecutionStationClient>(
                    (serviceProvider, client) =>
                    {
                        var options = serviceProvider
                            .GetRequiredService<IOptions<OperatorExecutionStationClientOptions>>()
                            .Value;
                        client.BaseAddress = options.ResolveBaseUri();
                    });
            });
    }
}
