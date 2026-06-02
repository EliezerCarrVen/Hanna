# Arquitectura Master/Worker Hanna.Lightweight

## Estado

**Planificado, no implementado.** No existe comunicación real entre nodos, no hay MQTT activo, no hay Wake-on-LAN real y Hanna.Lightweight no se conecta con Hanna principal.

## Nodo maestro

Un HP Mini 110 puede operar como maestro ligero:

- Mantiene `HannaData/`.
- Coordina tareas locales.
- Presenta estado y auditoría.
- Rechaza acciones peligrosas por defecto.
- Solicita confirmación humana antes de cualquier acción externa futura.

## Nodo worker

Un equipo más potente futuro puede actuar como worker:

- Indexación grande.
- Inferencia local pesada.
- Procesamiento de código.
- Tareas por lotes autorizadas.

## Contratos futuros

Toda solicitud será JSON estricta con `request_id`, `action`, `parameters`, `requires_confirmation` y `dry_run`. El valor predeterminado de `dry_run` será `true`.

## Acciones prohibidas en esta fase

Ejecutar scripts, encender equipos por Wake-on-LAN, publicar MQTT, abrir VPN, controlar IoT, escanear NAS, invocar ClamAV o desplegar Docker.
