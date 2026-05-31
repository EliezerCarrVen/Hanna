using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class OpenRouterService
{
    private readonly AppConfig config;
    private readonly HttpClient httpClient;
    private readonly QueryAnalyzerService queryAnalyzer;
    private readonly HannaPersonaService personaService;
    private readonly TokenEstimatorService estimator;
    private readonly TokenUsageLedgerService ledger;

    public OpenRouterService(
        AppConfig config,
        HttpClient httpClient,
        QueryAnalyzerService queryAnalyzer,
        HannaPersonaService personaService,
        TokenEstimatorService estimator,
        TokenUsageLedgerService ledger)
    {
        this.config = config;
        this.httpClient = httpClient;
        this.queryAnalyzer = queryAnalyzer;
        this.personaService = personaService;
        this.estimator = estimator;
        this.ledger = ledger;
    }

    public async Task<string> GenerateChat(long chatId, string userText, HannaContext hannaContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.OpenRouterApiKey) ||
            config.OpenRouterApiKey.Contains("PON_AQUI", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("OpenRouter no está configurado. Agrega OPENROUTER_API_KEY en HannaEnv.env.");
        }

        HannaPersona persona = personaService.GetActivePersona();
        QueryAnalysisResult analysis = queryAnalyzer.Analyze(userText);
        string selectedModel = SelectModel(persona, analysis);
        string system = personaService.BuildSystemPrompt(hannaContext, analysis);

        TokenEstimateResult estimate = estimator.EstimateText(system + "\n" + userText, "openrouter_prompt", selectedModel);
        decimal todayUsd = await ledger.GetTodayEstimatedUsd(cancellationToken);

        if (config.OpenRouterDailyBudgetUsd > 0 && todayUsd + estimate.EstimatedUsd > config.OpenRouterDailyBudgetUsd)
        {
            throw new InvalidOperationException(
                $"OpenRouter bloqueado por presupuesto. Hoy: ${todayUsd:0.0000}, petición estimada: ${estimate.EstimatedUsd:0.0000}, límite: ${config.OpenRouterDailyBudgetUsd:0.00}. Usa Ollama/local o cambia el límite.");
        }

        int maxOutput = Math.Max(128, persona.MaxOutputTokens);
        decimal temperature = persona.TemperatureProfile.ToLowerInvariant() switch
        {
            "strict" => 0.15m,
            "low" => 0.20m,
            "clear" => 0.30m,
            _ => 0.35m
        };

        var payload = new
        {
            model = selectedModel,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = userText }
            },
            temperature,
            max_tokens = maxOutput
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, config.OpenRouterBaseUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.OpenRouterApiKey);
        request.Headers.TryAddWithoutValidation("HTTP-Referer", config.OpenRouterReferer);
        request.Headers.TryAddWithoutValidation("X-Title", config.OpenRouterAppTitle);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenRouter Error: {json}");

        using JsonDocument doc = JsonDocument.Parse(json);
        string answer = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        long? promptTokens = TryGetUsage(doc.RootElement, "prompt_tokens");
        long? completionTokens = TryGetUsage(doc.RootElement, "completion_tokens");
        decimal? cost = null;

        if (promptTokens.HasValue && completionTokens.HasValue)
            cost = estimator.EstimateUsd(promptTokens.Value + completionTokens.Value, selectedModel);

        await ledger.RegisterAsync(
            chatId,
            "OpenRouter",
            selectedModel,
            persona.Id,
            system + "\n" + userText,
            answer,
            cancellationToken,
            promptTokens,
            completionTokens,
            cost);

        return answer;
    }

    private string SelectModel(HannaPersona persona, QueryAnalysisResult analysis)
    {
        if (!string.IsNullOrWhiteSpace(persona.ModelName))
            return persona.ModelName;

        return analysis.Route switch
        {
            QueryRoute.Technical => config.OpenRouterEngineerModel,
            QueryRoute.Creative => config.OpenRouterCreativeModel,
            QueryRoute.Administrative => config.OpenRouterAnalystModel,
            QueryRoute.Local => config.OpenRouterAnalystModel,
            _ => config.OpenRouterDefaultModel
        };
    }

    private static long? TryGetUsage(JsonElement root, string name)
    {
        try
        {
            if (root.TryGetProperty("usage", out JsonElement usage) &&
                usage.TryGetProperty(name, out JsonElement value) &&
                value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt64(out long parsed))
                return parsed;
        }
        catch
        {
        }

        return null;
    }
}
