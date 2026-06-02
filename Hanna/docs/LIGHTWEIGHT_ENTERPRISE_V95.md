# Hanna Lightweight Enterprise v9.5

## Estado

Arquitectura preparada con núcleo mínimo funcional en `Hanna.Lightweight/`. Las integraciones externas, peligrosas o de red quedan **planificadas, no implementadas** y con `DryRun=true` por defecto.

## Diferencia entre Hanna principal y Hanna.Lightweight

- Hanna principal vive en `Hanna/Hanna.csproj` y conserva sus servicios actuales, Telegram, WebUI, motores, `/motor`, `/fase`, `HANNA_MODE`, MongoDB y MySQL.
- Hanna.Lightweight vive en `Hanna.Lightweight/Hanna.Lightweight.csproj` y es una consola paralela, sin conexión con Hanna principal.
- Hanna.Lightweight usa memoria flat-file local, Markdown y JSONL para minimizar dependencias y facilitar reparación manual.

## Ejecución

```bash
# Hanna principal
dotnet run --project Hanna/Hanna.csproj

# Hanna.Lightweight
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj
```

## Núcleo funcional actual

- Creación de `HannaData/` y subdirectorios.
- `runtime/short_memory.jsonl` como memoria corta.
- Notas Markdown compatibles con Obsidian en `vault/memoria`.
- Caché mínimo de código en `vault/codigo_cache`.
- Búsqueda local con ripgrep o fallback C#.
- Filtro de secretos previo a persistencia.
- Logs locales y auditoría JSONL simulada.

## Módulos planificados

Búnker cifrado, ofuscación GUID, índice cifrado, whitelist IP/MAC, TOTP/2FA, visor en RAM, ingesta ciega por voz, multibóvedas, traducción dinámica bajo demanda, enrutamiento semántico de intenciones, MQTT, voz local, P2P, sinergia de pareja, multi-tenant, RBAC real, auditoría firmada, ClamAV, Docker, Node-RED, Wake-on-LAN, Zero-Leak RAG, failsafe post-corte, NTP, notificación IP pública, NAS indexer, inventario local y Serverless.

## Confirmación humana requerida

Toda acción externa o potencialmente peligrosa requiere una fase futura con confirmación humana explícita: ejecutar scripts, activar Wake-on-LAN, publicar MQTT, cifrar/descifrar bóvedas reales, tocar VPN, desplegar Docker, escanear NAS, invocar ClamAV o controlar IoT.
