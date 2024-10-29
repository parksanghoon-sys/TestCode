using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiSample.Api.WeatherForecasts;
using Moq;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalApiSample.Tests
{
    public class WeatherForecastTests
    {
        private string sampleResponse =
       """
            {
              "generationtime_ms": 0.01704692840576172,
              "utc_offset_seconds": 0,
              "timezone": "GMT",
              "timezone_abbreviation": "GMT",
              "elevation": 38,
              "current_units": {
                "time": "iso8601",
                "interval": "seconds",
                "temperature_2m": "°C"
              },
              "current": {
                "time": "2024-07-03T09:30",
                "interval": 900,
                "temperature_2m": 16.3
              }
            }
        """;

        [Test]
        public async Task It_return_the_expected_temperature()
        {

            var latitude = 22.4;
            var longitude = 23.5;

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(WeatherForecastService.BuildUrl(latitude,longitude).ToString())
                .Respond("application/json",sampleResponse);

            var clientFactory = new Mock<IHttpClientFactory>();

            clientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(mockHttp.ToHttpClient());

            var forecast = await WeatherForecastEndpoints.GetForecast(latitude, longitude, new WeatherForecastService(clientFactory.Object));

            Assert.That(forecast.Result, Is.TypeOf<Ok<WeatherForecastResult>>());

            var okResult = (Ok<WeatherForecastResult>)forecast.Result;

            Assert.Multiple(() =>
            {
                Assert.That(okResult.Value, Is.Not.Null);
                Assert.That(okResult.Value?.TemperatureC, Is.EqualTo(16.3));
            });
        }
    }

}
