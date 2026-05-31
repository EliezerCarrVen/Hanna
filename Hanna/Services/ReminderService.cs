using Hanna.Core;
using Telegram.Bot;

namespace Hanna.Services;

internal sealed class ReminderService
{
    private readonly AppConfig config;

    public ReminderService(AppConfig config)
    {
        this.config = config;
    }

    private string GetPath(long chatId) => Path.Combine(config.SettingsDirectory, $"reminders_{chatId}.txt");

    public async Task<string> CreateReminder(TelegramBotClient botClient, long chatId, string originalText, CancellationToken cancellationToken)
    {
        DateTime when = DateTime.Now.AddMinutes(5);

        var minutesMatch = Regex.Match(originalText, @"(?:en|dentro de)\s+(\d{1,3})\s+(minuto|minutos)", RegexOptions.IgnoreCase);
        var hoursMatch = Regex.Match(originalText, @"(?:en|dentro de)\s+(\d{1,2})\s+(hora|horas)", RegexOptions.IgnoreCase);

        if (minutesMatch.Success && int.TryParse(minutesMatch.Groups[1].Value, out int minutes))
            when = DateTime.Now.AddMinutes(minutes);
        else if (hoursMatch.Success && int.TryParse(hoursMatch.Groups[1].Value, out int hours))
            when = DateTime.Now.AddHours(hours);

        string message = Regex.Replace(originalText, @"\b(recuerdame|recuérdame|recordatorio|alarma|en|dentro de|minuto|minutos|hora|horas|\d+)\b", " ", RegexOptions.IgnoreCase);
        message = Regex.Replace(message, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(message))
            message = "Recordatorio pendiente.";

        string line = $"{when:yyyy-MM-dd HH:mm:ss} | {message}{Environment.NewLine}";
        await File.AppendAllTextAsync(GetPath(chatId), line, Encoding.UTF8, cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                TimeSpan delay = when - DateTime.Now;

                if (delay.TotalMilliseconds > 0)
                    await Task.Delay(delay);

                await botClient.SendMessage(chatId, $"Recordatorio: {message}");
            }
            catch
            {
            }
        });

        return $"Listo. Te recordaré: {message} a las {when:HH:mm}.";
    }

    public async Task<string> ListReminders(long chatId, CancellationToken cancellationToken)
    {
        string path = GetPath(chatId);

        if (!File.Exists(path))
            return "No tienes recordatorios guardados.";

        string content = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);

        return string.IsNullOrWhiteSpace(content) ? "No tienes recordatorios guardados." : "Tus recordatorios:\n" + content.Trim();
    }
}
