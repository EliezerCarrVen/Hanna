using Hanna.Utilities;

namespace Hanna.Core;

internal sealed class MultiCommandParser
{
    private static readonly Regex Separator = new(
        @"\s+(?:y luego|después|despues|también|tambien|además|ademas|y además|y ademas|luego|y)\s+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StopRegex = new(
        @"\b(?:hanna\s+para|hanna\s+detente|hanna\s+c[aá]llate|para\s+de\s+hablar|ya\s+no\s+sigas|detente|cancela\s+eso|silencio)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool IsStopCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return StopRegex.IsMatch(TextTools.Normalize(text));
    }

    public IReadOnlyList<string> Split(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        string clean = Regex.Replace(text.Trim(), @"^(?:oye\s+)?hanna[:,]?\s*", "", RegexOptions.IgnoreCase).Trim();

        if (IsStopCommand(clean))
            return new[] { clean };

        var rawParts = Separator.Split(clean)
            .Select(x => x.Trim())
            .Where(x => x.Length > 2)
            .Where(x => !Regex.IsMatch(TextTools.Normalize(x), @"^(?:y|luego|tambien|también|ademas|además|despues|después)$"))
            .ToList();

        if (rawParts.Count <= 1)
            return new[] { clean };

        var valid = rawParts
            .Where(LooksLikeCommand)
            .ToList();

        return valid.Count == 0 ? new[] { clean } : valid;
    }

    private static bool LooksLikeCommand(string text)
    {
        string n = TextTools.Normalize(text);
        return Regex.IsMatch(n, @"\b(reproduce|pon|toca|pausa|reanuda|siguiente|anterior|agrega|añade|anade|guarda|crea|recuerdame|recordatorio|abre|busca|dime|clima|noticias|baja|sube|mutea|silencia|volumen|enciende|apaga|clona|extiende|detecta|escanea)\b", RegexOptions.IgnoreCase);
    }
}
