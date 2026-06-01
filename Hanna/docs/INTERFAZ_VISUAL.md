# Hanna Visual Interface

## Objetivo

Esta carpeta agrega una interfaz visual opcional para Hanna basada en el diseño split-screen morado/turquesa del paquete `jarvis-ui` compartido como referencia. No reemplaza el backend C#/.NET ni elimina el Admin Web existente.

## Estado de integración

- Interfaz React/Vite independiente: implementada.
- Diseño split-screen morado/turquesa: implementado.
- Nombre y textos adaptados a Hanna: implementado.
- Endpoints reales de Hanna: preparados, no implementados en esta entrega.
- Chat visual real: preparado, no implementado.
- WebSocket/TTS visual real: preparado, no implementado.

## Cómo ejecutar Hanna backend

Desde la raíz del repositorio:

```powershell
dotnet run --project Hanna\Hanna.csproj
```

## Cómo ejecutar HannaWebUI

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Run-Hanna-WebUI.ps1
```

Luego abre:

```text
http://127.0.0.1:8788
```

## Cómo compilar HannaWebUI

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-Hanna-WebUI.ps1
```

## Endpoints preparados

La interfaz intenta usar estos endpoints si existen:

```text
GET  /api/status
GET  /api/diagnostico
GET  /api/logs
POST /api/comando
POST /api/chat
```

En desarrollo, Vite usa proxy `/api` hacia `http://127.0.0.1:8790`.

## Seguridad

- No se incluyen secretos.
- No se incluye `node_modules`.
- No se incluye `dist`.
- No se sube el ZIP original.
- Los botones de comandos se marcan como demo/preparados si el backend no responde.

## Archivos principales

```text
HannaWebUI/package.json
HannaWebUI/src/App.tsx
HannaWebUI/src/api/hannaClient.ts
HannaWebUI/src/components/ArcReactor.tsx
HannaWebUI/src/styles/hanna-ui.css
scripts/Run-Hanna-WebUI.ps1
scripts/Build-Hanna-WebUI.ps1
```

## Pendiente

1. Exponer endpoints reales desde Hanna C#.
2. Conectar `/api/status` con el estado de `StartupProfile`, motor y fase.
3. Conectar `/api/comando` con el router de comandos real.
4. Integrar logs seguros reales.
5. Evaluar si conviene servir el build desde Admin Web o mantener la UI como frontend separado.
