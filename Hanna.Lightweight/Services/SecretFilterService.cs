using System.Text.RegularExpressions;

namespace Hanna.Lightweight.Services;

public sealed class SecretFilterService
{
    private static readonly string[] BlockedTerms =
    [
        "TELEGRAM_TOKEN", "TELEGRAM_BOT_TOKEN", "GEMINI_API_KEY", "GROQ_API_KEY",
        "OPENROUTER_API_KEY", "SPOTIFY_CLIENT_SECRET", "MYSQL_PASSWORD", "HANNA_JWT_SECRET",
        "HANNA_MOBILE_API_PAIRING_TOKEN", "HannaEnv", "system prompt", "prompt interno",
        "prompts internos", "configuracion sensible", "configuración sensible"
    ];

    private static readonly Regex AssignmentPattern = new(
        "(?i)(token|api[_-]?key|secret|password|contraseña|passwd|pwd)\\s*[:=]\\s*[^\\s]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Filter(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var filtered = input;
        foreach (var term in BlockedTerms)
        {
            filtered = Regex.Replace(filtered, Regex.Escape(term), "[REDACTED_TERM]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return AssignmentPattern.Replace(filtered, match =>
        {
            var separatorIndex = match.Value.IndexOf('=');
            if (separatorIndex < 0)
            {
                separatorIndex = match.Value.IndexOf(':');
            }

            return separatorIndex > 0
                ? string.Concat(match.Value.AsSpan(0, separatorIndex + 1), "[REDACTED_SECRET]")
                : "[REDACTED_SECRET]";
        });
    }
}
