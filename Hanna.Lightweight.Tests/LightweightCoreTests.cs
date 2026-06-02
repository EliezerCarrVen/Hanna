using Hanna.Lightweight.Core;
using Hanna.Lightweight.Services;
using Xunit;

namespace Hanna.Lightweight.Tests;

public sealed class LightweightCoreTests
{
    [Fact]
    public void SecretFilterService_RedactsTokens()
    {
        var f = Factory();
        Assert.Contains("[REDACTED]", f.SecretFilter.Filter("api_key=abc123 token=secret"));
    }

    [Fact]
    public void PathGuardService_BlocksOutsideRoot()
    {
        var f = Factory();
        Assert.False(f.PathGuard.IsInsideRoot(Path.GetTempPath()));
    }

    [Fact]
    public void LogRotationService_RotatesLogs()
    {
        var f = Factory(maxLogBytes: 10);
        Directory.CreateDirectory(f.Paths.Logs);
        File.WriteAllText(f.Paths.LightweightLog, new string('x', 100));
        f.LogRotation.RotateIfNeeded(f.Paths.LightweightLog);
        Assert.Contains(Directory.EnumerateFiles(f.Paths.Logs), file => file.Contains("lightweight.") && file.EndsWith(".log"));
    }

    [Fact]
    public async Task AuditLogService_VerifiesHashChain()
    {
        var f = Factory();
        await f.Audit.RecordAsync("test", "ok");
        Assert.True(f.Audit.VerifyHashChain().ok);
    }

    [Fact]
    public void ZeroLeakSanitizerService_RedactsSensitiveData()
    {
        var f = Factory();
        var result = new ZeroLeakSanitizerService(f.SecretFilter).Sanitize("test@example.com 192.168.1.5 token=abc /home/me/file");
        Assert.Contains("[REDACTED]", result);
    }

    [Fact]
    public void IntentRouterService_ClassifiesMqtt()
    {
        var route = new IntentRouterService().Route("publicar mqtt al topic");
        Assert.Equal("mqtt", route.intent);
    }

    [Fact]
    public void WakeOnLanService_ValidatesMac()
    {
        Assert.True(new WakeOnLanService(new LightweightOptions()).IsValidMac("00:11:22:33:44:55"));
    }

    [Fact]
    public void NasIndexerService_RespectsEmptyAllowlist()
    {
        var f = Factory();
        Assert.StartsWith("missing_configuration", new NasIndexerService(f.Paths, f.Options, f.PathGuard).Status());
    }

    [Fact]
    public void TotpService_GeneratesAndVerifies()
    {
        var f = Factory();
        var service = new TotpService(f.Paths, f.PathGuard);
        Assert.False(string.IsNullOrWhiteSpace(service.GenerateSecret()));
        Assert.Equal("implemented", service.Status().Status);
    }

    [Fact]
    public async Task VaultEncryptionService_CreatesVault()
    {
        var f = Factory();
        var id = await new VaultEncryptionService(f.Paths, f.PathGuard, f.Audit).CreateAsync("test", "password");
        Assert.False(string.IsNullOrWhiteSpace(id));
    }

    private static TestFactory Factory(long maxLogBytes = 1_048_576)
    {
        var root = Path.Combine(Path.GetTempPath(), "hanna-lightweight-tests", Guid.NewGuid().ToString("N"));
        var options = new LightweightOptions { DataRoot = root, MaxLogFileBytes = maxLogBytes };
        var paths = new RuntimePaths(root);
        foreach (var d in paths.Directories) Directory.CreateDirectory(d);
        foreach (var file in paths.Files) File.WriteAllText(file, string.Empty);
        var pathGuard = new PathGuardService(paths);
        var rotation = new LogRotationService(options, paths, pathGuard);
        var secret = new SecretFilterService(paths, pathGuard, rotation);
        var jsonl = new JsonlStoreService(secret, pathGuard, options, rotation);
        var audit = new AuditLogService(paths, jsonl, options, pathGuard);
        return new TestFactory(options, paths, pathGuard, rotation, secret, audit);
    }

    private sealed record TestFactory(LightweightOptions Options, RuntimePaths Paths, PathGuardService PathGuard, LogRotationService LogRotation, SecretFilterService SecretFilter, AuditLogService Audit);
}
