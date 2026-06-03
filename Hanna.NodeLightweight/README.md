# Hanna.NodeLightweight

`Hanna.NodeLightweight` es una conversión funcional y progresiva de la Hanna original en C# hacia Node.js/JavaScript para HP Mini 110 con Debian 12 i386/x86. No es un bot nuevo, no es solo una CLI y no reemplaza `Hanna/`, `Hanna.Lightweight/` ni `HannaWebUI/`.

## Objetivo

La HP Mini 110 usa x86/i386 de 32 bits, donde .NET moderno no es viable como runtime principal. Esta versión usa Node.js de Debian, archivos locales y módulos opcionales para conservar el comportamiento central de Hanna: Telegram, conversación natural, comandos slash, memoria, diagnóstico, auditoría, motor/fase y respuestas humanas.

## Ejecutar CLI conversacional

```bash
npm install
npm start
npm run once -- "hola"
npm run once -- "estado"
npm run once -- "diagnostico"
npm run once -- "verifica auditoría"
npm run once -- "qué puedes hacer"
npm run once -- "guarda esto en memoria: la hp mini usa debian i386"
npm run once -- "busca en memoria hp mini"
```

## Modo JSON explícito

Por defecto Hanna responde en texto humano. Para JSON crudo usa:

```bash
npm run once -- "/json /doctor"
```

## Telegram como canal principal

```bash
export TELEGRAM_BOT_TOKEN="token-local-no-subir"
export TELEGRAM_ADMIN_ID="123456" # opcional
npm run telegram
```

Dry-run sin abrir polling real:

```bash
npm run telegram:dry-run
```

Si falta `TELEGRAM_BOT_TOKEN`, Telegram reporta `missing_configuration` y no crashea.

## Variables de entorno principales

- `TELEGRAM_BOT_TOKEN`: token del bot, solo local.
- `TELEGRAM_ADMIN_ID`: restringe comandos sensibles a un usuario.
- `SPOTIFY_CLIENT_ID`, `SPOTIFY_CLIENT_SECRET`, `SPOTIFY_REDIRECT_URI`, `SPOTIFY_REFRESH_TOKEN`: credenciales OAuth de Spotify, solo locales y nunca registradas en logs.
- `HANNA_SPOTIFY_DRY_RUN`: mantiene reproducción Spotify en modo seguro aunque haya credenciales.
- `HANNA_DATA_DIR`: carpeta local de datos, por defecto `../HannaData`.
- `HANNA_VAULT_PASSWORD`: contraseña maestra local para vault cifrado; no se guarda.
- `HANNA_MQTT_BROKER_URL`: broker opcional; sin valor se reporta `missing_configuration`.

## Pruebas

```bash
npm test
npm run self-test
npm run once -- "/doctor"
npm run once -- "/auditoria verificar"
```

## Paridad con Hanna C#

Revisa:

- `docs/PARITY_WITH_ORIGINAL_HANNA.md`
- `docs/ORIGINAL_BEHAVIOR_CHECKLIST.md`

Ahí se documenta qué comportamiento C# ya está portado, qué está parcial y qué queda bloqueado por configuración, dependencia o plataforma i386.

## Spotify

Hanna.NodeLightweight incluye un adapter Spotify ligero con `https` nativo. Usa `/spotify estado`, `/spotify auth estado`, `/spotify buscar TEXTO`, `/spotify reproducir TEXTO`, `/spotify pausar`, `/spotify siguiente` y `/spotify anterior`. En lenguaje natural acepta frases como `estado de spotify`, `pausa spotify` y `siguiente canción`. Si faltan credenciales OAuth reporta `blocked_by_configuration` y mantiene dry-run sin guardar ni loggear secretos.

## Runtime siempre activo en HP Mini

`node src/index.js` queda como chat local opcional. Para operación permanente usa procesos separados:

- `npm run core`: inicia `hanna-core` headless (configuración, memoria, Obsidian, emociones, IA, auditoría y estado).
- `npm run telegram`: inicia `hanna-telegram` por long polling usando `.env` local.
- `npm run web`: inicia la web compacta en `HANNA_WEB_PORT` (8787 por defecto).
- `npm run cli`: abre chat local.
- `npm run all:dry-run`: prueba core, Telegram dry-run y web self-test sin acciones peligrosas.

Para systemd en Debian 12 i386: revisa `systemd/hanna-core.service`, `systemd/hanna-telegram.service`, `systemd/hanna-web.service` y ejecuta `sudo scripts/install-services.sh` después de ajustar `WorkingDirectory` a la ruta real de instalación.

## Web compacta

`npm run web` expone páginas ligeras sin Express: `/`, `/chat`, `/status`, `/doctor`, `/memory`, `/obsidian`, `/emotions`, `/modules`, `/ai`, `/spotify`, `/telegram`, `/logs` y `/settings`. También expone endpoints `/api/health`, `/api/status`, `/api/doctor`, `/api/modules`, `/api/emotions`, `/api/obsidian/status`, `/api/chat`, `/api/command`, `/api/memory/search` y `/api/memory/save`.

## Obsidian/RAG e IA

Configura `HANNA_OBSIDIAN_VAULT_PATH` para usar una bóveda existente; si falta, Hanna usa `HannaData/vault`. Los comandos `/obsidian guardar`, `/obsidian buscar` y `/obsidian indexar` crean notas Markdown con frontmatter, buscan por `rg` si existe y hacen fallback por filesystem. Para preguntas generales (`busca qué es un LLM`, `explícame qué es...`) Hanna consulta primero Obsidian/RAG y luego `LlmRouterService`; si no hay motor configurado reporta las variables faltantes sin inventar respuesta.

## FASE 2: almacenamiento Zero-Bloat

La versión i386 no usa MongoDB nativo. El mapeo de colecciones históricas se documenta en `docs/STORAGE_UNIFICATION_PHASE_2.md`: memorias/contexto/código van a Markdown, conversaciones/mensajes/transcripciones/análisis/acciones van a JSONL y `estado_sistema` vive en `HannaData/runtime/config.json`. `RemoteSyncService` puede enviar respaldos por HTTP/HTTPS con `HANNA_REMOTE_SYNC_URL` sin instalar drivers MongoDB.
