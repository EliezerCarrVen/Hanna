# Módulos Hanna.Lightweight

## Funcionales

| Módulo | Estado | Alcance |
| --- | --- | --- |
| flat-file memory | implemented | JSONL y Markdown locales. |
| markdown vault | implemented | Bóveda compatible con Obsidian. |
| ripgrep search | implemented/fallback | `rg` si existe; fallback C# con límites de tamaño. |
| PathGuard | implemented | Bloqueo de escrituras fuera de `HannaData/`. |
| log rotation | implemented | Rotación por tamaño para logs locales. |
| doctor/self-test | implemented | Validaciones locales PASS/WARN/FAIL. |

## Parciales seguros

| Módulo | Estado | Alcance |
| --- | --- | --- |
| code cache | partial | Markdown, frontmatter, índice JSONL y deduplicación SHA256. |
| rolling summary | partial | Resumen extractivo local, no IA. |
| vault index | partial | Índice JSONL local regenerable. |
| SecretFilter | partial | Redacción por patrones; ampliar con tests futuros. |

## Planificados, no implementados

| Módulo | Estado | DryRun |
| --- | --- | --- |
| Búnker cifrado AES-256 | planned_not_implemented | true |
| Ofuscación física por GUID | planned_not_implemented | true |
| Índice maestro cifrado | planned_not_implemented | true |
| IP/MAC whitelisting | planned_not_implemented | true |
| TOTP/2FA | planned_not_implemented | true |
| Visor en RAM | planned_not_implemented | true |
| Ingesta ciega por voz | planned_not_implemented | true |
| Multi-bóvedas aisladas | planned_not_implemented | true |
| MQTT real | planned_not_implemented | true |
| Voz local | planned_not_implemented | true |
| Walkie-talkie P2P | planned_not_implemented | true |
| Sinergia de pareja | planned_not_implemented | true |
| Multi-tenant real | planned_not_implemented | true |
| RBAC real | planned_not_implemented | true |
| Auditoría firmada criptográficamente | planned_not_implemented | true |
| ClamAV real | planned_not_implemented | true |
| Docker staging/production | planned_not_implemented | true |
| Node-RED real | planned_not_implemented | true |
| Wake-on-LAN real | planned_not_implemented | true |
| Zero-Leak RAG | planned_not_implemented | true |
| Failsafe post-corte | planned_not_implemented | true |
| NTP | planned_not_implemented | true |
| Notificación IP pública | planned_not_implemented | true |
| NAS indexer real | planned_not_implemented | true |
| Serverless | planned_not_implemented | true |
| Traducción dinámica real | planned_not_implemented | true |
| Enrutamiento semántico real | planned_not_implemented | true |

## Regla de extensión

No reimplementar módulos funcionales; extenderlos mediante PRs pequeñas, con self-test y doctor actualizados.
