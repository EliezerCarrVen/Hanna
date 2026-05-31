using Hanna.Core;

namespace Hanna.Services;

internal sealed class OllamaDaemonService : IDisposable
{
    private readonly AppConfig config;
    private readonly HttpClient httpClient = new();
    private Process? process;
    private bool disposed;

    public OllamaDaemonService(AppConfig config)
    {
        this.config = config;
    }

    public async Task EnsureRunningAsync(CancellationToken cancellationToken)
    {
        if (!config.OllamaAutoStart)
            return;

        if (await IsAvailable(cancellationToken))
        {
            Console.WriteLine("[Ollama] API local disponible.");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo(config.OllamaExecutable)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            psi.ArgumentList.Add("serve");
            process = Process.Start(psi);
            Console.WriteLine("[Ollama] Iniciando servidor local en segundo plano...");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Ollama] No pude iniciar 'ollama serve'. Detalle: " + ex.Message);
            return;
        }

        DateTime limit = DateTime.UtcNow.AddSeconds(Math.Max(5, config.OllamaStartupTimeoutSeconds));

        while (DateTime.UtcNow < limit && !cancellationToken.IsCancellationRequested)
        {
            if (await IsAvailable(cancellationToken))
            {
                Console.WriteLine("[Ollama] Servidor listo.");
                return;
            }

            await Task.Delay(700, cancellationToken);
        }

        Console.WriteLine("[Ollama] El servidor no respondió dentro del tiempo esperado. Hanna seguirá con respaldos.");
    }

    public async Task<bool> IsAvailable(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(1500);
            using var response = await httpClient.GetAsync(config.OllamaBaseUrl.TrimEnd('/') + "/api/tags", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        httpClient.Dispose();
    }
}
