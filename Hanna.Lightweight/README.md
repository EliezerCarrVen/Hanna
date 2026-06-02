# Hanna.Lightweight

Hanna.Lightweight es un proyecto paralelo a Hanna principal. No reemplaza `Hanna/Hanna.csproj`, no modifica `Hanna/Program.cs`, no toca Telegram, WebUI, `/motor`, `/fase`, `HANNA_MODE`, MongoDB ni MySQL.

## Objetivo

Crear una Hanna Lite para hardware limitado como HP Mini 110 con CPU Intel Atom y disco de 250 GB. La prioridad es bajo consumo, costo mínimo, memoria flat-file, Markdown/Obsidian, JSONL, ripgrep, seguridad local y una arquitectura preparada para futuras fases Master/Worker.

## Cómo ejecutar

Hanna principal:

```bash
dotnet run --project Hanna/Hanna.csproj
```

Hanna.Lightweight:

```bash
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj
```

## Qué funciona realmente

- Creación automática de `HannaData/`.
- Memoria corta en `runtime/short_memory.jsonl`.
- Notas Markdown en `vault/memoria`.
- Caché mínimo de código en `vault/codigo_cache`.
- Búsqueda local con `rg` si existe, con fallback C# si no existe.
- Filtro de secretos antes de persistir memoria.
- Logs locales en `logs/lightweight.log`.
- Auditoría simulada en `logs/audit.log`.

## Comandos

- `/status`: muestra modo, rutas, módulos y estado de ripgrep.
- `/memoria prueba`: escribe una entrada JSONL y una nota Markdown.
- `/memoria buscar TEXTO`: busca texto dentro de `HannaData/vault`.
- `/codigo prueba`: crea una nota de caché de código de prueba.
- `/codigo buscar TEXTO`: busca en `vault/codigo_cache`.
- `/modulos`: lista módulos implementados y planificados.
- `/auditoria`: muestra últimos eventos de auditoría.
- `/salir`: cierra la consola.

## Estructura HannaData

```text
HannaData/
  vault/
    memoria/
    proyectos/
    sistema/
    inventario/
    tareas/
    codigo_cache/
    bovedas/
    perfiles/
    empresa/
  runtime/
    short_memory.jsonl
    current_session.jsonl
    last_summary.md
  indexes/
    file_index.jsonl
    vault_index.jsonl
    code_cache_index.jsonl
  logs/
    lightweight.log
    security.log
    audit.log
```

## Datos que nunca deben persistirse

No se deben guardar tokens, API keys, contraseñas, prompts internos, system prompts, `HannaEnv` ni configuraciones sensibles. El filtro redacta términos como `TELEGRAM_TOKEN`, `GEMINI_API_KEY`, `GROQ_API_KEY`, `OPENROUTER_API_KEY`, `SPOTIFY_CLIENT_SECRET`, `MYSQL_PASSWORD`, `HANNA_JWT_SECRET` y `HANNA_MOBILE_API_PAIRING_TOKEN`.

## Planificado, no implementado

Los módulos peligrosos quedan documentados y en `DryRun=true`: búnker cifrado AES-256, MQTT real, Node-RED, Master/Worker real, NAS indexer real, RBAC real, ClamAV, Wake-on-LAN, Serverless, Zero-Leak RAG, NTP, notificación de IP pública, Docker staging/production, voz local y walkie-talkie P2P.
