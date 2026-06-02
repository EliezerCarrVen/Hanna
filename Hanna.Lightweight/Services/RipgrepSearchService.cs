using System.Diagnostics;
using Hanna.Lightweight.Models;

namespace Hanna.Lightweight.Services;

public sealed class RipgrepSearchService(SecretFilterService secretFilter)
{
    public bool IsRipgrepAvailable { get; } = CheckRipgrep();

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string rootPath, string text, CancellationToken cancellationToken = default)
    {
        var safeText = secretFilter.Filter(text);
        if (string.IsNullOrWhiteSpace(safeText) || !Directory.Exists(rootPath))
        {
            return [];
        }

        return IsRipgrepAvailable
            ? await SearchWithRipgrepAsync(rootPath, safeText, cancellationToken)
            : await SearchWithFallbackAsync(rootPath, safeText, cancellationToken);
    }

    private static bool CheckRipgrep()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "rg",
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit(1500);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<IReadOnlyList<SearchResult>> SearchWithRipgrepAsync(string rootPath, string text, CancellationToken cancellationToken)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "rg",
            ArgumentList = { "--line-number", "--fixed-strings", "--ignore-case", text, rootPath },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process is null)
        {
            return [];
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => ParseRipgrepLine(line, rootPath))
            .Where(result => result is not null)
            .Cast<SearchResult>()
            .Take(50)
            .ToArray();
    }

    private static SearchResult? ParseRipgrepLine(string line, string rootPath)
    {
        var parts = line.Split(':', 3);
        if (parts.Length < 3 || !int.TryParse(parts[1], out var lineNumber))
        {
            return null;
        }

        var filePath = Path.GetRelativePath(rootPath, parts[0]);
        return new SearchResult(filePath, lineNumber, parts[2].Trim(), "ripgrep");
    }

    private static async Task<IReadOnlyList<SearchResult>> SearchWithFallbackAsync(string rootPath, string text, CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();
        foreach (var file in Directory.EnumerateFiles(rootPath, "*.md", SearchOption.AllDirectories))
        {
            var lineNumber = 0;
            foreach (var line in await File.ReadAllLinesAsync(file, cancellationToken))
            {
                lineNumber++;
                if (line.Contains(text, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new SearchResult(Path.GetRelativePath(rootPath, file), lineNumber, line.Trim(), "csharp-fallback"));
                    if (results.Count >= 50)
                    {
                        return results;
                    }
                }
            }
        }

        return results;
    }
}
