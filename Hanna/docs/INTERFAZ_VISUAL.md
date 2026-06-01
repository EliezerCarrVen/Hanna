# Interfaz visual Hanna WebUI

HannaWebUI es una interfaz compacta centrada en chat. No reemplaza el backend C#/.NET, no elimina el Admin Web actual y puede ejecutarse aunque la Mobile API no esté activa.

## Enfoque actual

- Chat online como área principal.
- Subida/selección de archivos preparada, sin prometer envío real cuando falta endpoint estable.
- Menú `Admin` compacto para ocultar opciones de administración.
- Estado mínimo visible: perfil, motor, fase, Telegram y backend.
- Estilo morado oscuro con acentos turquesa mínimos.
- Consola segura pequeña con los últimos eventos locales.

Se retiró el enfoque de panel visual cargado: no hay tarjetas grandes de demo, barras decorativas ni secciones abiertas que distraigan del chat.

## Ejecutar en modo demo

Modo recomendado para desarrollo visual cuando Hanna no está arrancada:

```powershell
scripts\Run-Hanna-WebUI.ps1
```

El script define `VITE_HANNA_DEMO_MODE=true`. En este modo la UI no llama a `/api/status`, evita errores `ECONNREFUSED` y muestra estado `demo`.

También puede ejecutarse manualmente:

```powershell
cd HannaWebUI
$env:VITE_HANNA_DEMO_MODE="true"
npm install
npm run dev
```

## Conectar con backend real

Cuando Hanna esté corriendo con Mobile API/Admin API disponible:

```powershell
scripts\Run-Hanna-WebUI.ps1 -Backend -ApiBaseUrl http://127.0.0.1:8790
```

Variables soportadas:

```text
VITE_HANNA_API_BASE_URL=http://127.0.0.1:8790
VITE_HANNA_DEMO_MODE=true|false
VITE_HANNA_TIMEOUT_MS=1800
VITE_HANNA_RETRY_DELAY_MS=30000
```

Si el backend no responde, `hannaClient.ts` usa timeout corto, marca `backend desconectado` y aplica espera antes de reintentar. No debe spamear la consola con errores repetidos.

## Endpoints preparados

La UI deja preparados estos métodos:

- `getStatus()`
- `getDiagnostico()`
- `getLogs()`
- `sendCommand(command)`
- `sendChatMessage(message, files?)`
- `uploadFiles(files)`
- `getAdminOptions()`

Endpoints previstos:

- `GET /api/status` — preparado, no implementado de forma estable en Mobile API.
- `GET /api/diagnostico` — preparado, no implementado de forma estable en Mobile API.
- `GET /api/logs` — preparado, no implementado de forma estable en Mobile API.
- `POST /api/comando` — preparado, no implementado de forma estable en Mobile API.
- `POST /api/chat` — preparado, no implementado de forma estable en Mobile API.
- `POST /api/files/upload` — preparado, no implementado.

## Integración real detectada

El backend C# ya expone endpoints útiles en servicios existentes:

- Mobile API: `GET /api/mobile/state`, `POST /api/mobile/message`, motores, fases y búsqueda de memoria.
- Admin Web: `GET /api/state`, ajustes, motor, fase, TTS, cámara y otras acciones administrativas.

La UI intenta primero endpoints genéricos preparados y usa fallback seguro a `GET /api/mobile/state` y `POST /api/mobile/message` cuando corresponde. Algunas rutas de Mobile API pueden requerir JWT o pairing token; esta UI no guarda secretos ni tokens reales.

## Archivos

El selector/drag & drop muestra nombre, tamaño, tipo y estado. La subida real queda preparada hasta que exista `POST /api/files/upload` estable y seguro.

## Agregar opciones al menú Admin

Editar `src/api/hannaClient.ts`, método `getAdminOptions(status)`, agregando una opción con:

- `section`
- `name`
- `state`
- `action`
- `note`

Mantener el menú colapsado y no crear nuevas pantallas salvo que exista una necesidad operativa real.

## Agregar otra interfaz en el futuro

Crear otra carpeta separada para no mezclar responsabilidades. Esta WebUI debe mantenerse compacta y enfocada en chat. El Admin Web C# existente sigue siendo la interfaz administrativa completa.
