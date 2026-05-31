using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class ModelModeService
{
    private readonly AppConfig config;
    private readonly FileStorageService storage;
    private static readonly AsyncLocal<EngineMode?> TemporaryMode = new();

    public ModelModeService(AppConfig config, FileStorageService storage)
    {
        this.config = config;
        this.storage = storage;
    }

    private string ModePath => Path.Combine(config.SettingsDirectory, "engine_mode.txt");

    public EngineMode GetMode()
    {
        if (TemporaryMode.Value.HasValue)
            return TemporaryMode.Value.Value;

        try
        {
            if (!File.Exists(ModePath))
                return ParseModeValue(config.DefaultEngineMode);

            string value = File.ReadAllText(ModePath, Encoding.UTF8).Trim().ToLowerInvariant();
            return ParseModeValue(value);
        }
        catch
        {
            return ParseModeValue(config.DefaultEngineMode);
        }
    }

    public IDisposable PushTemporaryMode(EngineMode mode)
    {
        EngineMode? previous = TemporaryMode.Value;
        TemporaryMode.Value = mode;
        return new TemporaryModeScope(previous);
    }

    public async Task SetMode(EngineMode mode, CancellationToken cancellationToken)
    {
        string value = mode switch
        {
            EngineMode.GroqOnly => "groq",
            EngineMode.GeminiOnly => "gemini",
            EngineMode.Hybrid => "hybrid",
            EngineMode.OllamaLocal => "ollama",
            EngineMode.OpenRouter => "openrouter",
            _ => "original"
        };

        await File.WriteAllTextAsync(ModePath, value, Encoding.UTF8, cancellationToken);
    }

    public static bool IsEngineModeCommandText(string text)
    {
        string normalized = Utilities.TextTools.Normalize(text);
        return Regex.IsMatch(normalized, @"\b(usa|usar|cambia|cambiar|modo|motor)\b") &&
               Regex.IsMatch(normalized, @"\b(ollama|local|qwen|groq|gemini|hibrido|híbrido|hybrid|original|openrouter|open router|open routes|openroutes)\b");
    }

    public static EngineMode ParseFromText(string text)
    {
        string normalized = Utilities.TextTools.Normalize(text);

        if (Regex.IsMatch(normalized, @"\b(openrouter|open router|open routes|openroutes|router ia|ruta abierta)\b"))
            return EngineMode.OpenRouter;

        if (Regex.IsMatch(normalized, @"\b(ollama|local|qwen|modo local|llm local|modelo local)\b"))
            return EngineMode.OllamaLocal;

        if (Regex.IsMatch(normalized, @"\b(original|default|normal|como estaba|configuracion actual|configuración actual)\b"))
            return EngineMode.Original;

        if (Regex.IsMatch(normalized, @"\b(groq|solo groq|groq only)\b"))
            return EngineMode.GroqOnly;

        if (Regex.IsMatch(normalized, @"\b(gemini|solo gemini|gemini only)\b"))
            return EngineMode.GeminiOnly;

        if (Regex.IsMatch(normalized, @"\b(hibrido|híbrido|hybrid|mixto)\b"))
            return EngineMode.Hybrid;

        return EngineMode.Original;
    }

    public string GetModeLabel()
    {
        return GetMode() switch
        {
            EngineMode.GroqOnly => "Groq",
            EngineMode.GeminiOnly => "Gemini",
            EngineMode.Hybrid => "híbrido",
            EngineMode.OllamaLocal => "Ollama local",
            EngineMode.OpenRouter => "OpenRouter",
            _ => "original"
        };
    }

    private static EngineMode ParseModeValue(string value)
    {
        value = (value ?? "").Trim().ToLowerInvariant();

        return value switch
        {
            "original" or "default" or "normal" => EngineMode.Original,
            "groq" or "groq_only" or "groqonly" => EngineMode.GroqOnly,
            "gemini" or "gemini_only" or "geminionly" => EngineMode.GeminiOnly,
            "hybrid" or "hibrido" or "híbrido" => EngineMode.Hybrid,
            "ollama" or "local" or "ollama_local" or "ollamalocal" or "qwen" => EngineMode.OllamaLocal,
            "openrouter" or "open_router" or "open-router" or "openroutes" or "open_routes" or "open routes" => EngineMode.OpenRouter,
            _ => EngineMode.Hybrid
        };
    }

    private sealed class TemporaryModeScope : IDisposable
    {
        private readonly EngineMode? previous;
        private bool disposed;

        public TemporaryModeScope(EngineMode? previous)
        {
            this.previous = previous;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            TemporaryMode.Value = previous;
        }
    }
}
