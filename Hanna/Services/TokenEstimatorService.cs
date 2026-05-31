using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class TokenEstimatorService
{
    private readonly AppConfig config;

    public TokenEstimatorService(AppConfig config)
    {
        this.config = config;
    }

    public long EstimateTokens(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Estimación conservadora para español/código: 1 token ≈ 4 caracteres.
        return Math.Max(1, (long)Math.Ceiling(text.Length / 4.0));
    }

    public decimal EstimateUsd(long tokens, string? model = null)
    {
        decimal per1k = ResolveModelPricePer1K(model);
        return Math.Round((tokens / 1000m) * per1k, 6, MidpointRounding.AwayFromZero);
    }

    public TokenEstimateResult EstimateText(string text, string source, string? model = null)
    {
        string finalModel = string.IsNullOrWhiteSpace(model) ? config.OpenRouterDefaultModel : model!;
        long tokens = EstimateTokens(text);
        decimal usd = EstimateUsd(tokens, finalModel);
        bool high = tokens > config.TokenUploadWarningThreshold;

        return new TokenEstimateResult
        {
            Source = source,
            Characters = text?.Length ?? 0,
            EstimatedTokens = tokens,
            EstimatedUsd = usd,
            Model = finalModel,
            ExceedsRecommendedLimit = high,
            Recommendation = high
                ? "Conviene resumir, dividir el archivo o usar RAG local antes de enviarlo completo a una API."
                : "El consumo parece razonable. Aun así, si el archivo es repetitivo conviene indexarlo localmente."
        };
    }

    public async Task<TokenEstimateResult> EstimateFile(string path, string? model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new TokenEstimateResult
            {
                Source = path ?? "archivo no especificado",
                Recommendation = "No encontré el archivo. Usa una ruta completa entre comillas."
            };
        }

        FileInfo info = new(path);
        string ext = info.Extension.ToLowerInvariant();

        if (info.Length > config.MaxFileTokenPreviewBytes)
        {
            long roughTokens = Math.Max(1, (long)Math.Ceiling(info.Length / 4.0));
            return new TokenEstimateResult
            {
                Source = path,
                Characters = info.Length,
                EstimatedTokens = roughTokens,
                EstimatedUsd = EstimateUsd(roughTokens, model),
                Model = model ?? config.OpenRouterDefaultModel,
                ExceedsRecommendedLimit = true,
                Recommendation = $"El archivo pesa {info.Length:N0} bytes. No lo cargué completo para ahorrar RAM; usa RAG, divide el archivo o resume secciones."
            };
        }

        string content;
        try
        {
            content = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
        }
        catch
        {
            return new TokenEstimateResult
            {
                Source = path,
                Characters = info.Length,
                EstimatedTokens = Math.Max(1, (long)Math.Ceiling(info.Length / 4.0)),
                EstimatedUsd = EstimateUsd(Math.Max(1, (long)Math.Ceiling(info.Length / 4.0)), model),
                Model = model ?? config.OpenRouterDefaultModel,
                ExceedsRecommendedLimit = true,
                Recommendation = $"No pude leerlo como texto ({ext}). Si es binario/PDF/imagen, extrae texto antes de enviarlo al modelo."
            };
        }

        return EstimateText(content, path, model);
    }

    private decimal ResolveModelPricePer1K(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return config.OpenRouterEstimatedUsdPer1KTokens;

        string m = model.ToLowerInvariant();

        if (m.Contains("free"))
            return 0m;

        if (m.Contains("opus"))
            return config.OpenRouterArchitectUsdPer1KTokens;

        if (m.Contains("sonnet"))
            return config.OpenRouterEngineerUsdPer1KTokens;

        if (m.Contains("gemini") || m.Contains("flash"))
            return config.OpenRouterOperatorUsdPer1KTokens;

        if (m.Contains("llama") || m.Contains("8b"))
            return config.OpenRouterAnalystUsdPer1KTokens;

        return config.OpenRouterEstimatedUsdPer1KTokens;
    }
}
