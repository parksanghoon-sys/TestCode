using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);



//builder.AddProject<Projects.Todo_API>("webapi");

var postgres = builder.AddPostgres("postgresdb")
    .WithPgAdmin().AddDatabase("todo");

//var mysql = builder.AddMySql("mysqldb").AddDatabase("todo");

var exampleProject = builder.AddProject<Projects.Todo_API>("webapi")
                            .WithReference(postgres);



builder.Build().Run();
