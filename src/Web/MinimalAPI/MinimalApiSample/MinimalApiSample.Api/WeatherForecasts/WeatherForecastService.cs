using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinimalApiSample.Api.WeatherForecasts
{
    public class WeatherForecastService(IHttpClientFactory httpClientFactory)
    {
        public class OpenMeteoForecastResponse
        {
            [JsonPropertyName("longitude")]
            public double Longitude { get; set; }
            [JsonPropertyName("latitude")]
            public double Latitude { get; set; }
            [JsonPropertyName("current")]
            public OpenMeteoForecastCurrent Current { get; set; }
        }
        public class OpenMeteoForecastCurrent
        {
            [JsonPropertyName("temperature_2m")]
            public double Templerature { get; set; }
        }
        public async Task<OpenMeteoForecastResponse> GetForecast(double latitude, double longitude)
        {
            var httpRequestMessage = new HttpRequestMessage(
                    HttpMethod.Get,
                    BuildUrl(latitude, longitude)
                );
            var httpClient = httpClientFactory.CreateClient("WeatherForecast");
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            if (httpResponseMessage.IsSuccessStatusCode == false)
            {
                throw new Exception("Failed to get forecaset");
            }

            await using var response = await httpResponseMessage.Content.ReadAsStreamAsync();
            var forecastResponse = await JsonSerializer.DeserializeAsync<OpenMeteoForecastResponse>(response);

            if (forecastResponse is null)
                throw new Exception("Error deserializing forecast response");

            return forecastResponse;
        }
        public static Uri BuildUrl(double latitude, double longitude)
        {
            return new Uri($"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m");
        }
    }
}
