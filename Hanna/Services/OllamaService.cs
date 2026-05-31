using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class OllamaService
{
    private readonly AppConfig config;
    private readonly HttpClient httpClient;
    private readonly RuntimeSettingsService? runtime;

    public OllamaService(AppConfig config, HttpClient httpClient, RuntimeSettingsService? runtime = null)
    {
        this.config = config;
        this.httpClient = httpClient;
        this.runtime = runtime;
    }

    public async Task<string> GenerateChat(string userText, HannaContext hannaContext, CancellationToken cancellationToken)
    {
        string system =
            hannaContext.ToPromptBlock() +
            "\n\nEres Hanna usando un LLM local por Ollama. Responde SIEMPRE en español. " +
            "No uses inglés en explicaciones, títulos ni resúmenes. " +
            "Mantén respuestas útiles, directas y breves. " +
            "Si el usuario pide código, genera código completo y explicación breve en español. " +
            "Si el usuario pide acciones del sistema, responde de forma breve para que las skills puedan ejecutar lo necesario.";

        var payload = new
        {
            model = runtime?.Snapshot().OllamaModel ?? config.OllamaModel,
            stream = false,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = userText }
            },
            options = new
            {
                temperature = 0.25,
                num_ctx = runtime?.Snapshot().OllamaContextSize ?? config.OllamaContextSize
            }
        };

        string url = (runtime?.Snapshot().OllamaBaseUrl ?? config.OllamaBaseUrl).TrimEnd('/') + "/api/chat";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama Error: {json}");

        using JsonDocument doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }
}
