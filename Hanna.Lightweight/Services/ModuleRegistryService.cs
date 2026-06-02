using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class ModuleRegistryService(RipgrepSearchService ripgrepSearch)
{
    public IReadOnlyList<ModuleStatus> GetModules() =>
    [
        new("flat-file memory", "implemented", false, "JSONL y Markdown locales."),
        new("markdown vault", "implemented", false, "Bóveda Obsidian-compatible."),
        new("ripgrep search", ripgrepSearch.IsRipgrepAvailable ? "implemented" : "fallback", false, "Usa rg cuando existe; si no, búsqueda C# simple."),
        new("code cache", "minimal implemented", false, "Notas Markdown e índice JSONL mínimo."),
        new("encrypted vault", "planned_not_implemented", true, "Búnker AES-256 futuro; sin cifrado real en esta fase."),
        new("MQTT", "planned_not_implemented", true, "Sin conexión a broker ni control IoT real."),
        new("Node-RED", "planned_not_implemented", true, "Sin despliegue ni flujos activos."),
        new("Master/Worker", "planned_not_implemented", true, "Sin enlace con Hanna principal ni workers."),
        new("NAS indexer", "planned_not_implemented", true, "Sin escaneo NAS real."),
        new("RBAC", "planned_not_implemented", true, "Modelos seguros solamente."),
        new("ClamAV", "planned_not_implemented", true, "Sin invocar antivirus externo."),
        new("Wake-on-LAN", "planned_not_implemented", true, "Sin paquetes mágicos reales."),
        new("serverless", "planned_not_implemented", true, "Sin funciones remotas."),
        new("voice local", "planned_not_implemented", true, "Sin micrófono ni TTS en esta fase."),
        new("walkie-talkie P2P", "planned_not_implemented", true, "Sin red P2P real."),
        new("Zero-Leak RAG", "planned_not_implemented", true, "Diseño documental, no pipeline activo.")
    ];
}
