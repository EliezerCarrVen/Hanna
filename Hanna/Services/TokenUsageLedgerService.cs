using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class TokenUsageLedgerService
{
    private readonly AppConfig config;
    private readonly TokenEstimatorService estimator;

    public TokenUsageLedgerService(AppConfig config, TokenEstimatorService estimator)
    {
        this.config = config;
        this.estimator = estimator;
    }

    private string UsageDir => config.TokenUsageDirectory;
    private string DailyPath => Path.Combine(UsageDir, $"tokens_{DateTime.UtcNow:yyyyMMdd}.jsonl");

    public async Task RegisterAsync(
        long chatId,
        string provider,
        string model,
        string personaId,
        string input,
        string output,
        CancellationToken cancellationToken,
        long? promptTokens = null,
        long? completionTokens = null,
        decimal? usd = null)
    {
        Directory.CreateDirectory(UsageDir);

        long finalPrompt = promptTokens ?? estimator.EstimateTokens(input);
        long finalCompletion = completionTokens ?? estimator.EstimateTokens(output);
        long total = finalPrompt + finalCompletion;
        decimal finalUsd = usd ?? estimator.EstimateUsd(total, model);

        var record = new
        {
            utc = DateTime.UtcNow,
            chatId,
            provider,
            model,
            personaId,
            promptTokens = finalPrompt,
            completionTokens = finalCompletion,
            totalTokens = total,
            estimatedUsd = finalUsd,
            inputPreview = Clip(input, 180),
            outputPreview = Clip(output, 180)
        };

        string line = JsonSerializer.Serialize(record);
        await File.AppendAllTextAsync(DailyPath, line + Environment.NewLine, Encoding.UTF8, cancellationToken);
    }

    public async Task<string> BuildDailyReport(CancellationToken cancellationToken)
    {
        if (!File.Exists(DailyPath))
            return "No hay registro de tokens para hoy.";

        var rows = await File.ReadAllLinesAsync(DailyPath, Encoding.UTF8, cancellationToken);

        long totalTokens = 0;
        decimal totalUsd = 0;
        var byModel = new Dictionary<string, (long tokens, decimal usd, int requests)>(StringComparer.OrdinalIgnoreCase);

        foreach (string row in rows)
        {
            if (string.IsNullOrWhiteSpace(row))
                continue;

            try
            {
                using JsonDocument doc = JsonDocument.Parse(row);
                string model = doc.RootElement.GetProperty("model").GetString() ?? "desconocido";
                long tokens = doc.RootElement.GetProperty("totalTokens").GetInt64();
                decimal usd = doc.RootElement.GetProperty("estimatedUsd").GetDecimal();

                totalTokens += tokens;
                totalUsd += usd;

                byModel.TryGetValue(model, out var current);
                byModel[model] = (current.tokens + tokens, current.usd + usd, current.requests + 1);
            }
            catch
            {
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("Registro de tokens de hoy:");
        sb.AppendLine($"Total tokens estimados: {totalTokens:N0}");
        sb.AppendLine($"Costo estimado: ${totalUsd:0.0000} USD");
        sb.AppendLine($"Límite diario OpenRouter configurado: ${config.OpenRouterDailyBudgetUsd:0.00} USD");
        sb.AppendLine();

        foreach (var item in byModel.OrderByDescending(x => x.Value.tokens))
            sb.AppendLine($"- {item.Key}: {item.Value.tokens:N0} tokens, ${item.Value.usd:0.0000}, {item.Value.requests} requests");

        return sb.ToString().Trim();
    }

    public async Task<decimal> GetTodayEstimatedUsd(CancellationToken cancellationToken)
    {
        if (!File.Exists(DailyPath))
            return 0;

        decimal total = 0;
        foreach (string line in await File.ReadAllLinesAsync(DailyPath, Encoding.UTF8, cancellationToken))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(line);
                total += doc.RootElement.GetProperty("estimatedUsd").GetDecimal();
            }
            catch
            {
            }
        }

        return total;
    }

    private static string Clip(string? value, int limit)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        value = Regex.Replace(value, @"\s+", " ").Trim();
        return value.Length <= limit ? value : value[..limit] + "...";
    }
}
