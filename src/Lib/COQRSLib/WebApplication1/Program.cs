using LibCQRS;
using LibCQRS.Commands;
using LibCQRS.Query;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Win32;
using WebApplication1;
using WebApplication1.CQRS.Commands;
using WebApplication1.CQRS.Quries;
using WebApplication1.Dtos;
using WebApplication1.Models;
using WebApplication1.Repository;
using WebApplication1.Store;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register mediator
builder.Services.AddSingleton<IMediator, Mediator>();

builder.Services.AddTransient<IStore<User>, InMemoryState>();
builder.Services.AddTransient<IUserRepository, UserRepository>();

builder.Services.AddTransient<IQueryHandler<GetUserByIdQuery, UserDto?>, GetUserByIdQueryHandler>();
builder.Services.AddTransient<ICommandHandler<CreateUserCommand, UserDto>, CreateUserCommandHandler>();

// Register pipeline behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior1<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior2<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior3<,>));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
