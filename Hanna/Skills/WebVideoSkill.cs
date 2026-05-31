using Hanna.Core;
using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class WebVideoSkill : ISkill
{
    private readonly WebVideoDownloadService downloader;
    private readonly AppConfig config;
    private readonly HttpClient httpClient = new();

    public WebVideoSkill(AppConfig config, WebVideoDownloadService downloader)
    {
        this.config = config;
        this.downloader = downloader;
    }

    public bool CanHandle(IntentResult intent) => intent.Type == IntentType.WebVideoDownload;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        await SendText(chatId, "Buscando y extrayendo el video. Si el sitio se pone diva, te aviso.", cancellationToken);

        var result = await downloader.DownloadVideo(originalText, chatId, cancellationToken);

        if (!result.Success)
            return SkillResult.Text(result.Message, true);

        bool sent = await SendVideo(chatId, result.FilePath, cancellationToken);

        if (!sent)
            return SkillResult.Text("Descargué el video, pero no pude enviarlo por Telegram. Revisa tamaño o formato. Archivo local: " + result.FilePath, true);

        try
        {
            File.Delete(result.FilePath);
        }
        catch
        {
        }

        return SkillResult.Silent();
    }

    private async Task SendText(long chatId, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.TelegramToken))
            return;

        string url = $"https://api.telegram.org/bot{config.TelegramToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        await httpClient.PostAsync(url, content, cancellationToken);
    }

    private async Task<bool> SendVideo(long chatId, string filePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.TelegramToken))
            return false;

        if (!File.Exists(filePath))
            return false;

        string url = $"https://api.telegram.org/bot{config.TelegramToken}/sendVideo";

        await using var stream = File.OpenRead(filePath);

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");

        content.Add(new StringContent(chatId.ToString()), "chat_id");
        content.Add(streamContent, "video", Path.GetFileName(filePath));

        using var response = await httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine("[Telegram sendVideo Error] " + error);
        }

        return response.IsSuccessStatusCode;
    }
}
