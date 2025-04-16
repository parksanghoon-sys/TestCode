using LibCQRS;
using LibCQRS.Query;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using WebApplication1.CQRS.Commands;
using WebApplication1.CQRS.Quries;
using WebApplication1.Dtos;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IMediator mediator;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IMediator mediator)
        {
            _logger = logger;
            this.mediator = mediator;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var createCommand = new CreateUserCommand("john.doe", "john.doe@example.com");
            var newUser = await mediator.SendCommandAsync<CreateUserCommand, UserDto>(createCommand);

            var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
            // Execute a query
            var getUserQuery = new GetUserByIdQuery(newUser.Id);
            var user = await mediator.SendQueryAsync<GetUserByIdQuery, UserDto?>(getUserQuery);
            return result;
        }
    }
}
