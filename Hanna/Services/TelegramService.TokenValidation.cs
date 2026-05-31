namespace Hanna.Services;

internal sealed partial class TelegramService
{
    private static bool IsValidTelegramToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        token = token.Trim();

        if (token.Contains("PEGA_AQUI", StringComparison.OrdinalIgnoreCase) ||
            token.Contains("TU_TOKEN", StringComparison.OrdinalIgnoreCase) ||
            token.Contains("TOKEN_REAL", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = token.Split(':', 2);

        if (parts.Length != 2)
        {
            return false;
        }

        if (!long.TryParse(parts[0], out _))
        {
            return false;
        }

        if (parts[1].Length < 20)
        {
            return false;
        }

        foreach (var c in parts[1])
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
            {
                return false;
            }
        }

        return true;
    }
}
