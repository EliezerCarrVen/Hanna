using System.Text.RegularExpressions;
using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class SecretFilterService(RuntimePaths paths, PathGuardService pathGuard, LogRotationService logRotation)
{
    private static readonly Regex[] SecretPatterns =
    [
        new("(?i)\\b(TELEGRAM_TOKEN|TELEGRAM_BOT_TOKEN|GROQ_API_KEY|GEMINI_API_KEY|OPENROUTER_API_KEY|SPOTIFY_CLIENT_SECRET|MYSQL_PASSWORD|HANNA_JWT_SECRET|HANNA_MOBILE_API_PAIRING_TOKEN|HannaEnv)\\b", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new("(?i)\\b(api[_-]?key|apikey|token|password|contraseña|pwd|secret|client_secret|refresh_token)\\s*[:=]\\s*[^\\s,;]+", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new("(?i)\\bbearer\\s+[-A-Za-z0-9._~+/]+=*", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new("(?i)\\b(mysql|postgres|postgresql)://[^\\s/@:]+:[^\\s/@]+@[^\\s]+", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new("(?i)\\b(server|host)=[^;]+;[^\\n]*?(password|pwd)=[^;\\s]+", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new("(?i)\\bhttps?://[^\\s/@:]+:[^\\s/@]+@[^\\s]+", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new("\\beyJ[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+\\b", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new("\\bsk-or-v1-[A-Za-z0-9_-]{12,}\\b", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new("\\bgsk_[A-Za-z0-9_-]{12,}\\b", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new("\\bAIza[A-Za-z0-9_-]{20,}\\b", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new("\\b(?=[A-Za-z0-9_-]{48,}\\b)(?=[A-Za-z0-9_-]*[_-])[A-Za-z0-9_-]+\\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)
    ];

    public string Filter(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var filtered = input;
        var redactionCount = 0;
        foreach (var pattern in SecretPatterns)
        {
            filtered = pattern.Replace(filtered, match =>
            {
                redactionCount++;
                return "[REDACTED]";
            });
        }

        if (redactionCount > 0)
        {
            LogRedaction(redactionCount);
        }

        return filtered;
    }

    private void LogRedaction(int redactionCount)
    {
        try
        {
            Directory.CreateDirectory(paths.Logs);
            pathGuard.EnsureInsideRoot(paths.SecurityLog);
            logRotation.RotateIfNeeded(paths.SecurityLog);
            File.AppendAllText(paths.SecurityLog, $"{DateTimeOffset.UtcNow:O} SecretFilter redacted {redactionCount} sensitive value(s). Original content was not logged.{Environment.NewLine}");
        }
        catch
        {
            // Security logging must never leak or break memory writes.
        }
    }
}
