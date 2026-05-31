using Hanna.Core;

namespace Hanna.Services;

internal sealed class ContextArchiveService
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService? runtime;
    private readonly object sync = new();

    public ContextArchiveService(AppConfig config, RuntimeSettingsService? runtime = null)
    {
        this.config = config;
        this.runtime = runtime;
    }

    public Task SaveAsync(long chatId, string source, string role, string text, CancellationToken cancellationToken = default)
    {
        if (!config.ContextAlwaysSave || string.IsNullOrWhiteSpace(text))
            return Task.CompletedTask;

        try
        {
            string dir = config.ContextArchiveDirectory;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"context_{chatId}_{DateTime.Now:yyyyMMdd}.jsonl");
            var row = new
            {
                at = DateTimeOffset.Now,
                chatId,
                source,
                role,
                text = text.Trim()
            };
            string json = JsonSerializer.Serialize(row) + Environment.NewLine;
            lock (sync)
                File.AppendAllText(path, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ContextArchive Error]: " + ex.Message);
        }

        return Task.CompletedTask;
    }
}
