# Plano Maestro Hanna.Lightweight

## Estado

Hanna.Lightweight es un proyecto paralelo y preparado para Enterprise v9.5. El núcleo mínimo funcional vive en `Hanna.Lightweight/`; las capacidades externas quedan **planificadas, no implementadas**.

## Separación absoluta

- Hanna principal: `Hanna/Hanna.csproj`.
- Hanna.Lightweight: `Hanna.Lightweight/Hanna.Lightweight.csproj`.
- Esta fase no reemplaza Hanna principal, no modifica `Hanna/Program.cs`, no toca Telegram, WebUI, `/motor`, `/fase`, `HANNA_MODE`, MongoDB ni MySQL.
- No hay conexión runtime entre ambos proyectos.

## Ejecución

```bash
dotnet run --project Hanna/Hanna.csproj
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj
```

## Objetivo de hardware

Optimizar para equipos modestos como HP Mini 110 con CPU Intel Atom y disco de 250 GB: bajo consumo, pocas dependencias, archivos reparables manualmente y operación local.

## Arquitectura preparada

- Memoria flat-file.
- Obsidian/Markdown como bóveda humana.
- JSONL como memoria corta.
- Rolling summary en `last_summary.md`.
- Búsqueda con ripgrep y fallback C#.
- Caché semántico de código inicial.
- Auditoría local JSONL.
- Seguridad local con filtro de secretos.
- Master/Worker, MQTT, NAS, cifrado, RBAC, Serverless y demás módulos documentados sin implementación real.

## Datos prohibidos

Nunca persistir tokens, API keys, contraseñas, prompts internos, system prompts, `HannaEnv` ni configuraciones sensibles.

## Módulos peligrosos

Búnker cifrado real, MQTT real, Node-RED, Wake-on-LAN, NAS, Docker, ClamAV, VPN, IoT y Serverless requieren confirmación humana y PR futura. Todo permanece en `DryRun=true`.
