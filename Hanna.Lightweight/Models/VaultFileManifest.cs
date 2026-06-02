namespace Hanna.Lightweight.Models;

public sealed record VaultFileManifest(string RelativePath, long SizeBytes, DateTimeOffset UpdatedUtc, string ContentType);
