using System.Globalization;
using System.Text.Json;
using TownSquare.Models;

namespace TownSquare.Services;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private const double BorasLatitude = 57.7210;
    private const double BorasLongitude = 12.9401;

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WeatherForecast?> GetWeatherForecastAsync(DateTime date)
    {
        try
        {
            // Use InvariantCulture to ensure consistent date formatting
            var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // Use InvariantCulture for latitude/longitude to ensure period as decimal separator
            var latitude = BorasLatitude.ToString(CultureInfo.InvariantCulture);
            var longitude = BorasLongitude.ToString(CultureInfo.InvariantCulture);

            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&daily=temperature_2m_max,temperature_2m_min,weathercode,windspeed_10m_max,precipitation_probability_max&timezone=Europe/Stockholm&start_date={dateString}&end_date={dateString}";

            _logger.LogInformation("Fetching weather forecast for {Date} from URL: {Url}", dateString, url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Weather API returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            var daily = jsonDoc.RootElement.GetProperty("daily");

            var tempMax = daily.GetProperty("temperature_2m_max")[0].GetDouble();
            var tempMin = daily.GetProperty("temperature_2m_min")[0].GetDouble();
            var weatherCode = daily.GetProperty("weathercode")[0].GetInt32();
            var windSpeed = daily.GetProperty("windspeed_10m_max")[0].GetDouble();
            var precipitationProbability = daily.GetProperty("precipitation_probability_max")[0].GetInt32();

            return new WeatherForecast
            {
                Date = date,
                Temperature = (tempMax + tempMin) / 2,
                Description = GetWeatherDescription(weatherCode),
                Icon = GetWeatherIcon(weatherCode),
                Humidity = precipitationProbability,
                WindSpeed = windSpeed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather forecast for date {Date}", date);
            return null;
        }
    }

    private string GetWeatherDescription(int weatherCode)
    {
        return weatherCode switch
        {
            0 => "Clear sky",
            1 or 2 or 3 => "Partly cloudy",
            45 or 48 => "Foggy",
            51 or 53 or 55 => "Drizzle",
            61 or 63 or 65 => "Rain",
            71 or 73 or 75 => "Snow",
            77 => "Snow grains",
            80 or 81 or 82 => "Rain showers",
            85 or 86 => "Snow showers",
            95 => "Thunderstorm",
            96 or 99 => "Thunderstorm with hail",
            _ => "Unknown"
        };
    }

    private string GetWeatherIcon(int weatherCode)
    {
        return weatherCode switch
        {
            0 => "☀️",
            1 or 2 or 3 => "⛅",
            45 or 48 => "🌫️",
            51 or 53 or 55 => "🌦️",
            61 or 63 or 65 => "🌧️",
            71 or 73 or 75 => "❄️",
            77 => "🌨️",
            80 or 81 or 82 => "🌧️",
            85 or 86 => "🌨️",
            95 => "⛈️",
            96 or 99 => "⛈️",
            _ => "🌡️"
        };
    }
}
