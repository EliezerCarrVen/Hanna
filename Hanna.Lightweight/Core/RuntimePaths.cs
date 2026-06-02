namespace Hanna.Lightweight.Core;

public sealed class RuntimePaths
{
    public RuntimePaths(string dataRoot)
    {
        DataRoot = Path.GetFullPath(dataRoot);
        Vault = Path.Combine(DataRoot, "vault");
        VaultMemoria = Path.Combine(Vault, "memoria");
        VaultProyectos = Path.Combine(Vault, "proyectos");
        VaultSistema = Path.Combine(Vault, "sistema");
        VaultInventario = Path.Combine(Vault, "inventario");
        VaultTareas = Path.Combine(Vault, "tareas");
        VaultCodigoCache = Path.Combine(Vault, "codigo_cache");
        VaultBovedas = Path.Combine(Vault, "bovedas");
        VaultPerfiles = Path.Combine(Vault, "perfiles");
        VaultEmpresa = Path.Combine(Vault, "empresa");
        Runtime = Path.Combine(DataRoot, "runtime");
        ShortMemory = Path.Combine(Runtime, "short_memory.jsonl");
        CurrentSession = Path.Combine(Runtime, "current_session.jsonl");
        LastSummary = Path.Combine(Runtime, "last_summary.md");
        Indexes = Path.Combine(DataRoot, "indexes");
        FileIndex = Path.Combine(Indexes, "file_index.jsonl");
        VaultIndex = Path.Combine(Indexes, "vault_index.jsonl");
        CodeCacheIndex = Path.Combine(Indexes, "code_cache_index.jsonl");
        Logs = Path.Combine(DataRoot, "logs");
        LightweightLog = Path.Combine(Logs, "lightweight.log");
        SecurityLog = Path.Combine(Logs, "security.log");
        AuditLog = Path.Combine(Logs, "audit.log");
    }

    public string DataRoot { get; }
    public string Vault { get; }
    public string VaultMemoria { get; }
    public string VaultProyectos { get; }
    public string VaultSistema { get; }
    public string VaultInventario { get; }
    public string VaultTareas { get; }
    public string VaultCodigoCache { get; }
    public string VaultBovedas { get; }
    public string VaultPerfiles { get; }
    public string VaultEmpresa { get; }
    public string Runtime { get; }
    public string ShortMemory { get; }
    public string CurrentSession { get; }
    public string LastSummary { get; }
    public string Indexes { get; }
    public string FileIndex { get; }
    public string VaultIndex { get; }
    public string CodeCacheIndex { get; }
    public string Logs { get; }
    public string LightweightLog { get; }
    public string SecurityLog { get; }
    public string AuditLog { get; }

    public IEnumerable<string> Directories =>
    [
        DataRoot, Vault, VaultMemoria, VaultProyectos, VaultSistema, VaultInventario,
        VaultTareas, VaultCodigoCache, VaultBovedas, VaultPerfiles, VaultEmpresa,
        Runtime, Indexes, Logs
    ];

    public IEnumerable<string> Files =>
    [
        ShortMemory, CurrentSession, LastSummary, FileIndex, VaultIndex, CodeCacheIndex,
        LightweightLog, SecurityLog, AuditLog
    ];
}
