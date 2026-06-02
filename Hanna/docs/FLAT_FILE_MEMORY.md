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

## JSONL

`runtime/short_memory.jsonl` guarda eventos recientes como una línea JSON por entrada. Es barato, append-only y fácil de reparar manualmente.

## Markdown/Obsidian

`vault/memoria` y `vault/codigo_cache` guardan notas `.md` con front matter simple. Obsidian puede abrir la carpeta `HannaData/vault` sin ser dependencia de arranque.

## Rolling summary

`runtime/last_summary.md` existe desde el arranque y queda listo para una fase futura de resumen incremental.

## Búsqueda

Hanna.Lightweight usa `rg --line-number --fixed-strings --ignore-case` cuando ripgrep está disponible. Si no existe, usa búsqueda C# simple sobre Markdown.

## Protección de secretos

Antes de guardar memoria se aplica un filtro local para redactar nombres de variables sensibles y asignaciones de token, API key, secret o password.
