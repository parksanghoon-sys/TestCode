using Microservice.Persistence.DatabaseContext;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

builder.AddNpgsqlDataSource("pgdb");

builder.AddNpgsqlDbContext<OrderDbContext>("product");

builder.Services.AddSwaggerGen();

app.MapGet("/", () => "Hello World!");


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.Run();
