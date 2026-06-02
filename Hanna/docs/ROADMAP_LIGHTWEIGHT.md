# Roadmap Hanna.Lightweight

## Fase actual: núcleo paralelo mínimo

- Crear `Hanna.Lightweight/Hanna.Lightweight.csproj`.
- Crear consola ejecutable independiente.
- Crear `HannaData/` automáticamente.
- Implementar memoria JSONL corta, notas Markdown, caché de código mínimo, búsqueda local, logs y auditoría simulada.
- Mantener Hanna principal intacta.

## Fase 1: robustez local

- Tests automatizados para filtro de secretos.
- Rotación segura de logs JSONL.
- Rolling summary real y regenerable.
- Índices regenerables para vault y código.

## Fase 2: Obsidian y cache semántico

- Convenciones de front matter.
- Backlinks y tags estables.
- Extracción de snippets de código sin secretos.
- Búsqueda híbrida textual/semántica local, sin subir datos.

## Fase 3: Master/Worker seguro

- Contratos JSON estrictos.
- Confirmación humana para acciones peligrosas.
- Workers sin acceso a secretos.
- Sin ejecución remota hasta tener autenticación, auditoría y rollback.

## Fase 4: MQTT, IoT y Node-RED

- Solo broker local autorizado.
- Allowlist de tópicos.
- DryRun antes de publicar.
- Node-RED como staging aislado.

## Fase 5: Enterprise y seguridad avanzada

- Multi-tenant real.
- RBAC real.
- Auditoría firmada.
- Búnker cifrado AES-256.
- ClamAV real.
- NAS indexer real.
- Serverless controlado.

## Riesgos pendientes

- Persistencia accidental de secretos.
- Desgaste de disco por escritura excesiva.
- Comandos peligrosos sin confirmación.
- Acoplar accidentalmente Hanna principal con Hanna.Lightweight.
