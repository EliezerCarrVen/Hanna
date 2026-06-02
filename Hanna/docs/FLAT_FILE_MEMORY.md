# Memoria Flat-File de Hanna

## Estado

La memoria flat-file queda **planificada, no implementada** como flujo activo. Este documento define la estructura objetivo para una implementación futura sin reemplazar MongoDB, MySQL, `MemoryService` ni `TieredMemoryService`.

## Estructura de carpetas propuesta

```text
HannaData/
  vault/
    personas/
    proyectos/
    memoria/
    inventario/
    tareas/
    sistema/
  runtime/
    short_memory.jsonl
    current_session.jsonl
    last_summary.md
  indexes/
    file_index.jsonl
    vault_index.jsonl
```

### `vault/`

Bóveda Markdown pensada para consulta humana y edición opcional en Obsidian.

- `personas/`: perfiles no sensibles, preferencias públicas o autorizadas.
- `proyectos/`: notas de proyectos, contexto y decisiones.
- `memoria/`: conocimiento persistente resumido.
- `inventario/`: catálogos futuros de dispositivos o archivos; **planificado, no implementado**.
- `tareas/`: pendientes y seguimiento.
- `sistema/`: notas operativas no secretas.

### `runtime/`

Archivos generados por Hanna para estado temporal o corto plazo.

- `short_memory.jsonl`: eventos recientes persistentes.
- `current_session.jsonl`: conversación o actividad de la sesión actual.
- `last_summary.md`: resumen acumulado más reciente.

### `indexes/`

Índices derivados y regenerables.

- `file_index.jsonl`: inventario futuro de archivos; **planificado, no implementado**.
- `vault_index.jsonl`: índice de notas Markdown; **planificado, no implementado**.

## Formato de notas Markdown

Cada nota debe ser legible sin herramientas especiales. Formato recomendado:

```markdown
---
type: proyecto
title: Hanna Lightweight
created_utc: 2026-06-02T00:00:00Z
updated_utc: 2026-06-02T00:00:00Z
tags:
  - hanna
  - lightweight
sensitivity: normal
source: manual
---

# Hanna Lightweight

## Resumen

Descripción breve del contexto.

## Hechos relevantes

- Hecho persistente y verificable.

## Decisiones

- Decisión tomada y fecha.

## Pendientes

- [ ] Tarea futura.
```

## YAML frontmatter

Campos mínimos recomendados:

- `type`: `persona`, `proyecto`, `memoria`, `inventario`, `tarea` o `sistema`.
- `title`: nombre humano de la nota.
- `created_utc`: fecha de creación en UTC.
- `updated_utc`: fecha de última actualización en UTC.
- `tags`: lista de etiquetas.
- `sensitivity`: `public`, `normal`, `private` o `restricted`.
- `source`: `manual`, `telegram`, `local`, `summary`, `import` u otro origen permitido.

Campos opcionales:

- `expires_utc`: fecha de expiración si el dato no debe persistir indefinidamente.
- `related`: enlaces a otras notas.
- `confidence`: `low`, `medium` o `high`.

## Memoria corta JSONL

Cada línea debe ser un JSON independiente. Formato propuesto:

```json
{"id":"01HNA...","timestamp_utc":"2026-06-02T00:00:00Z","source":"telegram","role":"user","content":"Recordatorio no sensible","tags":["recordatorio"],"importance":3,"expires_utc":null}
```

Reglas:

- Append-only durante la sesión.
- No guardar secretos.
- Permitir truncado o compactación por tamaño.
- Validar tamaño máximo por entrada.
- Escribir timestamps en UTC.

## Rolling summary

El rolling summary convierte eventos recientes en un resumen compacto.

Debe conservar:

- Preferencias persistentes autorizadas.
- Decisiones del proyecto.
- Pendientes y fechas.
- Errores relevantes y soluciones.

Debe descartar:

- Tokens, claves, contraseñas o cookies.
- Datos personales innecesarios.
- Mensajes repetidos.
- Contenido de baja confianza sin marca de incertidumbre.

Estado: **planificado, no implementado**.

## Búsqueda con ripgrep

La búsqueda propuesta usa `rg` por ser rápida y disponible en muchos entornos.

Variable futura documentada:

```env
HANNA_RIPGREP_PATH=rg
```

Ejemplo conceptual, **planificado, no implementado**:

```bash
rg --json -- "texto a buscar" HannaData/vault
```

Reglas de seguridad:

- No pasar entrada de usuario sin sanitizar a shell.
- Preferir ejecución con argumentos separados en vez de comando concatenado.
- Limitar rutas al directorio configurado de la bóveda.
- Aplicar timeout.
- Limitar tamaño de resultados.

## Filtros de seguridad

Antes de persistir memoria, aplicar filtros para bloquear o redactar:

- API keys y tokens.
- Contraseñas.
- Cookies y sesiones.
- Llaves privadas SSH/GPG.
- Datos bancarios.
- Documentos oficiales o identificaciones personales sin permiso explícito.
- Rutas personales sensibles.
- Contenido que el usuario marque como temporal o confidencial.

## Qué datos no deben guardarse

No deben guardarse por defecto:

- Secretos de `.env`, `HannaEnv.env` o variables de entorno.
- Tokens de Telegram, OpenRouter, Groq, Gemini, Spotify u otros proveedores.
- Contraseñas, PIN, frases semilla o claves privadas.
- Datos médicos, financieros o legales sensibles salvo instrucción explícita y mecanismo de protección.
- Conversaciones privadas de terceros sin consentimiento.
- Capturas de pantalla o transcripciones con datos sensibles sin confirmación.

## Variables futuras documentadas

```env
HANNA_LIGHTWEIGHT_MODE=false
HANNA_OBSIDIAN_VAULT_PATH=
HANNA_SHORT_MEMORY_PATH=
HANNA_RIPGREP_PATH=rg
HANNA_ROLLING_SUMMARY_ENABLED=false
```

Estas variables están documentadas, pero **no implementadas ni exigidas** en esta fase.
