using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Hanna.Lightweight.Services;

public sealed record DependencyStatus(string Name, bool Found, string? Path, string? Version, string DebianInstall, string WindowsInstall, string Status);

public sealed class DependencyCheckerService
{
    private static readonly (string name, string[] commands, string debian, string windows)[] Dependencies =
    [
        ("dotnet", ["dotnet"], "Instalar Microsoft .NET SDK desde packages.microsoft.com", "winget install Microsoft.DotNet.SDK.10"),
        ("rg", ["rg"], "sudo apt install ripgrep", "winget install BurntSushi.ripgrep.MSVC"),
        ("git", ["git"], "sudo apt install git", "winget install Git.Git"),
        ("docker", ["docker"], "sudo apt install docker.io", "winget install Docker.DockerDesktop"),
        ("clamscan", ["clamscan"], "sudo apt install clamav clamav-daemon", "winget install ClamAV.ClamAV"),
        ("node", ["node"], "sudo apt install nodejs", "winget install OpenJS.NodeJS.LTS"),
        ("npm", ["npm"], "sudo apt install npm", "Incluido con Node.js"),
        ("node-red", ["node-red"], "sudo npm install -g --unsafe-perm node-red", "npm install -g --unsafe-perm node-red"),
        ("mosquitto", ["mosquitto", "mosquitto_pub"], "sudo apt install mosquitto mosquitto-clients", "winget install EclipseMosquitto.Mosquitto"),
        ("ssh", ["ssh"], "sudo apt install openssh-client", "Habilitar OpenSSH Client en Windows"),
        ("ping", ["ping"], "sudo apt install iputils-ping", "Incluido en Windows"),
        ("curl", ["curl"], "sudo apt install curl", "winget install curl.curl"),
        ("systemctl", ["systemctl"], "Incluido con systemd", "No aplica"),
        ("timedatectl", ["timedatectl"], "Incluido con systemd", "No aplica"),
        ("ipconfig/ifconfig/ip", ["ip", "ifconfig", "ipconfig"], "sudo apt install iproute2 net-tools", "Incluido en Windows"),
        ("powershell", ["pwsh", "powershell"], "sudo apt install powershell", "Incluido en Windows / winget install Microsoft.PowerShell")
    ];

    public IReadOnlyList<DependencyStatus> CheckAll() => Dependencies.Select(d => Check(d.name, d.commands, d.debian, d.windows)).ToArray();

    public DependencyStatus Check(string name)
    {
        foreach (var d in Dependencies)
        {
            if (d.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return Check(d.name, d.commands, d.debian, d.windows);
            }
        }

        return Check(name, [name], "Instalar paquete correspondiente", "Instalar dependencia correspondiente");
    }

    public bool IsFound(string name) => Check(name).Found;

    private static DependencyStatus Check(string name, string[] commands, string debian, string windows)
    {
        foreach (var command in commands)
        {
            var path = FindExecutable(command);
            if (path is not null)
            {
                return new DependencyStatus(name, true, path, TryGetVersion(command), debian, windows, "implemented");
            }
        }

        return new DependencyStatus(name, false, null, null, debian, windows, "missing_dependency");
    }

    private static string? FindExecutable(string command)
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD").Split(';')
            : [string.Empty];

        foreach (var dir in paths)
        {
            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(dir, command + ext);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static string? TryGetVersion(string command)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = command,
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process is null || !process.WaitForExit(1200)) return null;
            var output = process.StandardOutput.ReadLine() ?? process.StandardError.ReadLine();
            return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
        }
        catch
        {
            return null;
        }
    }
}
