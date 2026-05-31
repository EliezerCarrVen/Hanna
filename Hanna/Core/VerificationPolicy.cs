namespace Hanna.Core;

internal static class VerificationPolicy
{
    public static bool RequiresWebVerification(string userText)
    {
        string text = Utilities.TextTools.Normalize(userText);

        return Regex.IsMatch(text, @"\b(actual|actualmente|hoy|ahorita|noticias|precio|precios|version|versión|fecha|reciente|recientes|último|ultimo|última|ultima|clima|pronóstico|pronostico|presidente|ceo|director|titular|ganador|resultado|marcador)\b");
    }
}
