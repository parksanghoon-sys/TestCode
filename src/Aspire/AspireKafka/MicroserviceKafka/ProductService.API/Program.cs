using Microservice.Persistence.DatabaseContext;
using Microservice.Application;
using Microservice.Persistence;
using Microservice.Application.Repositories;
using Microservice.Persistence.Repositories;
using Autofac.Core;
using System.Reflection;
using ProductService.API.DatabaseContext;
using Microservice.Infrastructure.Models;
using Microservice.Application.Kafka;
using Microservice.Infrastructure.KafkaService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
//builder.AddMySqlDbContext<ProductDbContext>("product");
builder.AddNpgsqlDataSource("pgdb");

builder.AddNpgsqlDbContext<ProductDbContext>("product");
var test = builder.Configuration.GetSection("KafkaConfig");



builder.Services.Configure<KafkaConfig>(builder.Configuration.GetSection("KafkaConfig"));



builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddAPersistenceServiceService();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IKafkaProducer<string,string>, KafkaProducer>();
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
var options = app.Services.GetRequiredService<IOptions<KafkaConfig>>().Value;
// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await context.InitializeDatabaseAsync().ConfigureAwait(false);
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run();
