using Hanna.Core;

namespace Hanna.Services;

internal sealed class CodeOutputService
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService? runtime;

    public CodeOutputService(AppConfig config, RuntimeSettingsService? runtime = null)
    {
        this.config = config;
        this.runtime = runtime;
    }

    public async Task<string?> SaveIfContainsCode(string userRequest, string responseText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return null;

        string code = ExtractBestCodeBlock(responseText);
        if (string.IsNullOrWhiteSpace(code) && LooksLikeCodeRequest(userRequest))
            code = responseText.Trim();

        if (string.IsNullOrWhiteSpace(code))
            return null;

        return await SaveCode(userRequest, code, cancellationToken);
    }

    public async Task<string> SaveCode(string title, string code, CancellationToken cancellationToken)
    {
        string outputDirectory = runtime?.Snapshot().AgentOutputDirectory ?? config.AgentOutputDirectory;
        Directory.CreateDirectory(outputDirectory);

        string safe = Regex.Replace(title ?? "codigo", @"[^a-zA-Z0-9áéíóúÁÉÍÓÚñÑ_-]+", "_").Trim('_');
        if (safe.Length > 42)
            safe = safe[..42];
        if (string.IsNullOrWhiteSpace(safe))
            safe = "codigo";

        string path = Path.Combine(outputDirectory, $"{DateTime.Now:yyyyMMdd_HHmmss}_{safe}.txt");
        await File.WriteAllTextAsync(path, code, Encoding.UTF8, cancellationToken);
        return path;
    }

    public void OpenGeneratedFile(string path)
    {
        bool open = runtime?.Snapshot().AgentOpenGeneratedCode ?? config.AgentOpenGeneratedCode;
        if (!open || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        // No abrir Visual Studio ni VS Code automáticamente: consume recursos y estorba.
        // Si el usuario activa HANNA_AGENT_OPEN_GENERATED_CODE=true, se abre en Bloc de notas minimizado.
        try
        {
            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"")
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            });
        }
        catch
        {
        }
    }

    private static string ExtractBestCodeBlock(string text)
    {
        var matches = Regex.Matches(text, @"```(?:[a-zA-Z0-9_#+.-]+)?\s*([\s\S]*?)```", RegexOptions.Multiline);
        if (matches.Count == 0)
            return "";

        return matches
            .Select(m => m.Groups[1].Value.Trim())
            .OrderByDescending(x => x.Length)
            .FirstOrDefault() ?? "";
    }

    public static bool LooksLikeCodeRequest(string text)
    {
        string normalized = Utilities.TextTools.Normalize(text ?? "");

        return Regex.IsMatch(normalized, @"\b(codigo|código|programa|programame|programar|script|sql|base de datos|clase|metodo|método|funcion|función|html|css|javascript|csharp|c#|python|java|visual studio|vscode|code)\b");
    }
}
