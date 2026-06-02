# Arquitectura Master/Worker Lightweight

## Estado

La arquitectura Master/Worker queda **planificada, no implementada**. Esta base solo documenta responsabilidades, límites y modelos futuros; no activa comunicación local, MQTT ni ejecución remota.

## Nodo Maestro: HP Mini 110

El HP Mini 110 actúa como coordinador ligero y punto de continuidad.

Responsabilidades propuestas:

- Mantener estado mínimo de Hanna.
- Gestionar memoria flat-file futura.
- Coordinar sesiones y rolling summary.
- Validar comandos JSON estrictos.
- Solicitar confirmación humana antes de acciones peligrosas.
- Delegar tareas pesadas a workers solo cuando `HANNA_MASTER_WORKER_ENABLED=true` en una fase futura.

No debe:

- Ejecutar cargas pesadas si degradan el sistema.
- Exponer comandos remotos sin autenticación.
- Depender de NAS, VPN, MQTT o Wake-on-LAN para arrancar.

## Nodo Agente: Victus 15

La Victus 15 puede funcionar como worker de mayor potencia.

Responsabilidades propuestas:

- Ejecutar cómputo intensivo autorizado.
- Procesar indexaciones grandes.
- Correr modelos locales más pesados si están disponibles.
- Ejecutar scripts aprobados con límites.
- Devolver resultados resumidos al maestro.

No debe:

- Aceptar comandos arbitrarios sin validación.
- Acceder a secretos del maestro salvo que una fase futura defina un mecanismo seguro.
- Ejecutar acciones destructivas sin confirmación explícita.

## Responsabilidades por nodo

| Área | HP Mini 110 Maestro | Victus 15 Worker |
| --- | --- | --- |
| Estado de sesión | Principal | Secundario |
| Memoria flat-file | Principal | Lectura o apoyo futuro |
| Inferencia pesada | Evitar | Preferida si se autoriza |
| Indexación grande | Coordina | Ejecuta si se autoriza |
| Comandos peligrosos | Pide confirmación | No ejecuta sin autorización |
| MQTT | Planificado, no implementado | Planificado, no implementado |
| Wake-on-LAN | Planificado, no implementado | Planificado, no implementado |

## Comandos JSON estrictos

Los comandos futuros deben ser objetos JSON con campos conocidos y rechazo por defecto. No se debe inferir una acción si faltan campos requeridos.

Ejemplo conceptual:

```json
{
  "action": "buscar_memoria",
  "request_id": "01HNAEXAMPLE",
  "parameters": {
    "query": "Hanna lightweight",
    "limit": 10
  },
  "requires_confirmation": false,
  "dry_run": true
}
```

Reglas:

- `action` debe pertenecer a una lista permitida.
- `parameters` debe validarse por acción.
- `dry_run` debe existir para acciones con efectos externos.
- Toda acción peligrosa debe requerir confirmación.
- Comandos desconocidos se rechazan.

## Seguridad

Principios mínimos:

- Deny-by-default.
- Autenticación explícita para comunicación entre nodos cuando se implemente.
- Lista de acciones permitidas.
- Timeouts y límites de tamaño.
- Auditoría local de acciones aceptadas y rechazadas.
- Separación entre intención, validación y ejecución.
- Nunca guardar secretos en memoria plana.

## Confirmación antes de acciones peligrosas

Deben requerir confirmación humana acciones como:

- Ejecutar scripts.
- Borrar, mover o modificar archivos.
- Enviar mensajes externos.
- Despertar equipos por red.
- Cambiar configuración de red.
- Instalar paquetes.
- Acceder a rutas sensibles.

La confirmación debe mostrar acción, parámetros, riesgo y resultado esperado.

## Qué queda planificado, no implementado

- MQTT entre nodos.
- Worker Service real.
- Ejecución de scripts remotos.
- Wake-on-LAN.
- NAS indexer.
- VPN o red privada.
- Sincronización de Obsidian.
- Autenticación entre nodos.
- Serverless.

## Variables futuras documentadas

```env
HANNA_MASTER_WORKER_ENABLED=false
HANNA_MQTT_ENABLED=false
```

Estas variables están documentadas para diseño y permanecen **planificadas, no implementadas**.
