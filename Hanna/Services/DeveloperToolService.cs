using Hanna.Core;
using MySqlConnector;

namespace Hanna.Services;

internal sealed class DeveloperToolService
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService runtime;

    public DeveloperToolService(AppConfig config, RuntimeSettingsService runtime)
    {
        this.config = config;
        this.runtime = runtime;
    }

    public async Task<object> BuildStatus(CancellationToken cancellationToken)
    {
        RuntimeSettings s = runtime.Snapshot();
        return new
        {
            node = await CheckProcess(config.NodeExecutable, "--version", cancellationToken),
            vscode = await CheckProcess(config.VsCodeExecutable, "--version", cancellationToken),
            visualStudio = await CheckProcess(config.VisualStudioExecutable, "/?", cancellationToken, allowFailure: true),
            mysql = await CheckMySql(cancellationToken),
            mongo = new { enabled = config.MongoEnabled, database = config.MongoDatabase, uri = Mask(config.MongoUri) },
            projectsDirectory = s.ProjectsDirectory,
            agentOutputDirectory = s.AgentOutputDirectory,
            nodeProjectsDirectory = config.NodeProjectsDirectory
        };
    }

    private async Task<object> CheckMySql(CancellationToken cancellationToken)
    {
        if (!config.MySqlEnabled)
            return new { ok = false, message = "MySQL deshabilitado" };

        try
        {
            await using var connection = new MySqlConnection(config.MySqlConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var cmd = new MySqlCommand("SELECT DATABASE();", connection);
            object? db = await cmd.ExecuteScalarAsync(cancellationToken);
            return new { ok = true, database = db?.ToString() ?? "" };
        }
        catch (Exception ex)
        {
            return new { ok = false, message = ex.Message };
        }
    }

    private static async Task<object> CheckProcess(string fileName, string arguments, CancellationToken cancellationToken, bool allowFailure = false)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return new { ok = false, message = "No configurado" };

        try
        {
            using var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(cts.Token);

            string output = (await process.StandardOutput.ReadToEndAsync(cancellationToken)).Trim();
            string error = (await process.StandardError.ReadToEndAsync(cancellationToken)).Trim();
            bool ok = process.ExitCode == 0 || allowFailure;
            return new { ok, exitCode = process.ExitCode, output = Utilities.TextTools.Clip(string.IsNullOrWhiteSpace(output) ? error : output, 400) };
        }
        catch (Exception ex)
        {
            return new { ok = false, message = ex.Message };
        }
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        return Regex.Replace(value, @"(password|pwd)=([^;]+)", "$1=***", RegexOptions.IgnoreCase);
    }
}
