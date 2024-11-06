using Microservice.Application.Repositories;
using Microservice.Persistence.Repositories;
using Microservice.Persistence;
using System.Reflection;
using OrderService.API.DatabaseContext;
using MediatR;
using OrderService.API.Features.Order.Queries.GetAllOrder;
using Microservice.Infrastructure.Models;
using Microservice.Application.Kafka;
using Microservice.Infrastructure.KafkaService;
using Microservice.Doamin;
using Confluent.Kafka;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using OrderService.API.Features.Order.Command.Create;

var builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults();
//builder.AddMySqlDbContext<OrderDbContext>("order");
builder.AddNpgsqlDataSource("pgdb");

builder.AddNpgsqlDbContext<OrderDbContext>("order");
builder.Services.AddOptions<KafkaConfig>().BindConfiguration(nameof(KafkaConfig));

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddAPersistenceServiceService();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddSingleton<IKafkaProducer<string,string>, KafkaProducer>();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await context.InitializeDatabaseAsync().ConfigureAwait(false);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});
app.MapGet("/order", (IMediator mediator) =>
    {
        var orderList = mediator.Send(new GetOrdersQuery());
        return orderList;         
    })
.WithName("Order")
.WithOpenApi();

app.MapPost("/order/postorder",async (CreateOrderCommand model, IMediator mediator, IKafkaProducer<string, string> kafkaProducer) =>
{
    var response = await mediator.Send(model);
    var pruductMessage = new OrderMessage
    {
        OrderId = model.OrderId,
        ProductId = model.ProductId,
        Quantity = 1
    };

    await kafkaProducer.ProduceAsync("order-topic", new Message<string, string>
    {
        Key = model.OrderId.ToString(),
        Value = JsonSerializer.Serialize(pruductMessage)
    });

    return response;

}).WithName("PostOrder")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
