using Hanna.Core;

namespace Hanna.Services;

internal sealed class WebChatHostService : IDisposable
{
    private readonly AppConfig config;
    private Process? process;

    public WebChatHostService(AppConfig config)
    {
        this.config = config;
    }

    public void Start()
    {
        if (!config.WebChatEnabled || !config.NodeJsEnabled)
        {
            Console.WriteLine("[WebChat] Standalone deshabilitado. Chat principal integrado en el panel web 8787.");
            return;
        }

        string serverPath = Path.Combine(config.WebChatDirectory, "server.js");
        if (!File.Exists(serverPath))
        {
            Console.WriteLine("[WebChat] No encontrado: " + serverPath);
            return;
        }

        try
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = config.NodeExecutable,
                    Arguments = "\"" + serverPath + "\"",
                    WorkingDirectory = config.WebChatDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Console.WriteLine("[WebChat] " + e.Data); };
            process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Console.WriteLine("[WebChat Error] " + e.Data); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            Console.WriteLine($"[WebChat] Activo en http://127.0.0.1:{config.WebChatPort}/");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[WebChat Error]: " + ex.Message);
        }
    }

    public void Dispose()
    {
        try
        {
            if (process != null && !process.HasExited)
                process.Kill(true);
        }
        catch { }
        process?.Dispose();
    }
}
