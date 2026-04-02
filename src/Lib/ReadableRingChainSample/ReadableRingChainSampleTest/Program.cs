using ReadableRingChainSample.Domain;
using ReadableRingChainSample.Infra;
using ReadableRingChainSampleTest;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var logger = new ConsoleLogger();
        var transport = new FakeDeviceTransport();
        var scenario = new DeviceScenario(transport);

        var runner = scenario.Build(logger);
        var initialState = DeviceSessionState.Create("DEV-001");

        var result = await runner.RunAsync(
            startStepName: "HELLO",
            initialState: initialState,
            maxSteps: 10);

        Console.WriteLine();
        Console.WriteLine("===== FINAL RESULT =====");

        if (result.IsSuccess && result.Value is not null)
        {
            Console.WriteLine("Scenario succeeded.");
            Console.WriteLine($"DeviceId      : {result.Value.DeviceId}");
            Console.WriteLine($"Handshaked    : {result.Value.Handshaked}");
            Console.WriteLine($"Authenticated : {result.Value.Authenticated}");
            Console.WriteLine($"Token         : {result.Value.Token}");
            Console.WriteLine($"Data          : {result.Value.Data}");
            Console.WriteLine("Logs:");

            foreach (var log in result.Value.Logs)
            {
                Console.WriteLine($" - {log}");
            }
        }
        else
        {
            Console.WriteLine($"Scenario failed: {result.ErrorCode} / {result.ErrorMessage}");
        }
    }
}