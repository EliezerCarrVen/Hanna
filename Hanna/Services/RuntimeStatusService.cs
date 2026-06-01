namespace Hanna.Services;

internal sealed class RuntimeStatusService
{
    private readonly object sync = new();
    private readonly SafeLogService logs;
    private readonly Dictionary<string, ServiceStatus> services = new(StringComparer.OrdinalIgnoreCase);

    public RuntimeStatusService(SafeLogService logs)
    {
        this.logs = logs;
    }

    public void RecordDecision(string name, bool enabled, string reason)
    {
        lock (sync)
            services[name] = new ServiceStatus(name, enabled ? "habilitado" : "omitido", reason, DateTimeOffset.Now);
    }

    public void Started(string name)
    {
        lock (sync)
            services[name] = new ServiceStatus(name, "activo", "Inicio confirmado.", DateTimeOffset.Now);
        logs.Info("motores", name + ": activo");
    }

    public void Skipped(string name, string reason)
    {
        lock (sync)
            services[name] = new ServiceStatus(name, "omitido", reason, DateTimeOffset.Now);
    }

    public void Failed(string name, Exception ex)
    {
        string safe = SecretSanitizer.Sanitize(ex.Message, 600);
        lock (sync)
            services[name] = new ServiceStatus(name, "error", safe, DateTimeOffset.Now);
        logs.Error("errors", ex);
    }

    public IReadOnlyList<ServiceStatus> Snapshot()
    {
        lock (sync)
            return services.Values.OrderBy(x => x.Name).ToList();
    }

    public string GetStatus(string name)
    {
        lock (sync)
            return services.TryGetValue(name, out var status) ? status.State : "sin iniciar";
    }
}

internal sealed record ServiceStatus(string Name, string State, string Detail, DateTimeOffset UpdatedAt);
