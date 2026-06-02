# Tool Calling seguro para Hanna.Lightweight

## Estado

Los esquemas son documentación. No hay ejecución real de acciones externas en esta fase.

## Respuesta común

```json
{
  "request_id": "01HNAEXAMPLE",
  "action": "buscar_memoria",
  "success": true,
  "status": "completed",
  "message": "Ejecución local segura o simulada.",
  "dry_run": true
}
```

## Acción: buscar_memoria

Segura cuando solo busca en `HannaData/vault`.

```json
{
  "request_id": "01HNASEARCH",
  "action": "buscar_memoria",
  "parameters": { "query": "prueba" },
  "requires_confirmation": false,
  "dry_run": false
}
```

## Acción: ejecutar_script

**Planificado, no implementado.** Requiere confirmación humana y allowlist.

```json
{
  "request_id": "01HNASCRIPT",
  "action": "ejecutar_script",
  "parameters": { "script_id": "approved-only", "arguments": [] },
  "requires_confirmation": true,
  "dry_run": true
}
```

## Acción: publicar_mqtt

**Planificado, no implementado.** Requiere broker local, allowlist de tópicos, RBAC y auditoría.

```json
{
  "request_id": "01HNAMQTT",
  "action": "publicar_mqtt",
  "parameters": { "topic": "hanna/dryrun", "payload": "simulado" },
  "requires_confirmation": true,
  "dry_run": true
}
```

## Acción: wake_on_lan

**Planificado, no implementado.** No se envían paquetes mágicos en esta fase.

```json
{
  "request_id": "01HNAWOL",
  "action": "wake_on_lan",
  "parameters": { "device_id": "worker-1" },
  "requires_confirmation": true,
  "dry_run": true
}
```
