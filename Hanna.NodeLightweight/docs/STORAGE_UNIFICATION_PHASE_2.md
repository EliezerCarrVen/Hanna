# FASE 2 — Unificación de almacenamiento i386

Hanna.NodeLightweight no usa MongoDB, mongoose, mongodb, bson ni drivers nativos. El esquema histórico de MongoDB de Hanna C# se mapea a almacenamiento Zero-Bloat compatible con Debian 12 i386:

| Colección histórica | Almacenamiento NodeLightweight | Servicio |
|---|---|---|
| `memorias` | Markdown `.md` en vault local | `MarkdownVaultService` vía `StorageMappingService` |
| `contexto_proyectos` | Markdown `.md` en `proyectos/` | `MarkdownVaultService` vía `StorageMappingService` |
| `codigo_generado` | Markdown `.md` en `codigo_cache/` | `MarkdownVaultService` vía `StorageMappingService` |
| `conversaciones` | JSONL | `JsonlStoreService` vía `StorageMappingService` |
| `mensajes` | JSONL | `JsonlStoreService` vía `StorageMappingService` |
| `transcripciones_audio` | JSONL | `JsonlStoreService` vía `StorageMappingService` |
| `analisis_pantalla` | JSONL | `JsonlStoreService` vía `StorageMappingService` |
| `acciones_agente` | JSONL | `JsonlStoreService` vía `StorageMappingService` |
| `estado_sistema` | `HannaData/runtime/config.json` | `StorageMappingService` |

`RemoteSyncService` permite enviar copias a un servidor externo vía HTTP/HTTPS usando `HANNA_REMOTE_SYNC_URL`, sin instalar drivers nativos de MongoDB en la HP Mini.
