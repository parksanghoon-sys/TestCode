using MinimalApiSample.Api.WeatherForecasts;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.Services.AddWeatherForecastService();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(e =>
{
    e.Run(async context =>
    {
        await Results.Problem().ExecuteAsync(context);
    });
});
app.AddWeatherForecastEndpoints();

app.Run();
