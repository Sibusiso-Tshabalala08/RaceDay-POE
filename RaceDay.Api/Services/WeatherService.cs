using System.Text.Json;

namespace RaceDay.Api.Services;

public interface IWeatherService
{
    Task<JsonElement?> GetForecastAsync(decimal latitude, decimal longitude);
}

/// <summary>
/// Calls Open-Meteo (https://open-meteo.com) — a free, keyless weather API —
/// using the event's Latitude/Longitude from the Part 1 ERD. Supports the
/// background brief's "prepare for race day using live weather information."
/// </summary>
public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JsonElement?> GetForecastAsync(decimal latitude, decimal longitude)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,precipitation,wind_speed_10m";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
