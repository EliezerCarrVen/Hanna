using System.Text.Json;

namespace Hanna.Lightweight.Core;

public sealed record LightweightOptions
{
    public string Mode { get; init; } = "lightweight";
    public string MemoryMode { get; init; } = "flat-file";
    public string DataRoot { get; init; } = Path.Combine(Environment.CurrentDirectory, "HannaData");
    public bool DryRun { get; init; } = true;
    public bool DangerousModulesDryRun { get; init; } = true;
    public bool RequireConfirmation { get; init; } = true;
    public string[] AllowedNasRoots { get; init; } = [];
    public string[] AllowedVaultImportRoots { get; init; } = [];
    public string? MqttBroker { get; init; }
    public int MqttPort { get; init; } = 1883;
    public string? MqttUsername { get; init; }
    public bool MqttUseTls { get; init; }
    public bool DockerEnabled { get; init; }
    public bool ClamAvEnabled { get; init; }
    public string? NodeRedBaseUrl { get; init; }
    public string? ServerlessWebhookUrl { get; init; }
    public string WolBroadcastAddress { get; init; } = "255.255.255.255";
    public bool TailscaleExpected { get; init; }
    public string? NtpExpectedServer { get; init; }
    public bool PublicIpCheckEnabled { get; init; }
    public int MaxRotatedLogs { get; init; } = 10;
    public int LogRetentionDays { get; init; } = 30;
    public int LastEntriesToRead { get; init; } = 10;
    public int MaxJsonlEntriesToRead { get; init; } = 50;
    public int MaxMemoryEntryLength { get; init; } = 4000;
    public int MaxMarkdownNoteLength { get; init; } = 12000;
    public long MaxLogFileBytes { get; init; } = 1_048_576;
    public int MaxSearchResults { get; init; } = 20;
    public long MaxSearchFileBytes { get; init; } = 524_288;
    public int MaxAuditEventsToRead { get; init; } = 30;
    public int MaxCommandLength { get; init; } = 2000;

    public static LightweightOptions CreateDefault()
    {
        var options = new LightweightOptions();
        options = ApplyJson(options, Path.Combine(Environment.CurrentDirectory, "Hanna.Lightweight", "appsettings.example.json"), required: false);
        options = ApplyJson(options, Path.Combine(Environment.CurrentDirectory, "Hanna.Lightweight", "appsettings.local.json"), required: false);
        return ApplyEnvironment(options);
    }

    public string TruncateMemory(string value) => Truncate(value, MaxMemoryEntryLength);

    public string TruncateMarkdown(string value) => Truncate(value, MaxMarkdownNoteLength);

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        return value[..maxLength] + "\n[TRUNCATED_BY_HANNA_LIGHTWEIGHT_LIMIT]";
    }

    private static LightweightOptions ApplyJson(LightweightOptions current, string path, bool required)
    {
        if (!File.Exists(path))
        {
            return current;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("Lightweight", out var json))
        {
            return current;
        }

        string? S(string name) => json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        bool B(string name, bool old) => json.TryGetProperty(name, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : old;
        int I(string name, int old) => json.TryGetProperty(name, out var v) && v.TryGetInt32(out var n) ? n : old;
        long L(string name, long old) => json.TryGetProperty(name, out var v) && v.TryGetInt64(out var n) ? n : old;
        string[] A(string name, string[] old) => json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToArray() : old;

        return current with
        {
            Mode = S(nameof(Mode)) ?? current.Mode,
            MemoryMode = S(nameof(MemoryMode)) ?? current.MemoryMode,
            DataRoot = S(nameof(DataRoot)) ?? current.DataRoot,
            DryRun = B(nameof(DryRun), current.DryRun),
            DangerousModulesDryRun = B(nameof(DangerousModulesDryRun), current.DangerousModulesDryRun),
            RequireConfirmation = B(nameof(RequireConfirmation), current.RequireConfirmation),
            AllowedNasRoots = A(nameof(AllowedNasRoots), current.AllowedNasRoots),
            AllowedVaultImportRoots = A(nameof(AllowedVaultImportRoots), current.AllowedVaultImportRoots),
            MqttBroker = S(nameof(MqttBroker)) ?? current.MqttBroker,
            MqttPort = I(nameof(MqttPort), current.MqttPort),
            MqttUsername = S(nameof(MqttUsername)) ?? current.MqttUsername,
            MqttUseTls = B(nameof(MqttUseTls), current.MqttUseTls),
            DockerEnabled = B(nameof(DockerEnabled), current.DockerEnabled),
            ClamAvEnabled = B(nameof(ClamAvEnabled), current.ClamAvEnabled),
            NodeRedBaseUrl = S(nameof(NodeRedBaseUrl)) ?? current.NodeRedBaseUrl,
            ServerlessWebhookUrl = S(nameof(ServerlessWebhookUrl)) ?? current.ServerlessWebhookUrl,
            WolBroadcastAddress = S(nameof(WolBroadcastAddress)) ?? current.WolBroadcastAddress,
            TailscaleExpected = B(nameof(TailscaleExpected), current.TailscaleExpected),
            NtpExpectedServer = S(nameof(NtpExpectedServer)) ?? current.NtpExpectedServer,
            PublicIpCheckEnabled = B(nameof(PublicIpCheckEnabled), current.PublicIpCheckEnabled),
            MaxRotatedLogs = I(nameof(MaxRotatedLogs), current.MaxRotatedLogs),
            LogRetentionDays = I(nameof(LogRetentionDays), current.LogRetentionDays),
            MaxJsonlEntriesToRead = I(nameof(MaxJsonlEntriesToRead), current.MaxJsonlEntriesToRead),
            MaxMemoryEntryLength = I(nameof(MaxMemoryEntryLength), current.MaxMemoryEntryLength),
            MaxMarkdownNoteLength = I(nameof(MaxMarkdownNoteLength), current.MaxMarkdownNoteLength),
            MaxLogFileBytes = L(nameof(MaxLogFileBytes), current.MaxLogFileBytes),
            MaxSearchResults = I(nameof(MaxSearchResults), current.MaxSearchResults),
            MaxSearchFileBytes = L(nameof(MaxSearchFileBytes), current.MaxSearchFileBytes),
            MaxAuditEventsToRead = I(nameof(MaxAuditEventsToRead), current.MaxAuditEventsToRead),
            MaxCommandLength = I(nameof(MaxCommandLength), current.MaxCommandLength)
        };
    }

    private static LightweightOptions ApplyEnvironment(LightweightOptions current)
    {
        string? E(string name) => Environment.GetEnvironmentVariable("HANNA_LIGHTWEIGHT_" + name.ToUpperInvariant());
        bool EB(string name, bool old) => bool.TryParse(E(name), out var value) ? value : old;
        int EI(string name, int old) => int.TryParse(E(name), out var value) ? value : old;
        return current with
        {
            DataRoot = E(nameof(DataRoot)) ?? current.DataRoot,
            DryRun = EB(nameof(DryRun), current.DryRun),
            RequireConfirmation = EB(nameof(RequireConfirmation), current.RequireConfirmation),
            MqttBroker = E(nameof(MqttBroker)) ?? current.MqttBroker,
            MqttPort = EI(nameof(MqttPort), current.MqttPort),
            MqttUsername = E(nameof(MqttUsername)) ?? current.MqttUsername,
            MqttUseTls = EB(nameof(MqttUseTls), current.MqttUseTls),
            DockerEnabled = EB(nameof(DockerEnabled), current.DockerEnabled),
            ClamAvEnabled = EB(nameof(ClamAvEnabled), current.ClamAvEnabled),
            NodeRedBaseUrl = E(nameof(NodeRedBaseUrl)) ?? current.NodeRedBaseUrl,
            ServerlessWebhookUrl = E(nameof(ServerlessWebhookUrl)) ?? current.ServerlessWebhookUrl,
            WolBroadcastAddress = E(nameof(WolBroadcastAddress)) ?? current.WolBroadcastAddress,
            PublicIpCheckEnabled = EB(nameof(PublicIpCheckEnabled), current.PublicIpCheckEnabled)
        };
    }
}
