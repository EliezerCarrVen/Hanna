using Hanna.Core;

namespace Hanna.Services;

internal sealed class ConfigUpdateService
{
    private readonly AppConfig config;

    public ConfigUpdateService(AppConfig config)
    {
        this.config = config;
    }

    public async Task<string> AppendPersonality(string addition, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(addition))
            return "Dime qué quieres agregar a la personalidad de Hanna.";

        string text = Environment.NewLine + Environment.NewLine + "[ACTUALIZACIÓN DINÁMICA]" + Environment.NewLine + addition.Trim();

        await File.AppendAllTextAsync(config.PersonalityPath, text, Encoding.UTF8, cancellationToken);

        return "Actualicé la personalidad de Hanna.";
    }
}
