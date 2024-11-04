using Microservice.Persistence.DatabaseContext;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

builder.AddNpgsqlDataSource("pgdb");

builder.AddNpgsqlDbContext<ProductDbContext>("product");

app.MapGet("/", () => "Hello World!");

app.Run();
