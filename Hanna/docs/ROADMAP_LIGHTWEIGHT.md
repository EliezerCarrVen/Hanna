# Roadmap Hanna.Lightweight

## Fase actual: hardening local seguro

Ya existen arranque, `HannaData/`, JSONL, Markdown vault, búsqueda, caché mínimo, logs, auditoría y comandos iniciales. Esta fase no los reimplementa; agrega protección y diagnósticos:

- `HannaData/` en `.gitignore`.
- `--self-test`, `/selftest` y `--once`.
- `/doctor` con PASS/WARN/FAIL.
- `PathGuardService` para impedir escrituras fuera de `HannaData/`.
- límites de tamaño para memoria, búsqueda, logs y comandos.
- `LogRotationService`.
- `SecretFilterService` ampliado.
- `/summary` local extractivo.
- `/indexar` e `/indice estado`.
- caché de código con deduplicación SHA256 y estado/listado.

## Qué no debe reimplementarse

No rehacer desde cero el núcleo funcional: startup, estructura `HannaData/`, JSONL, vault Markdown, búsqueda `rg`/fallback, logs, auditoría, comandos base ni modelos existentes. Las siguientes fases deben integrarse de forma incremental.

## Fase siguiente recomendada

- Tests unitarios para SecretFilter, PathGuard, LogRotation, JSONL, doctor, self-test e indexación.
- Modo `--data-root` seguro si se requiere ubicación configurable dentro de una allowlist.
- Rotación con retención máxima configurable.
- Índices regenerables por lotes para hardware lento.

## Fases futuras planificadas

### Obsidian y memoria avanzada

Frontmatter estable, backlinks, tareas, rolling summary incremental y cache semántico local sin servicios externos.

### Master/Worker seguro

Contratos JSON estrictos, autenticación, auditoría, confirmación humana y `DryRun=true` por defecto. No conectar con Hanna principal todavía.

### MQTT, IoT y Node-RED

Broker local autorizado, allowlist de tópicos, staging, simulación previa y confirmación humana.

### Enterprise real

Multi-tenant, RBAC, auditoría firmada, búnker cifrado, ClamAV, NAS indexer, Docker y Serverless. Todo sigue `planned_not_implemented` hasta PR específica.

## Riesgos pendientes

- Falsos negativos del filtro de secretos.
- Falsos positivos al redactar cadenas largas.
- Crecimiento de logs si no se define retención.
- Compilación pendiente cuando el entorno no tiene .NET SDK.
