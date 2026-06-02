using System.Diagnostics;
using Hanna.Lightweight.Core;
using Hanna.Lightweight.Models;

namespace Hanna.Lightweight.Services;

public sealed class RipgrepSearchService(SecretFilterService secretFilter, LightweightOptions options, PathGuardService pathGuard)
{
    public bool IsRipgrepAvailable { get; } = CheckRipgrep();

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string rootPath, string text, CancellationToken cancellationToken = default)
    {
        var safeRoot = pathGuard.EnsureInsideRoot(rootPath);
        var safeText = secretFilter.Filter(text);
        if (string.IsNullOrWhiteSpace(safeText) || !Directory.Exists(safeRoot))
        {
            return [];
        }

        return IsRipgrepAvailable
            ? await SearchWithRipgrepAsync(safeRoot, safeText, cancellationToken)
            : await SearchWithFallbackAsync(safeRoot, safeText, cancellationToken);
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

    private async Task<IReadOnlyList<SearchResult>> SearchWithRipgrepAsync(string rootPath, string text, CancellationToken cancellationToken)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "rg",
            ArgumentList = { "--line-number", "--fixed-strings", "--ignore-case", "--max-filesize", options.MaxSearchFileBytes.ToString(), text, rootPath },
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
            .Take(options.MaxSearchResults)
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

    private async Task<IReadOnlyList<SearchResult>> SearchWithFallbackAsync(string rootPath, string text, CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();
        foreach (var file in Directory.EnumerateFiles(rootPath, "*.md", SearchOption.AllDirectories))
        {
            var safeFile = pathGuard.EnsureInsideRoot(file);
            var info = new FileInfo(safeFile);
            if (info.Length > options.MaxSearchFileBytes)
            {
                continue;
            }

            var lineNumber = 0;
            foreach (var line in await File.ReadAllLinesAsync(safeFile, cancellationToken))
            {
                lineNumber++;
                if (line.Contains(text, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new SearchResult(Path.GetRelativePath(rootPath, safeFile), lineNumber, line.Trim(), "csharp-fallback"));
                    if (results.Count >= options.MaxSearchResults)
                    {
                        return results;
                    }
                }
            }
        }

        return results;
    }
}
