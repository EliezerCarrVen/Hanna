namespace Hanna.Services;

internal sealed class AppLauncherService
{
    public string Open(string text)
    {
        string normalized = Utilities.TextTools.Normalize(text);
        string target = ResolveTarget(normalized);

        if (string.IsNullOrWhiteSpace(target))
            return "No reconocí qué aplicación quieres abrir. Puedes decir: abre Chrome, abre Spotify, abre VS Code, abre Visual Studio, abre bloc de notas o abre PowerShell.";

        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            return $"Abrí {target}.";
        }
        catch (Exception ex)
        {
            return $"No pude abrir {target}. Detalle: {ex.Message}";
        }
    }

    public string OpenFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return "No encontré el archivo para abrir.";

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return "Archivo abierto.";
        }
        catch (Exception ex)
        {
            return "No pude abrir el archivo. Detalle: " + ex.Message;
        }
    }

    public string Shutdown()
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(1200);
            Environment.Exit(0);
        });

        return "Cerrando Hanna.";
    }

    private static string ResolveTarget(string normalized)
    {
        if (normalized.Contains("spotify")) return "spotify:";
        if (normalized.Contains("chrome")) return "chrome";
        if (normalized.Contains("edge")) return "msedge";
        if (normalized.Contains("bloc") || normalized.Contains("notepad") || normalized.Contains("notas")) return "notepad";
        if (normalized.Contains("calculadora")) return "calc";
        if (normalized.Contains("powershell")) return "powershell";
        if (normalized.Contains("terminal")) return "wt";
        if (normalized.Contains("cmd")) return "cmd";
        if (normalized.Contains("visual studio code") || normalized.Contains("vs code") || normalized.Contains("vscode")) return "code";
        if (normalized.Contains("visual studio")) return "devenv";
        if (normalized.Contains("explorador") || normalized.Contains("explorer") || normalized.Contains("archivos")) return "explorer";
        if (normalized.Contains("word")) return "winword";
        if (normalized.Contains("excel")) return "excel";

        return "";
    }
}
