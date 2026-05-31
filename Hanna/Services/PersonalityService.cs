using Hanna.Core;

namespace Hanna.Services;

internal sealed class PersonalityService
{
    private readonly AppConfig config;

    public PersonalityService(AppConfig config)
    {
        this.config = config;
    }

    public async Task<string> Load(CancellationToken cancellationToken = default)
    {
        string fallback = "Eres Hanna, una asistente virtual inteligente, directa, útil y conversacional. Habla en español neutro, sin modismos mexicanos salvo que el usuario los pida explícitamente. Mantén una personalidad consistente aunque cambie el modelo usado.";

        if (!File.Exists(config.PersonalityPath))
            return fallback;

        string content = await File.ReadAllTextAsync(config.PersonalityPath, Encoding.UTF8, cancellationToken);

        string styleRule = "\n\nREGLA DE ESTILO ACTIVA: responde en español neutro. Evita modismos mexicanos como qué onda, jale, chido, órale, no manches, wey, compa, carnal, sale, va, cámara, simón y qué pedo, salvo solicitud explícita del usuario.";

        return string.IsNullOrWhiteSpace(content) ? fallback : content.Trim() + styleRule;
    }
}
