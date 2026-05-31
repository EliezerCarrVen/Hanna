using Hanna.Core;

namespace Hanna.Services;

internal sealed class WeatherService
{
    private readonly AppConfig config;
    private readonly HttpClient httpClient;

    public WeatherService(AppConfig config, HttpClient httpClient)
    {
        this.config = config;
        this.httpClient = httpClient;
    }

    public async Task<string> GetWeather(string place, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.OpenWeatherApiKey))
            return "No tengo conectada una API de clima. Agrega OPENWEATHER_API_KEY en HannaEnv.env para consultar clima real.";

        string location = string.IsNullOrWhiteSpace(place) ? "Gómez Palacio, Durango, MX" : place;

        try
        {
            string url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(location)}&appid={Uri.EscapeDataString(config.OpenWeatherApiKey)}&units=metric&lang=es";

            using var response = await httpClient.GetAsync(url, cancellationToken);
            string json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return $"No pude consultar el clima de {location}.";

            using JsonDocument doc = JsonDocument.Parse(json);

            double temp = doc.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
            int humidity = doc.RootElement.GetProperty("main").GetProperty("humidity").GetInt32();
            string description = doc.RootElement.GetProperty("weather")[0].GetProperty("description").GetString() ?? "sin descripción";
            double wind = doc.RootElement.GetProperty("wind").TryGetProperty("speed", out var w) ? w.GetDouble() : 0;
            string city = doc.RootElement.GetProperty("name").GetString() ?? location;

            return $"En {city} hay {Math.Round(temp)} grados, {description}, humedad de {humidity} por ciento y viento de {Math.Round(wind * 3.6)} kilómetros por hora.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Weather Error]: {ex.Message}");
            return "No pude consultar el clima en este momento.";
        }
    }
}
