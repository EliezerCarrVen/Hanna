using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class PathGuardService(RuntimePaths paths)
{
    private readonly string _root = NormalizeDirectory(paths.DataRoot);

    public string Root => _root;

    public bool IsInsideRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || HasParentTraversal(path))
        {
            return false;
        }

        var fullPath = NormalizePath(path);
        return fullPath.Equals(_root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase);
    }

    public string EnsureInsideRoot(string? path)
    {
        RejectIfSensitivePath(path);
        if (!IsInsideRoot(path))
        {
            ThrowBlocked("ruta fuera de HannaData");
        }

        return NormalizePath(path!);
    }

    public void RejectIfSensitivePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            ThrowBlocked("ruta vacía");
        }

        if (HasParentTraversal(path))
        {
            ThrowBlocked("ruta con traversal");
        }

        var fileName = Path.GetFileName(path);
        var normalized = path.Replace('\\', '/');
        var lower = normalized.ToLowerInvariant();
        var lowerFile = fileName.ToLowerInvariant();

        if (lower.Contains("hannaenv", StringComparison.Ordinal)
            || lowerFile is ".env" or "hannaenv.env"
            || lower.Contains("appsettings.development.json", StringComparison.Ordinal)
            || lower.Contains("appsettings.production.json", StringComparison.Ordinal)
            || lower.Contains("appsettings.secrets.json", StringComparison.Ordinal)
            || lower.Contains("google_client_secret", StringComparison.Ordinal)
            || lower.Contains("secret", StringComparison.Ordinal) && lower.Contains("config", StringComparison.Ordinal))
        {
            ThrowBlocked("ruta de configuración sensible");
        }
    }

    private void ThrowBlocked(string reason)
    {
        try
        {
            Directory.CreateDirectory(paths.Logs);
            var message = $"{DateTimeOffset.UtcNow:O} PathGuard blocked attempt: {reason}. Path value intentionally omitted.{Environment.NewLine}";
            File.AppendAllText(paths.SecurityLog, message);
            File.AppendAllText(paths.AuditLog, $"{{\"timestampUtc\":\"{DateTimeOffset.UtcNow:O}\",\"eventType\":\"pathguard_blocked\",\"actor\":\"local-runtime\",\"description\":\"PathGuard blocked attempt: {reason}. Path omitted.\",\"dryRun\":true,\"severity\":\"warn\"}}{Environment.NewLine}");
        }
        catch
        {
            // PathGuard must fail closed even if logging is unavailable.
        }

        throw new InvalidOperationException($"PathGuard bloqueó una {reason}.");
    }

    private static bool HasParentTraversal(string path)
    {
        var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment == "..");
    }

    private static string NormalizeDirectory(string path)
    {
        var full = NormalizePath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return full + Path.DirectorySeparatorChar;
    }

    private static string NormalizePath(string path) => Path.GetFullPath(path);
}
