using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class GroqService
{
    private readonly AppConfig config;
    private readonly HttpClient httpClient;
    private readonly ContextService context;

    public GroqService(AppConfig config, HttpClient httpClient, ContextService context)
    {
        this.config = config;
        this.httpClient = httpClient;
        this.context = context;
    }

    public async Task<string> GenerateChat(string userText, HannaContext hannaContext, CancellationToken cancellationToken)
    {
        string system =
            hannaContext.ToPromptBlock() +
            "\n\nReglas: Responde como la misma Hanna. Si el usuario pide código, entrega el código completo en bloque Markdown. No inventes datos actuales. Si algo requiere datos actuales, dilo brevemente y espera verificación con Gemini.";

        var payload = new
        {
            model = config.GroqChatModel,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = userText }
            },
            temperature = 0.45
        };

        return await SendChat(payload, cancellationToken);
    }

    public async Task<string> RefineWithVerifiedContext(string userText, string groqDraft, string geminiVerified, HannaContext hannaContext, CancellationToken cancellationToken)
    {
        string system =
            hannaContext.ToPromptBlock() +
            "\n\nVas a responder como Hanna usando una verificación de Gemini. Mantén la personalidad, corrige errores del borrador si Gemini verificó algo distinto y responde corto.";

        string user =
            "Pregunta del usuario:\n" + userText +
            "\n\nBorrador de Groq:\n" + groqDraft +
            "\n\nVerificación de Gemini:\n" + geminiVerified +
            "\n\nRespuesta final de Hanna:";

        var payload = new
        {
            model = config.GroqChatModel,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            },
            temperature = 0.35
        };

        return await SendChat(payload, cancellationToken);
    }

    public async Task<string> TranscribeAudio(string filePath, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        await using var fileStream = File.OpenRead(filePath);
        using var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(GetAudioContentType(filePath));
        form.Add(fileContent, "file", Path.GetFileName(filePath));
        form.Add(new StringContent("whisper-large-v3"), "model");
        form.Add(new StringContent("es"), "language");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/audio/transcriptions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.GroqApiKey);
        request.Content = form;

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Groq Whisper Error: {json}");

        using JsonDocument doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("text", out var textEl) ? textEl.GetString() ?? "" : "";
    }

    public async Task<string> AnalyzeImage(string prompt, string base64Image, HannaContext hannaContext, CancellationToken cancellationToken)
    {
        string finalPrompt =
            hannaContext.ToPromptBlock() +
            "\n\nAnaliza la imagen manteniendo la personalidad de Hanna.\n" +
            prompt;

        var payload = new
        {
            model = config.GroqVisionModel,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = finalPrompt },
                        new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                    }
                }
            },
            temperature = 0.2,
            max_completion_tokens = 800
        };

        return await SendChat(payload, cancellationToken);
    }

    private async Task<string> SendChat(object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.GroqApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Groq Error: {json}");

        using JsonDocument doc = JsonDocument.Parse(json);

        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private static string GetAudioContentType(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".ogg" => "audio/ogg",
            ".opus" => "audio/ogg",
            ".webm" => "audio/webm",
            _ => "application/octet-stream"
        };
    }
}
