var builder = DistributedApplication.CreateBuilder(args);

var mysqlproduct = builder.AddMySql("Productdb").AddDatabase("product");
var mysqlorder = builder.AddMySql("Orderdb").AddDatabase("order");

var apiProductService = builder.AddProject<Projects.ProductService_API>("apiservice-product").WithReference(mysqlproduct);

var apiOrderService = builder.AddProject<Projects.OrderService_API>("apiservice-order").WithReference(mysqlorder);

builder.Build().Run();
