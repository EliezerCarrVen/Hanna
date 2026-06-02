using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class ModuleRegistryService(RipgrepSearchService ripgrepSearch, DependencyCheckerService deps, LightweightOptions options)
{
    public IReadOnlyList<ModuleStatus> GetModules() =>
    [
        new("flat-file memory", "implemented", false, "JSONL y Markdown locales."),
        new("markdown vault", "implemented", false, "Bóveda Obsidian-compatible."),
        new("ripgrep search", ripgrepSearch.IsRipgrepAvailable ? "implemented" : "missing_dependency", false, ripgrepSearch.IsRipgrepAvailable ? "rg disponible." : "rg no disponible; fallback C# activo."),
        new("code cache", "partial", false, "Notas Markdown, índice JSONL y deduplicación SHA256 mínima."),
        new("rolling summary", "partial", false, "Resumen extractivo local básico, sin IA externa."),
        new("vault index", "partial", false, "Índice JSONL local regenerable."),
        new("PathGuard", "implemented", false, "Bloquea escrituras fuera de HannaData."),
        new("SecretFilter", "partial", false, "Redacta patrones comunes y registra redacciones sin secretos."),
        new("dependency checker", "implemented", false, "Detecta dependencias portables sin instalar."),
        new("encrypted vault AES-256", "partial", options.DryRun, "AES-GCM local, PBKDF2, GUID físico y manifest mínimo."),
        new("physical GUID obfuscation", "implemented", false, "GUID físico por archivo del búnker."),
        new("encrypted master index", "partial", options.DryRun, "Manifest mínimo; cifrado avanzado de manifest pendiente."),
        new("IP/MAC whitelisting", "implemented", false, "Política interna local; no modifica firewall."),
        new("TOTP/2FA", "missing_configuration", false, "Código TOTP local listo; requiere secreto configurado."),
        new("RAM viewer", "dry_run", true, "Control plane listo; no abre visor real sin confirmación."),
        new("blind voice ingestion", "missing_configuration", true, "Control plane listo; fuente local no configurada."),
        new("isolated multi-vault", "partial", options.DryRun, "Bóvedas locales por GUID."),
        new("MQTT real", string.IsNullOrWhiteSpace(options.MqttBroker) ? "missing_configuration" : deps.IsFound("mosquitto") ? "dry_run" : "missing_dependency", true, "Broker/config requerido; publicación real bloqueada por DryRun."),
        new("voice local", deps.IsFound("node") ? "missing_configuration" : "missing_dependency", true, "Control plane listo; falta motor STT/TTS."),
        new("walkie-talkie P2P", "missing_hardware_or_network", true, "Control plane listo; falta red P2P/móvil."),
        new("couple synergy", "planned_only", true, "Requiere diseño de producto y consentimiento explícito."),
        new("multi-tenant real", "implemented", false, "Tenant local flat-file."),
        new("RBAC real", "implemented", false, "Roles locales y permisos para comandos sensibles."),
        new("cryptographically signed audit", "implemented", false, "Hash-chain local; firma externa pendiente si se requiere PKI."),
        new("ClamAV real", deps.IsFound("clamscan") ? (options.ClamAvEnabled ? "implemented" : "disabled_by_config") : "missing_dependency", true, "clamscan opcional."),
        new("Docker staging/production", deps.IsFound("docker") ? (options.DockerEnabled ? "dry_run" : "disabled_by_config") : "missing_dependency", true, "Docker real exige confirmación y HANNA_ALLOW_DEPLOY."),
        new("Node-RED real", string.IsNullOrWhiteSpace(options.NodeRedBaseUrl) ? "missing_configuration" : deps.IsFound("node-red") ? "partial" : "missing_dependency", true, "Connector HTTP/control plane."),
        new("Wake-on-LAN real", "dry_run", true, "Magic packet implementado; DryRun=true por defecto."),
        new("Zero-Leak RAG", "implemented", false, "Sanitizador local sin LLM."),
        new("post-outage failsafe", "missing_hardware_or_network", true, "Diagnóstico; BIOS no modificable por software."),
        new("NTP", string.IsNullOrWhiteSpace(options.NtpExpectedServer) ? "missing_configuration" : "partial", false, "Diagnóstico NTP local."),
        new("public IP notification", options.PublicIpCheckEnabled ? "partial" : "disabled_by_config", true, "No consulta IP pública salvo config explícita."),
        new("NAS indexer real", options.AllowedNasRoots.Length == 0 ? "missing_configuration" : "partial", false, "Indexer allowlist sin copiar archivos."),
        new("serverless", string.IsNullOrWhiteSpace(options.ServerlessWebhookUrl) ? "missing_configuration" : "dry_run", true, "Webhook connector; POST real exige DryRun=false."),
        new("dynamic translation", "partial", false, "Planificador local waiting_external_llm, no finge traducción."),
        new("semantic intent routing", "implemented", false, "Clasificador local por reglas, sin LLM.")
    ];
}
