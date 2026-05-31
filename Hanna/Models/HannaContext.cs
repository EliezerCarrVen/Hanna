namespace Hanna.Models;

internal sealed record HannaContext(
    string Personality,
    string RecentConversation,
    string ModelMemory,
    string UserPreferences,
    string ResponseMode
)
{
    public string ToPromptBlock()
    {
        var sb = new StringBuilder();
        sb.AppendLine("PERSONALIDAD DE HANNA:");
        sb.AppendLine(Personality);
        sb.AppendLine();
        sb.AppendLine("CONTEXTO RECIENTE:");
        sb.AppendLine(string.IsNullOrWhiteSpace(RecentConversation) ? "Sin contexto reciente." : RecentConversation);
        sb.AppendLine();
        sb.AppendLine("MEMORIA COMPARTIDA ENTRE GROQ Y GEMINI:");
        sb.AppendLine(string.IsNullOrWhiteSpace(ModelMemory) ? "Sin memoria compartida todavía." : ModelMemory);
        sb.AppendLine();
        sb.AppendLine("PREFERENCIAS:");
        sb.AppendLine(UserPreferences);
        sb.AppendLine($"Modo de respuesta: {ResponseMode}");
        return sb.ToString();
    }
}
