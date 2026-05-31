# Hanna V6.4 - Panel integrado y arranque tolerante

Esta versión corrige dos puntos detectados en V6.2/V6.3:

1. Hanna podía parecer atorada después de MongoDB porque la memoria jerárquica importaba índices durante el arranque. En V6.4 `TieredMemoryService` crea `index.db` sin importar JSONL de forma síncrona. La consolidación queda para mantenimiento.
2. El chat online ya no debe estar separado como experiencia principal. Ahora está dentro del panel principal `http://127.0.0.1:8787/` en la pestaña **Chat online integrado**.

## Cambios principales

- Arranque con mensajes de diagnóstico después de MongoDB.
- Servicios nuevos iniciados con manejo tolerante a errores (`SafeStart` / `SafeStartAsync`).
- Panel web rediseñado.
- Chat integrado en el panel 8787.
- Selector de motor y fase dentro del panel.
- Estado visible de Ollama, Mobile API, memoria, auditoría, RBAC y mantenimiento.
- `HANNA_WEBCHAT_ENABLED=false` porque el servidor separado 8789 queda como legado/opcional.
- `HANNA_SCREEN_ANALYSIS_ENABLED=true` para permitir F9.
- Memoria jerárquica sin bloqueo en arranque.

## Cómo probar

```powershell
cd "C:\Users\Eliezer\Downloads\Hanna_V6_4_PANEL_REDISSENO_ARRANQUE_ESTABLE\Hanna\Hanna"
dotnet restore
dotnet build
dotnet run
```

Deben aparecer líneas parecidas a:

```text
[Arranque] MongoDB listo. Inicializando servicios base V6.4...
[TieredMemory] Memoria jerárquica lista sin bloqueo de arranque.
[Arranque] Servicios de IA, fases, memoria, auditoría y respuesta inicializados.
[Arranque] Skills cargadas...
[Mobile API] Activa en http://127.0.0.1:8790/
[Admin Web] Activo en http://127.0.0.1:8787/
Hanna modular está en línea...
```

Luego abre:

```text
http://127.0.0.1:8787/
```

Entra a **Chat online integrado**.
