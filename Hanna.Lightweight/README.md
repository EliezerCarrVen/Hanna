# Hanna.Lightweight

Hanna.Lightweight es un proyecto paralelo a Hanna principal. No reemplaza `Hanna/Hanna.csproj`, no modifica `Hanna/Program.cs`, no toca Telegram, WebUI, `/motor`, `/fase`, `HANNA_MODE`, MongoDB ni MySQL.

## Qué ya funciona

- Arranque de consola ligera con modo `lightweight` y memoria `flat-file`.
- Creación automática de `HannaData/`.
- Memoria corta JSONL en `runtime/short_memory.jsonl`.
- Vault Markdown compatible con Obsidian en `vault/`.
- Búsqueda local con `rg` o fallback C#.
- Caché de código mínimo con notas Markdown, YAML frontmatter, índice JSONL y deduplicación SHA256.
- Filtro de secretos con registro seguro de redacciones en `logs/security.log`.
- Rotación local de logs cuando superan el límite configurado.
- Auditoría local JSONL en `logs/audit.log`.
- Doctor, self-test, rolling summary local e índice simple de vault.

## Qué no se debe reimplementar

No reconstruir desde cero el arranque, la estructura `HannaData/`, JSONL, Markdown vault, búsqueda `rg`/fallback, caché mínimo, filtro básico, logs, auditoría ni comandos iniciales. Solo deben extenderse con cambios pequeños y seguros.

## Cómo ejecutar

Hanna principal:

```bash
dotnet run --project Hanna/Hanna.csproj
```

Hanna.Lightweight interactivo:

```bash
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj
```

Self-test sin sesión interactiva:

```bash
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj -- --self-test
```

Ejecutar un comando y salir:

```bash
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj -- --once "/status"
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj -- --once "/doctor"
```

## Comandos

- `/help`: lista comandos disponibles.
- `/status`: estado, rutas, conteos, módulos y salud global.
- `/doctor`: revisa estructura, permisos, logs, configuración, ripgrep, módulos y `.gitignore`.
- `/selftest`: ejecuta el mismo flujo que `--self-test`.
- `/memoria prueba`: escribe entrada JSONL y nota Markdown.
- `/memoria buscar TEXTO`: busca dentro de `HannaData/vault`.
- `/codigo prueba`: crea entrada segura de caché de código.
- `/codigo buscar TEXTO`: busca dentro de `vault/codigo_cache`.
- `/codigo listar`: muestra entradas recientes del índice de caché.
- `/codigo estado`: muestra estado del caché de código.
- `/summary` y `/summary regenerar`: crean `runtime/last_summary.md` con resumen extractivo local, no IA.
- `/indexar`: regenera `indexes/vault_index.jsonl`.
- `/indice estado`: muestra estado del índice del vault.
- `/modulos`: lista módulos implementados, parciales y planificados.
- `/auditoria`: muestra últimos eventos de auditoría.
- `/salir`: cierra la consola.

## PathGuard

`PathGuardService` impide escrituras fuera de `HannaData/`, bloquea rutas con `..`, rutas vacías, `.env`, `HannaEnv`, appsettings sensibles y configuraciones con secretos. Los intentos bloqueados se registran sin guardar la ruta original completa.

## SecretFilter

`SecretFilterService` redacta patrones como `api_key=`, `apikey=`, `token=`, `bearer`, `password=`, `pwd=`, `secret=`, `client_secret=`, `refresh_token=`, tokens conocidos de Hanna, cadenas de conexión MySQL/Postgres con password, URLs con credenciales, JWT compactos y prefijos `sk-or-v1`, `gsk_` y `AIza`. El reemplazo siempre es `[REDACTED]`.

## Log rotation

`LogRotationService` rota `lightweight.log`, `audit.log` y `security.log` cuando superan `MaxLogFileBytes`. El archivo se renombra como `nombre.yyyyMMddHHmmss.log`. No borra logs todavía.

## Rolling summary local

`/summary` lee las últimas entradas de `short_memory.jsonl`, calcula temas por palabras frecuentes, lista últimas acciones, agrega advertencias de redacción y escribe `runtime/last_summary.md`. Es extractivo local, sin LLM ni servicios externos.

## Vault index

`/indexar` recorre `HannaData/vault/`, ignora archivos mayores a `MaxSearchFileBytes`, calcula SHA256 y guarda ruta relativa, nombre, extensión, tamaño, fecha y tags de frontmatter en `indexes/vault_index.jsonl`.

## HannaData no se sube a GitHub

`HannaData/` contiene memoria local, logs, auditoría, índices y runtime. Debe permanecer fuera del repositorio y está en `.gitignore` para evitar subir datos privados o sensibles.

## Planificado, no implementado

Siguen en `planned_not_implemented` o `DryRun=true`: búnker cifrado AES-256, ofuscación por GUID, índice maestro cifrado, IP/MAC whitelisting, TOTP/2FA, visor en RAM, ingesta ciega por voz, multi-bóvedas, MQTT real, voz local, walkie-talkie P2P, multi-tenant real, RBAC real, auditoría firmada, ClamAV, Docker, Node-RED, Wake-on-LAN, Zero-Leak RAG, failsafe, NTP, notificación IP pública, NAS indexer real, Serverless, traducción dinámica y enrutamiento semántico real.
