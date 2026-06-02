# Roadmap Lightweight de Hanna

## Alcance

Este roadmap organiza la evolución ligera de Hanna por fases. Las fases futuras están **planificadas, no implementadas** y no deben reemplazar la arquitectura actual hasta que exista una PR específica, pruebas y mecanismo de rollback.

## Fase 0: congelar versión estable

### Objetivo

Establecer una línea base de la versión actual antes de conectar componentes lightweight.

### Archivos probables

- `Hanna/Hanna.csproj`
- `Hanna/Program.cs`
- `Hanna/Core/AppConfig.cs`
- `Hanna/Services/TelegramService.cs`
- `Hanna/Services/MemoryService.cs`
- `Hanna/Services/TieredMemoryService.cs`
- `Hanna/docs/`

### Riesgos

- No detectar regresiones existentes.
- Cambiar comportamiento de Telegram, `/motor`, `/fase` o `HANNA_MODE` sin intención.

### Pruebas

- `dotnet build Hanna/Hanna.csproj --no-incremental`
- Pruebas manuales de arranque con configuración actual.
- Verificación manual de comandos Telegram críticos.

### Resultado esperado

Una referencia estable documentada y lista para comparar cambios futuros.

## Fase 1: memoria flat-file

### Objetivo

Agregar una memoria opcional basada en Markdown, JSONL y rolling summary, sin reemplazar MongoDB, MySQL ni servicios actuales.

### Archivos probables

- `Hanna/Core/Lightweight/FlatFileMemoryOptions.cs`
- `Hanna/Core/Lightweight/ShortMemoryEntry.cs`
- `Hanna/Services/FlatFileMemoryService.cs` (planificado, no implementado)
- `Hanna/docs/FLAT_FILE_MEMORY.md`

### Riesgos

- Duplicación de recuerdos.
- Escrituras excesivas en hardware lento.
- Persistencia accidental de secretos.

### Pruebas

- Build completo.
- Pruebas unitarias de serialización JSONL cuando existan.
- Pruebas de búsqueda con `rg` en una bóveda temporal.

### Resultado esperado

Memoria flat-file disponible detrás de configuración explícita y apagada por defecto.

## Fase 2: Worker Service

### Objetivo

Crear un Worker Service opcional para tareas de fondo ligeras y desacopladas.

### Archivos probables

- `Hanna/Workers/LightweightWorker.cs` (planificado, no implementado)
- `Hanna/Core/Lightweight/`
- `Hanna/Core/AppConfig.cs`
- `Hanna/docs/MASTER_WORKER_ARCHITECTURE.md`

### Riesgos

- Consumo innecesario de CPU/RAM.
- Dificultad para apagar limpiamente.
- Interacciones accidentales con servicios actuales.

### Pruebas

- Build completo.
- Arranque con worker deshabilitado.
- Arranque con worker habilitado en modo dry-run cuando se implemente.

### Resultado esperado

Worker apagado por defecto, seguro y sin impacto en la versión actual.

## Fase 3: comandos por JSON/tool calling

### Objetivo

Definir y validar comandos JSON estrictos antes de permitir acciones automatizadas.

### Archivos probables

- `Hanna/Core/Lightweight/ToolAction.cs`
- `Hanna/Core/Lightweight/ToolActionResult.cs`
- `Hanna/Services/ToolActionValidatorService.cs` (planificado, no implementado)
- `Hanna/docs/TOOL_CALLING_SCHEMA.md`

### Riesgos

- Ejecución de comandos peligrosos.
- Interpretación laxa de JSON.
- Falta de confirmación humana.

### Pruebas

- Validación de esquemas permitidos y rechazados.
- Pruebas con acciones peligrosas simuladas.
- Verificar que no exista ejecución real hasta habilitación explícita.

### Resultado esperado

Canal de intención estructurada en JSON con deny-by-default y confirmaciones.

## Fase 4: MQTT

### Objetivo

Agregar mensajería local ligera para Master/Worker.

### Archivos probables

- `Hanna/Services/MqttBridgeService.cs` (planificado, no implementado)
- `Hanna/Core/Lightweight/`
- `Hanna/docs/MASTER_WORKER_ARCHITECTURE.md`

### Riesgos

- Exposición de comandos en red local.
- Falta de autenticación.
- Reintentos que dupliquen acciones.

### Pruebas

- Broker local de prueba.
- Mensajes firmados o autenticados cuando aplique.
- Verificación de `HANNA_MQTT_ENABLED=false` por defecto.

### Resultado esperado

MQTT opcional, configurado explícitamente y sin ejecución peligrosa automática.

## Fase 5: NAS indexer

### Objetivo

Crear índice de inventario para archivos de NAS o almacenamiento compartido, inicialmente solo lectura.

### Archivos probables

- `Hanna/Services/NasIndexerService.cs` (planificado, no implementado)
- `Hanna/Core/Lightweight/`
- `HannaData/indexes/file_index.jsonl`

### Riesgos

- Recorrer rutas enormes.
- Indexar datos privados.
- Bloquear red o disco.

### Pruebas

- Carpeta de prueba pequeña.
- Límites de tamaño, profundidad y extensiones.
- Confirmar que no modifica archivos remotos.

### Resultado esperado

Inventario local consultable, seguro y acotado.

## Fase 6: VPN/red

### Objetivo

Documentar y preparar conectividad segura entre nodos sin automatizar infraestructura sensible en fases tempranas.

### Archivos probables

- `Hanna/docs/MASTER_WORKER_ARCHITECTURE.md`
- `Hanna/docs/ROADMAP_LIGHTWEIGHT.md`
- Scripts futuros de diagnóstico (planificado, no implementado)

### Riesgos

- Exponer servicios internos.
- Automatizar cambios de red sin confirmación.
- Confundir red local con red confiable.

### Pruebas

- Checklist manual de conectividad.
- Validación de puertos esperados.
- Confirmación explícita antes de acciones de red.

### Resultado esperado

Diseño de red claro, con seguridad primero y sin cambios automáticos no autorizados.

## Fase 7: serverless

### Objetivo

Evaluar procesamiento externo puntual para tareas que no deban ejecutarse en el HP Mini 110.

### Archivos probables

- `Hanna/docs/MASTER_PLAN_LIGHTWEIGHT.md`
- `Hanna/Services/ServerlessBridgeService.cs` (planificado, no implementado)

### Riesgos

- Costos inesperados.
- Envío de datos sensibles.
- Dependencia de servicios externos.

### Pruebas

- Dry-run con payloads mínimos.
- Límites de presupuesto.
- Auditoría de datos enviados.

### Resultado esperado

Capacidad opcional de offload, protegida por configuración y presupuestos.
