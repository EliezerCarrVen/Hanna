using Hanna.Core;

namespace Hanna.Services;

internal sealed class MemoryService
{
    private readonly AppConfig config;

    public MemoryService(AppConfig config)
    {
        this.config = config;
    }

    private string GetPath(long chatId) => Path.Combine(config.MemoryDirectory, $"user_memory_{chatId}.txt");

    public async Task Save(long chatId, string memory, CancellationToken cancellationToken)
    {
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {SecretSanitizer.Sanitize(memory.Trim())}{Environment.NewLine}";
        await File.AppendAllTextAsync(GetPath(chatId), line, Encoding.UTF8, cancellationToken);
    }

    public async Task<string> Read(long chatId, CancellationToken cancellationToken)
    {
        string path = GetPath(chatId);

        if (!File.Exists(path))
            return "Aún no tengo memoria guardada para este chat.";

        string content = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);

        return string.IsNullOrWhiteSpace(content) ? "Aún no tengo memoria guardada para este chat." : SecretSanitizer.Sanitize(content.Trim());
    }
}
