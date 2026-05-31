using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class AssignmentSkill : ISkill
{
    private readonly AssignmentService assignments;
    private readonly CourseNotebookService notebooks;
    private readonly GoogleIntegrationService google;

    public AssignmentSkill(AssignmentService assignments, CourseNotebookService notebooks, GoogleIntegrationService google)
    {
        this.assignments = assignments;
        this.notebooks = notebooks;
        this.google = google;
    }

    public bool CanHandle(IntentResult intent)
    {
        return intent.Type is IntentType.AssignmentCreate or IntentType.AssignmentList or IntentType.AssignmentCheck or IntentType.NotebookCreate;
    }

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        switch (intent.Type)
        {
            case IntentType.AssignmentList:
                return SkillResult.Text(ListAssignments(), true);
            case IntentType.AssignmentCheck:
                await assignments.CheckAndNotify(cancellationToken);
                return SkillResult.Text("Revisé las tareas registradas. Para Google Classroom, configura credenciales OAuth desde el panel web.", true);
            case IntentType.NotebookCreate:
                string subject = ExtractSubject(originalText);
                string folder = notebooks.CreateOrUpdateNotebook(subject, "Comando de usuario", originalText);
                return SkillResult.Text("Listo. Preparé/actualicé el cuaderno de estudio: " + folder, true);
            case IntentType.AssignmentCreate:
                AssignmentItem item = ParseAssignment(originalText);
                assignments.Upsert(item);
                string notebook = notebooks.CreateOrUpdateNotebook(item.Subject, "Tarea", $"# {item.Title}\n\nEntrega: {item.DueAt:yyyy-MM-dd HH:mm}\n\n{item.Description}\n\n{item.Url}");
                return SkillResult.Text($"Registré la tarea '{item.Title}' de {item.Subject}. Entrega: {item.DueAt:yyyy-MM-dd HH:mm}. Cuaderno: {notebook}", true);
            default:
                return SkillResult.NotHandled();
        }
    }

    private string ListAssignments()
    {
        var items = assignments.List().Where(x => !x.Completed).OrderBy(x => x.DueAt).Take(20).ToList();
        if (items.Count == 0)
            return "No tengo tareas pendientes registradas.";

        var sb = new StringBuilder();
        sb.AppendLine("Tareas pendientes:");
        int i = 1;
        foreach (var item in items)
        {
            sb.AppendLine($"{i}. {item.Subject}: {item.Title} - entrega {item.DueAt:yyyy-MM-dd HH:mm}");
            i++;
        }
        return sb.ToString().Trim();
    }

    private static AssignmentItem ParseAssignment(string text)
    {
        string subject = ExtractSubject(text);
        string title = ExtractTitle(text);
        DateTime due = ExtractDueDate(text);
        return new AssignmentItem
        {
            Subject = subject,
            Title = title,
            Description = text,
            Source = "Voz/chat",
            DueAt = due
        };
    }

    private static string ExtractSubject(string text)
    {
        var match = Regex.Match(text, @"\b(?:materia|clase|curso)\s+(?:de\s+)?(.+?)(?:\s+(?:tarea|actividad|entrega|para|con|que)|$)", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value.Trim();
        return "Materia general";
    }

    private static string ExtractTitle(string text)
    {
        string cleaned = Regex.Replace(text, @"\b(hanna|crea|registra|agrega|nueva|tarea|actividad|entrega|materia|clase|curso)\b", " ", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Tarea" : Hanna.Utilities.TextTools.Clip(cleaned, 80);
    }

    private static DateTime ExtractDueDate(string text)
    {
        string normalized = Hanna.Utilities.TextTools.Normalize(text);
        DateTime now = DateTime.Now;
        if (normalized.Contains("mañana") || normalized.Contains("manana"))
            return now.Date.AddDays(1).AddHours(23).AddMinutes(59);
        if (normalized.Contains("hoy"))
            return now.Date.AddHours(23).AddMinutes(59);
        if (normalized.Contains("pasado mañana") || normalized.Contains("pasado manana"))
            return now.Date.AddDays(2).AddHours(23).AddMinutes(59);

        var m = Regex.Match(text, @"\b(\d{1,2})[/-](\d{1,2})(?:[/-](\d{2,4}))?\b");
        if (m.Success && int.TryParse(m.Groups[1].Value, out int d) && int.TryParse(m.Groups[2].Value, out int mo))
        {
            int y = now.Year;
            if (m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out int yy))
                y = yy < 100 ? 2000 + yy : yy;
            try { return new DateTime(y, mo, d, 23, 59, 0); } catch { }
        }
        return now.AddDays(1);
    }
}
