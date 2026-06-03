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
