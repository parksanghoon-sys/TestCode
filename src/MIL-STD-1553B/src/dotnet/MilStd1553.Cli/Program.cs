using System.Text;
using MilStd1553.Cli.Services;
using MilStd1553.Host.Services;
using MilStd1553.Interop.Services;

using var cancellationTokenSource = new CancellationTokenSource();

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

var nativeHarnessClient = new NativeHarnessClient();
var sessionCoordinator = new SessionCoordinator(
    new ScenarioDefinitionJsonLoader(),
    nativeHarnessClient);
var telemetryQueryService = new TelemetryQueryService(
    sessionCoordinator,
    nativeHarnessClient);
var runner = new HarnessCliRunner(
    sessionCoordinator,
    telemetryQueryService,
    new ScenarioFileReader(),
    Console.Out,
    Console.Error);

return await runner.RunAsync(args, cancellationTokenSource.Token);
