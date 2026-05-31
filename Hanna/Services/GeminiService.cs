using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class GeminiService
{
    private readonly AppConfig config;
    private readonly HttpClient httpClient;
    private readonly ContextService context;

    public GeminiService(AppConfig config, HttpClient httpClient, ContextService context)
    {
        this.config = config;
        this.httpClient = httpClient;
        this.context = context;
    }

    public async Task<string> VerifyWithWeb(string userText, string groqDraft, HannaContext hannaContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.GeminiApiKey))
            return "";

        string prompt =
            hannaContext.ToPromptBlock() +
            "\n\nEres la misma Hanna, pero ahora actúas como verificador web con Gemini. Conserva personalidad, historial y contexto. Verifica la respuesta de Groq con búsqueda web si aplica. No pierdas continuidad de conversación.\n\n" +
            "Pregunta del usuario:\n" + userText + "\n\n" +
            "Respuesta preliminar de Groq:\n" + groqDraft + "\n\n" +
            "Devuelve una respuesta final breve, corregida y verificable. Si no puedes confirmar algo, dilo claramente.";

        var payload = new
        {
            contents = new object[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new { text = prompt }
                    }
                }
            },
            tools = new object[]
            {
                new { google_search = new { } }
            },
            generationConfig = new
            {
                temperature = 0.25
            }
        };

        string result = await SendGenerateContent(payload, cancellationToken);

        if (!string.IsNullOrWhiteSpace(result))
            return result;

        var fallbackPayload = new
        {
            contents = new object[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new { text = prompt }
                    }
                }
            },
            tools = new object[]
            {
                new { google_search_retrieval = new { } }
            },
            generationConfig = new
            {
                temperature = 0.25
            }
        };

        return await SendGenerateContent(fallbackPayload, cancellationToken);
    }

    public async Task<string> GenerateWithContext(string userText, HannaContext hannaContext, CancellationToken cancellationToken)
    {
        string prompt =
            hannaContext.ToPromptBlock() +
            "\n\nResponde como la misma Hanna. Mantén continuidad de personalidad e historial aunque este modelo sea Gemini.\n\nUsuario:\n" +
            userText;

        var payload = new
        {
            contents = new object[]
            {
                new
                {
                    role = "user",
                    parts = new object[] { new { text = prompt } }
                }
            },
            generationConfig = new { temperature = 0.45 }
        };

        return await SendGenerateContent(payload, cancellationToken);
    }

    private async Task<string> SendGenerateContent(object payload, CancellationToken cancellationToken)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(config.GeminiModel)}:generateContent?key={Uri.EscapeDataString(config.GeminiApiKey)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Gemini Error]: {json}");
            return "";
        }

        using JsonDocument doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return "";

        var parts = candidates[0].GetProperty("content").GetProperty("parts");

        var sb = new StringBuilder();

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text))
                sb.Append(text.GetString());
        }

        return sb.ToString().Trim();
    }
}
