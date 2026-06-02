using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class ModuleRegistryService(RipgrepSearchService ripgrepSearch)
{
    public IReadOnlyList<ModuleStatus> GetModules() =>
    [
        new("flat-file memory", "implemented", false, "JSONL y Markdown locales."),
        new("markdown vault", "implemented", false, "Bóveda Obsidian-compatible."),
        new("ripgrep search", ripgrepSearch.IsRipgrepAvailable ? "implemented" : "fallback", false, "Usa rg cuando existe; si no, búsqueda C# simple."),
        new("code cache", "partial", false, "Notas Markdown, índice JSONL y deduplicación SHA256 mínima."),
        new("rolling summary", "partial", false, "Resumen extractivo local básico, sin IA externa."),
        new("vault index", "partial", false, "Índice JSONL local regenerable."),
        new("PathGuard", "implemented", false, "Bloquea escrituras fuera de HannaData."),
        new("SecretFilter", "partial", false, "Redacta patrones comunes y registra redacciones sin secretos."),
        new("encrypted vault AES-256", "planned_not_implemented", true, "Búnker cifrado futuro; sin cifrado real en esta fase."),
        new("physical GUID obfuscation", "planned_not_implemented", true, "Ofuscación física futura."),
        new("encrypted master index", "planned_not_implemented", true, "Índice maestro cifrado futuro."),
        new("IP/MAC whitelisting", "planned_not_implemented", true, "Sin controles de red activos."),
        new("TOTP/2FA", "planned_not_implemented", true, "Sin autenticación 2FA real."),
        new("RAM viewer", "planned_not_implemented", true, "Sin visor en RAM activo."),
        new("blind voice ingestion", "planned_not_implemented", true, "Sin micrófono ni ingesta ciega."),
        new("isolated multi-vault", "planned_not_implemented", true, "Sin multi-bóvedas reales."),
        new("MQTT real", "planned_not_implemented", true, "Sin conexión a broker ni control IoT real."),
        new("voice local", "planned_not_implemented", true, "Sin TTS/STT local activo."),
        new("walkie-talkie P2P", "planned_not_implemented", true, "Sin red P2P real."),
        new("couple synergy", "planned_not_implemented", true, "Sin módulo relacional activo."),
        new("multi-tenant real", "planned_not_implemented", true, "Sin aislamiento multi-tenant real."),
        new("RBAC real", "planned_not_implemented", true, "Modelos seguros solamente."),
        new("cryptographically signed audit", "planned_not_implemented", true, "Auditoría firmada futura."),
        new("ClamAV real", "planned_not_implemented", true, "Sin invocar antivirus externo."),
        new("Docker staging/production", "planned_not_implemented", true, "Sin despliegue Docker."),
        new("Node-RED real", "planned_not_implemented", true, "Sin flujos activos."),
        new("Wake-on-LAN real", "planned_not_implemented", true, "Sin paquetes mágicos reales."),
        new("Zero-Leak RAG", "planned_not_implemented", true, "Diseño documental, no pipeline activo."),
        new("post-outage failsafe", "planned_not_implemented", true, "Sin control eléctrico real."),
        new("NTP", "planned_not_implemented", true, "Sin sincronización NTP activa."),
        new("public IP notification", "planned_not_implemented", true, "Sin llamadas externas de IP pública."),
        new("NAS indexer real", "planned_not_implemented", true, "Sin escaneo NAS real."),
        new("serverless", "planned_not_implemented", true, "Sin funciones remotas."),
        new("dynamic translation", "planned_not_implemented", true, "Sin traducción real todavía."),
        new("semantic intent routing", "planned_not_implemented", true, "Sin router semántico real todavía.")
    ];
}
