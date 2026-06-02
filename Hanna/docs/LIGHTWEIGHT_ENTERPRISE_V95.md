# Hanna Lightweight Enterprise v9.5

## Estado

Arquitectura preparada con núcleo funcional en `Hanna.Lightweight/`. Las integraciones externas, peligrosas o de red quedan **planificadas, no implementadas** y con `DryRun=true` por defecto.

## Diferencia entre Hanna principal y Hanna.Lightweight

- Hanna principal vive en `Hanna/Hanna.csproj` y conserva Telegram, WebUI, motores, `/motor`, `/fase`, `HANNA_MODE`, MongoDB y MySQL.
- Hanna.Lightweight vive en `Hanna.Lightweight/Hanna.Lightweight.csproj` y es una consola paralela, sin conexión con Hanna principal.
- Hanna.Lightweight usa memoria flat-file local, Markdown y JSONL para minimizar dependencias.

## Ejecución

```bash
# Hanna principal
dotnet run --project Hanna/Hanna.csproj

# Hanna.Lightweight interactivo
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj

# Self-test
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj -- --self-test

# Un comando y salir
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj -- --once "/doctor"
```

## Núcleo funcional actual

- Creación de `HannaData/` y subdirectorios.
- `runtime/short_memory.jsonl` como memoria corta.
- Notas Markdown compatibles con Obsidian.
- Caché de código con deduplicación SHA256.
- Búsqueda local con ripgrep o fallback C#.
- PathGuard, SecretFilter, log rotation, doctor y self-test.
- Rolling summary local básico.
- Índice de vault JSONL.
- Logs locales y auditoría JSONL simulada.

## Módulos planificados

Búnker cifrado, ofuscación GUID, índice cifrado, whitelist IP/MAC, TOTP/2FA, visor en RAM, ingesta ciega por voz, multibóvedas, traducción dinámica real, enrutamiento semántico real, MQTT, voz local, P2P, sinergia de pareja, multi-tenant, RBAC real, auditoría firmada, ClamAV, Docker, Node-RED, Wake-on-LAN, Zero-Leak RAG, failsafe post-corte, NTP, notificación IP pública, NAS indexer, inventario local avanzado y Serverless.

## Confirmación humana requerida

Toda acción externa o potencialmente peligrosa requiere una fase futura con confirmación humana explícita: ejecutar scripts, activar Wake-on-LAN, publicar MQTT, cifrar/descifrar bóvedas reales, tocar VPN, desplegar Docker, escanear NAS, invocar ClamAV o controlar IoT.
