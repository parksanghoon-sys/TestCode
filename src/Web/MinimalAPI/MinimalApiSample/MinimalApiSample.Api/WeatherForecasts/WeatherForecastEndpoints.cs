
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace MinimalApiSample.Api.WeatherForecasts;

public static class WeatherForecastEndpoints
{
    public static void AddWeatherForecastEndpoints(this WebApplication app)
    {
        app.MapGet("/forecast", GetForecast)
          .WithName("GetWeatherForecast")
          .WithOpenApi(x => new OpenApiOperation(x)
          {
              Summary = "Fetch the weather forecast for a given location"
          });
    }

    public static async Task<Results<Ok<WeatherForecastResult>, ProblemHttpResult>> GetForecast([FromQuery(Name = "latitude")] double latitude,
        [FromQuery(Name = "longitude")] double longitude,
        [FromServices] WeatherForecastService weatherForecastService)
    {
        var forecaset= await weatherForecastService.GetForecast(latitude, longitude);

        if (latitude < -90 || latitude > 90)
        {
            return TypedResults.Problem(
                "The value for latitude is out of range. Must be between -90 and 90",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        var result = new WeatherForecastResult()
        {
            Time = DateTime.UtcNow,
            TemperatureC = forecaset.Current.Templerature,
        };
        return TypedResults.Ok(result);
    }
}
