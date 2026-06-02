# Esquemas JSON seguros para Tool Calling futuro

## Estado

Este documento propone contratos JSON para acciones futuras. No existe ejecución real en esta fase. Todo queda **planificado, no implementado**.

## Reglas generales

Todos los comandos deben incluir:

- `action`: nombre exacto de acción permitida.
- `request_id`: identificador único de la solicitud.
- `parameters`: objeto de parámetros validado por acción.
- `requires_confirmation`: `true` para acciones con riesgo.
- `dry_run`: `true` por defecto hasta autorización explícita.

Respuesta común propuesta:

```json
{
  "request_id": "01HNAEXAMPLE",
  "action": "buscar_memoria",
  "success": true,
  "status": "completed",
  "message": "Resultado simulado; ejecución real planificada, no implementada.",
  "data": {},
  "error_code": null
}
```

## `abrir_programa`

Abre una aplicación permitida localmente. **Planificado, no implementado**.

```json
{
  "action": "abrir_programa",
  "request_id": "01HNAOPENAPP",
  "parameters": {
    "program_alias": "notepad",
    "arguments": [],
    "working_directory": null
  },
  "requires_confirmation": true,
  "dry_run": true
}
```

Seguridad:

- Usar alias permitidos, no rutas arbitrarias.
- Rechazar argumentos desconocidos.
- Confirmar antes de abrir programas externos.

## `ejecutar_script`

Ejecuta un script previamente aprobado. **Planificado, no implementado**.

```json
{
  "action": "ejecutar_script",
  "request_id": "01HNASCRIPT",
  "parameters": {
    "script_id": "diagnostico_basico",
    "arguments": {},
    "timeout_seconds": 30
  },
  "requires_confirmation": true,
  "dry_run": true
}
```

Seguridad:

- Prohibir scripts por ruta directa.
- Mantener catálogo de scripts permitidos.
- Aplicar timeout.
- Registrar salida truncada.

## `buscar_memoria`

Busca en memoria flat-file futura. **Planificado, no implementado**.

```json
{
  "action": "buscar_memoria",
  "request_id": "01HNASEARCH",
  "parameters": {
    "query": "proyecto Hanna",
    "scope": "vault",
    "limit": 10
  },
  "requires_confirmation": false,
  "dry_run": true
}
```

Seguridad:

- Limitar búsqueda a la bóveda configurada.
- Sanitizar consulta.
- Limitar resultados.

## `indexar_archivos`

Indexa archivos locales o de inventario futuro en modo solo lectura. **Planificado, no implementado**.

```json
{
  "action": "indexar_archivos",
  "request_id": "01HNAINDEX",
  "parameters": {
    "root_alias": "vault",
    "max_depth": 4,
    "include_extensions": [".md", ".txt", ".jsonl"],
    "max_files": 1000
  },
  "requires_confirmation": true,
  "dry_run": true
}
```

Seguridad:

- Usar alias de raíz permitidos.
- No modificar archivos.
- Respetar límites de cantidad, tamaño y profundidad.

## `resumir_sesion`

Genera rolling summary de la sesión actual. **Planificado, no implementado**.

```json
{
  "action": "resumir_sesion",
  "request_id": "01HNASUMMARY",
  "parameters": {
    "session_id": "local-session",
    "max_source_events": 200,
    "write_summary": false
  },
  "requires_confirmation": false,
  "dry_run": true
}
```

Seguridad:

- Filtrar secretos antes de resumir.
- Marcar incertidumbre.
- Permitir vista previa antes de escribir.

## `enviar_mqtt`

Envía mensaje MQTT local futuro. **Planificado, no implementado**.

```json
{
  "action": "enviar_mqtt",
  "request_id": "01HNAMQTT",
  "parameters": {
    "topic": "hanna/worker/tasks",
    "payload": {
      "type": "ping"
    },
    "qos": 0,
    "retain": false
  },
  "requires_confirmation": true,
  "dry_run": true
}
```

Seguridad:

- Requerir `HANNA_MQTT_ENABLED=true` cuando se implemente.
- Validar topic contra prefijos permitidos.
- No enviar secretos en payload.

## `despertar_equipo`

Despierta un equipo por Wake-on-LAN futuro. **Planificado, no implementado**.

```json
{
  "action": "despertar_equipo",
  "request_id": "01HNAWOL",
  "parameters": {
    "device_alias": "victus15",
    "network_alias": "lan_principal"
  },
  "requires_confirmation": true,
  "dry_run": true
}
```

Seguridad:

- Usar alias de dispositivo, no MAC directa en prompts.
- Confirmación humana obligatoria.
- Registrar intentos.
- No implementar hasta fase aprobada.

## Acciones explícitamente fuera de alcance en esta base

- Ejecución real de comandos.
- MQTT real.
- Wake-on-LAN real.
- NAS indexer real.
- Cambios de VPN, Pi-hole o red.
- Modificaciones de WebUI.
