# Paridad de respuestas Hanna C# -> Hanna.NodeLightweight

| Entrada | Respuesta C# esperada | Respuesta Node actual | Estado | Prueba |
|---|---|---|---|---|
| `hola` | Saludo cálido de Hanna y disponibilidad | Saludo cálido con capacidades reales y estado emocional | ported | `npm run once -- "hola"` |
| `diagnostico` | Diagnóstico legible sin stack traces | Doctor humano con dependencias, auditoría y bloqueos | ported | `npm run once -- "diagnostico"` |
| `busca que es un llm` | Pipeline de conocimiento/IA, no unknown command | General QA: RAG primero; si no hay contexto/LLM reporta configuración faltante | ported | `npm run once -- "busca que es un llm"` |
| `guarda esto en memoria: ...` | Guarda memoria local sanitizada | JSONL + Markdown vault sanitizado | ported | `npm run once -- "guarda esto en memoria: ..."` |
| `guarda esto en obsidian: título :: contenido` | Guarda nota de conocimiento | Nota Markdown con frontmatter en Obsidian/fallback HannaData/vault | ported | `npm run once -- "guarda esto en obsidian: prueba :: contenido"` |
| `estado emocional` | Tono/reacción/persona de Hanna | Estado emocional persistente en runtime | ported | `npm run once -- "estado emocional"` |
| `/spotify estado` | Estado/auth Spotify seguro | Adapter OAuth/dry-run sin exponer secretos | ported | `npm run once -- "/spotify estado"` |
| Error técnico | Mensaje empático sin stack trace | Respuesta segura y auditoría | ported | `npm test` |
