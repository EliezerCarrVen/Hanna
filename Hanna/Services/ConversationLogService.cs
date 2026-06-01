using Hanna.Core;
using Hanna.Utilities;

namespace Hanna.Services;

internal sealed class ConversationLogService
{
    private readonly AppConfig config;
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly object syncLock = new();

    public string SessionLogPath { get; }

    public ConversationLogService(AppConfig config)
    {
        this.config = config;
        SessionLogPath = Path.Combine(config.LogsDirectory, $"sesion_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

        Console.CancelKeyPress += (_, _) => RegisterSystemSync("Hanna recibió solicitud de cierre desde consola.");
        AppDomain.CurrentDomain.ProcessExit += (_, _) => RegisterSystemSync("Hanna cerró el proceso.");
    }

    public async Task RegisterMessage(long chatId, string role, string content, CancellationToken cancellationToken = default)
    {
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [CHAT {chatId}] [{role}] {SecretSanitizer.Sanitize(TextTools.CleanLog(content))}{Environment.NewLine}";

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            await File.AppendAllTextAsync(SessionLogPath, line, Encoding.UTF8, cancellationToken);
            await File.AppendAllTextAsync(Path.Combine(config.ContextDirectory, $"chat_{chatId}.txt"), line, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task RegisterSystem(string content, CancellationToken cancellationToken = default)
    {
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SISTEMA] {SecretSanitizer.Sanitize(content)}{Environment.NewLine}";

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            await File.AppendAllTextAsync(SessionLogPath, line, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void RegisterSystemSync(string content)
    {
        try
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SISTEMA] {SecretSanitizer.Sanitize(content)}{Environment.NewLine}";

            lock (syncLock)
                File.AppendAllText(SessionLogPath, line, Encoding.UTF8);
        }
        catch
        {
        }
    }
}
