using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

string dockerComposeFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../..","kafka.yml"));

var processStartInfo = new ProcessStartInfo
{
    FileName = "docker-compose",
    Arguments = $"-f {dockerComposeFilePath} up --abort-on-container-exit --remove-orphans",
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

try
{
    // ���μ��� ����
    using (var process = new Process { StartInfo = processStartInfo })
    {
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // ��� ��� ǥ��
        Console.WriteLine("Output:");
        Console.WriteLine(output);

        // ������ ���� ��� ���
        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine("Error:");
            Console.WriteLine(error);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}

var mysqlproduct = builder.AddMySql("Productdb").AddDatabase("product");
var mysqlorder = builder.AddMySql("Orderdb").AddDatabase("order");

var apiProductService = builder.AddProject<Projects.ProductService_API>("apiservice-product").WithReference(mysqlproduct);

var apiOrderService = builder.AddProject<Projects.OrderService_API>("apiservice-order").WithReference(mysqlorder);

builder.Build().Run();
