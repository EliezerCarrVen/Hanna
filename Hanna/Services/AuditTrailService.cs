using Hanna.Core;

namespace Hanna.Services;

internal sealed class AuditTrailService
{
    private readonly AppConfig config;

    public AuditTrailService(AppConfig config)
    {
        this.config = config;
    }

    public async Task AppendAsync(long chatId, string source, string action, string detail, bool success, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(config.AuditLogPath) ?? config.BaseDirectory);
            var entry = new
            {
                ts = DateTimeOffset.Now,
                chatId,
                source,
                action,
                detail = detail.Length > 1200 ? detail[..1200] : detail,
                success
            };
            await File.AppendAllTextAsync(config.AuditLogPath, JsonSerializer.Serialize(entry) + Environment.NewLine, Encoding.UTF8, cancellationToken);
        }
        catch
        {
            // La auditoría no debe tumbar a Hanna.
        }
    }
}
