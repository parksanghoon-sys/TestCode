namespace MinimalApiSample.Api.WeatherForecasts
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWeatherForecastService(this IServiceCollection services)
        {
            services.AddSingleton<WeatherForecastService>();

            return services;
        }
    }
}
