using Hanna.Core;

namespace Hanna.Services;

internal sealed class TelegramMirrorService
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService? runtime;
    private readonly HttpClient httpClient = new();

    public TelegramMirrorService(AppConfig config, RuntimeSettingsService? runtime = null)
    {
        this.config = config;
        this.runtime = runtime;
    }

    public async Task MirrorLocalUser(string text, CancellationToken cancellationToken)
    {
        await Send($"👤 Usuario local:\n{text}", cancellationToken);
    }

    public async Task MirrorLocalHanna(string text, CancellationToken cancellationToken)
    {
        await Send($"🤖 Hanna local:\n{text}", cancellationToken);
    }

    public async Task MirrorSystem(string text, CancellationToken cancellationToken)
    {
        await Send($"🖥️ Hanna PC:\n{text}", cancellationToken);
    }

    public async Task Send(string text, CancellationToken cancellationToken)
    {
        bool enabled = runtime?.Snapshot().MirrorLocalToTelegram ?? config.MirrorLocalToTelegram;
        if (!enabled)
            return;

        if (config.LocalChatId == 0 || string.IsNullOrWhiteSpace(config.TelegramToken) || string.IsNullOrWhiteSpace(text))
            return;

        foreach (string part in Split(text, 3900))
            await SendPart(config.LocalChatId, part, cancellationToken);
    }

    private async Task SendPart(long chatId, string text, CancellationToken cancellationToken)
    {
        string url = $"https://api.telegram.org/bot{config.TelegramToken}/sendMessage";
        var payload = new { chat_id = chatId, text };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            await httpClient.PostAsync(url, content, cancellationToken);
        }
        catch
        {
        }
    }

    private static IEnumerable<string> Split(string text, int max)
    {
        while (text.Length > max)
        {
            int cut = text.LastIndexOf('\n', max);
            if (cut < 500)
                cut = max;

            yield return text[..cut].Trim();
            text = text[cut..].Trim();
        }

        if (!string.IsNullOrWhiteSpace(text))
            yield return text;
    }
}
