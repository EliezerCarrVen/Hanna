namespace Hanna.Services;

internal static class SecretSanitizer
{
    private static readonly (string Pattern, string Replacement)[] Patterns =
    {
        (@"\b\d{6,}:[A-Za-z0-9_\-]{20,}\b", "[TELEGRAM_TOKEN_OCULTO]"),
        (@"\bsk-or-v1-[A-Za-z0-9_\-]{12,}\b", "[OPENROUTER_KEY_OCULTA]"),
        (@"\bgsk_[A-Za-z0-9_\-]{12,}\b", "[GROQ_KEY_OCULTA]"),
        (@"\bAIza[A-Za-z0-9_\-]{20,}\b", "[GOOGLE_API_KEY_OCULTA]"),
        (@"\bBearer\s+[A-Za-z0-9_\-\.]+", "Bearer [TOKEN_OCULTO]"),
        (@"\beyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\b", "[JWT_OCULTO]"),
        (@"(?i)(password|pwd|secret|api[_-]?key|token|pairing[_-]?token|refresh[_-]?token)\s*=\s*[^;\s\r\n]+", "$1=[OCULTO]"),
        (@"(?i)(User\s+ID|Uid|User)\s*=\s*[^;\r\n]+", "$1=[OCULTO]"),
        (@"(?i)(Server|Host|Database)\s*=\s*[^;\r\n]+", "$1=[OCULTO]"),
        (@"(?i)mongodb(\+srv)?://[^\s\r\n]+", "mongodb://[CONNECTION_STRING_OCULTA]"),
        (@"(?i)mysql://[^\s\r\n]+", "mysql://[CONNECTION_STRING_OCULTA]"),
        (@"(?i)C:\\Users\\[^\\\s\r\n]+", "C:\\Users\\[USUARIO]"),
        (@"(?i)/home/[^/\s\r\n]+", "/home/[USUARIO]"),
        (@"(?i)/Users/[^/\s\r\n]+", "/Users/[USUARIO]")
    };

    public static string Sanitize(string? value, int maxLength = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string sanitized = value;
        foreach (var (pattern, replacement) in Patterns)
            sanitized = Regex.Replace(sanitized, pattern, replacement, RegexOptions.CultureInvariant);

        sanitized = Regex.Replace(sanitized, @"(?i)(HannaEnv\.env|\.env)\s*[:=]\s*[^\r\n]+", "$1=[CONTENIDO_OCULTO]");

        if (maxLength > 0 && sanitized.Length > maxLength)
            sanitized = sanitized[..maxLength].Trim() + "...";

        return sanitized;
    }

    public static bool LooksSensitive(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Sanitize(value) != value;
    }
}
