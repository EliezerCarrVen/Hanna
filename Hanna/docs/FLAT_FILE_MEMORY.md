# Memoria Flat-File de Hanna.Lightweight

## Estado

Implementación mínima activa en `Hanna.Lightweight/Services/`. No reemplaza MongoDB, MySQL ni servicios de memoria de Hanna principal.

## Estructura

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

## Por qué HannaData no se sube

`HannaData/` contiene memoria local, auditoría, logs, índices y datos de runtime. Está en `.gitignore` para evitar publicar datos privados, secretos redactados parcialmente o historial de actividad.

## JSONL

`runtime/short_memory.jsonl` guarda eventos recientes como una línea JSON por entrada. La lectura está limitada por `MaxJsonlEntriesToRead` y las entradas se truncan con `MaxMemoryEntryLength` antes de persistir.

## Markdown/Obsidian

`vault/memoria` y `vault/codigo_cache` guardan notas `.md` con frontmatter simple. Obsidian puede abrir `HannaData/vault` sin ser dependencia de arranque.

## Rolling summary local

`/summary` o `/summary regenerar` lee memoria reciente, detecta palabras frecuentes, lista últimas acciones, agrega advertencias de seguridad y actualiza `runtime/last_summary.md`. Es un resumen extractivo local, no IA.

## Índice de vault

`/indexar` recorre `vault/`, ignora archivos mayores a `MaxSearchFileBytes`, detecta tags de YAML frontmatter, calcula SHA256 y escribe `indexes/vault_index.jsonl`. `PathGuard` impide salir de `HannaData/`.

## Caché de código

El caché de código usa `vault/codigo_cache` más `indexes/code_cache_index.jsonl`. Incluye YAML frontmatter, tags de lenguaje/tema/origen/fecha, deduplicación SHA256, `/codigo listar` y `/codigo estado`. La traducción dinámica sigue `planned_not_implemented`.

## Búsqueda

Hanna.Lightweight usa ripgrep cuando existe. Si no, usa fallback C# limitado por `MaxSearchFileBytes` y `MaxSearchResults`.

## Seguridad

Antes de guardar memoria se aplica `SecretFilterService`, y todas las escrituras pasan por `PathGuardService`. Los logs rotan por tamaño con `LogRotationService`.
