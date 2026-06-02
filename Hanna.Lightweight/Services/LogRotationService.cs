using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class LogRotationService(LightweightOptions options, RuntimePaths paths, PathGuardService pathGuard)
{
    public void RotateIfNeeded(string logPath)
    {
        var safePath = pathGuard.EnsureInsideRoot(logPath);
        if (!File.Exists(safePath))
        {
            return;
        }

        var info = new FileInfo(safePath);
        if (info.Length <= options.MaxLogFileBytes)
        {
            return;
        }

        var directory = info.DirectoryName ?? paths.Logs;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(info.Name);
        var rotatedPath = Path.Combine(directory, $"{fileNameWithoutExtension}.{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.log");
        pathGuard.EnsureInsideRoot(rotatedPath);
        File.Move(safePath, rotatedPath, overwrite: false);
    }

    public void RotateKnownLogs()
    {
        RotateIfNeeded(paths.LightweightLog);
        RotateIfNeeded(paths.AuditLog);
        RotateIfNeeded(paths.SecurityLog);
    }
}
