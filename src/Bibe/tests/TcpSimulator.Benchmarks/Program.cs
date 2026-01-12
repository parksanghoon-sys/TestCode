using BenchmarkDotNet.Running;
using TcpSimulator.Benchmarks;

Console.WriteLine("=== TCP Simulator Performance Benchmarks ===");
Console.WriteLine();
Console.WriteLine("Select benchmark to run:");
Console.WriteLine("  1. Protocol Serialization Benchmarks");
Console.WriteLine("  2. Buffer Management Benchmarks");
Console.WriteLine("  3. CRC32 Calculation Benchmarks");
Console.WriteLine("  4. Message Dispatch Benchmarks");
Console.WriteLine("  5. End-to-End Communication Benchmarks");
Console.WriteLine("  6. Run All Benchmarks");
Console.WriteLine("  0. Exit");
Console.WriteLine();
Console.Write("Enter choice: ");

var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        BenchmarkRunner.Run<ProtocolSerializationBenchmarks>();
        break;
    case "2":
        BenchmarkRunner.Run<BufferManagementBenchmarks>();
        break;
    case "3":
        BenchmarkRunner.Run<Crc32CalculationBenchmarks>();
        break;
    case "4":
        BenchmarkRunner.Run<MessageDispatchBenchmarks>();
        break;
    case "5":
        BenchmarkRunner.Run<EndToEndBenchmarks>();
        break;
    case "6":
        BenchmarkRunner.Run<ProtocolSerializationBenchmarks>();
        BenchmarkRunner.Run<BufferManagementBenchmarks>();
        BenchmarkRunner.Run<Crc32CalculationBenchmarks>();
        BenchmarkRunner.Run<MessageDispatchBenchmarks>();
        BenchmarkRunner.Run<EndToEndBenchmarks>();
        break;
    case "0":
        Console.WriteLine("Exiting...");
        break;
    default:
        Console.WriteLine("Invalid choice. Exiting...");
        break;
}

Console.WriteLine();
Console.WriteLine("Benchmarks completed.");
