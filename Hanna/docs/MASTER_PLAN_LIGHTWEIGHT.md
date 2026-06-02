# Hanna Lightweight Master Plan

## Estado del documento

Este documento define la base arquitectónica para evolucionar Hanna hacia un modo ligero ejecutable en hardware limitado. Es una guía de diseño: salvo las clases modelo bajo `Core/Lightweight`, todo lo descrito aquí queda **planificado, no implementado** hasta que una fase posterior lo conecte explícitamente.

## Objetivo de la arquitectura ligera

Preparar a Hanna para funcionar como asistente local de bajo consumo en equipos modestos, especialmente un HP Mini 110, sin romper la versión actual que ya integra Telegram, motores IA, memoria existente, perfiles `HANNA_MODE`, WebUI y servicios locales.

La arquitectura ligera busca:

- Reducir dependencias obligatorias en runtime.
- Mantener una memoria local simple, auditable y portable.
- Permitir operación degradada cuando no existan GPU, NAS, bases de datos o red estable.
- Separar responsabilidades entre un nodo maestro ligero y nodos trabajadores más potentes.
- Mantener compatibilidad con MongoDB, MySQL, `TieredMemoryService`, `MemoryService`, Telegram, `/motor`, `/fase` y `HANNA_MODE`.

## Motivación

Hanna ya tiene capacidades amplias, pero un equipo como HP Mini 110 no debe cargar todos los servicios pesados para cumplir tareas básicas. La estrategia ligera prioriza persistencia flat-file, resúmenes incrementales, búsqueda local con herramientas simples y delegación futura a workers cuando se requiera cómputo intensivo.

## Limitaciones de hardware esperadas

Un HP Mini 110 puede tener CPU Atom, memoria RAM limitada, almacenamiento lento y poca tolerancia a procesos residentes pesados. Por eso la versión ligera debe asumir:

- Sin GPU útil para inferencia local grande.
- Poca RAM disponible para índices grandes o bases de datos en memoria.
- Disco posiblemente HDD o SSD pequeño.
- Red local intermitente.
- Necesidad de arranque simple y reparación manual fácil.
- Priorización de procesos secuenciales sobre concurrencia agresiva.

## Principios de compatibilidad

- No eliminar MongoDB ni MySQL todavía.
- No reemplazar servicios actuales de memoria.
- No tocar WebUI dentro de esta base.
- No romper Telegram, `/motor`, `/fase` ni perfiles `HANNA_MODE`.
- No agregar secretos ni rutas personales.
- Marcar toda integración futura como **planificado, no implementado**.

## Memoria flat-file

La memoria flat-file será una capa opcional y futura para almacenar conocimiento local sin depender de base de datos. Usará archivos Markdown, JSONL e índices simples. Queda **planificada, no implementada** como servicio activo.

Objetivos:

- Facilitar edición manual.
- Permitir respaldo simple copiando carpetas.
- Usar `rg` para búsqueda textual rápida.
- Evitar corrupción compleja por caídas de energía.
- Mantener datos sensibles fuera de la memoria por defecto.

## Obsidian como bóveda de conocimiento

Obsidian se propone como visor/editor opcional para la bóveda Markdown. Hanna no debe depender de Obsidian para arrancar. La bóveda debe ser válida como carpetas y archivos `.md` normales.

Uso esperado, **planificado, no implementado**:

- Notas por personas, proyectos, tareas, inventario y sistema.
- YAML frontmatter para metadatos.
- Enlaces wiki-style opcionales entre notas.
- Revisión manual desde Obsidian sin bloquear a Hanna.

## JSONL como memoria corta

La memoria corta usará JSON Lines para eventos recientes y sesiones activas. Cada línea representa un evento independiente para permitir append seguro y recuperación parcial.

Archivos propuestos:

- `runtime/short_memory.jsonl`: historial corto persistente.
- `runtime/current_session.jsonl`: sesión actual.
- `runtime/last_summary.md`: último resumen acumulado.

Esta memoria corta queda **planificada, no implementada** como flujo activo.

## Rolling summary

El rolling summary compactará memoria reciente en resúmenes Markdown para reducir contexto y uso de tokens. Debe conservar decisiones, preferencias persistentes, pendientes y hechos relevantes; no debe guardar secretos.

Estado: **planificado, no implementado**.

## Worker Service

Un Worker Service .NET futuro podrá ejecutar tareas ligeras de fondo: indexar archivos, compactar memoria, generar resúmenes o coordinar trabajos con agentes externos. Debe ser opcional y controlado por variable de entorno.

Estado: **planificado, no implementado**.

## Arquitectura Master/Worker

La arquitectura futura separa un Nodo Maestro ligero en HP Mini 110 y uno o más nodos Worker, por ejemplo una Victus 15, para tareas pesadas.

- Maestro: estado, memoria, comandos seguros, coordinación.
- Worker: cómputo pesado, scripts autorizados, indexación intensiva, inferencia local si existe hardware.

La comunicación local queda **planificada, no implementada**.

## MQTT futuro

MQTT se considera como canal liviano para mensajes locales entre nodos. No se implementa en esta base. Cualquier uso de MQTT debe requerir configuración explícita, autenticación cuando aplique y límites de comandos.

Estado: **planificado, no implementado**.

## APIs IA externas

Las APIs IA externas seguirán siendo útiles para razonamiento, resumen o clasificación cuando el hardware local no alcance. En modo ligero deben usarse con presupuestos, límites de tokens y fallback seguro. No se agregan claves ni secretos.

Estado de nuevos flujos: **planificado, no implementado**.

## NAS e inventario futuro

Un indexador futuro podrá catalogar archivos de NAS o discos compartidos para búsqueda local. Esta fase no implementa NAS, inventario, Pi-hole, VPN, Wake-on-LAN ni rutas de red.

Estado: **planificado, no implementado**.

## Serverless futuro

Serverless puede servir para trabajos esporádicos, webhooks o procesamiento externo sin mantener servidores activos. Debe evaluarse después de estabilizar la memoria y la arquitectura local.

Estado: **planificado, no implementado**.

## Variables futuras documentadas

Estas variables se documentan para diseño, pero no se exigen ni se conectan todavía:

```env
HANNA_LIGHTWEIGHT_MODE=false
HANNA_OBSIDIAN_VAULT_PATH=
HANNA_SHORT_MEMORY_PATH=
HANNA_RIPGREP_PATH=rg
HANNA_ROLLING_SUMMARY_ENABLED=false
HANNA_MASTER_WORKER_ENABLED=false
HANNA_MQTT_ENABLED=false
```

## Riesgos

- Duplicar memoria entre servicios actuales y flat-file si se conecta sin estrategia de precedencia.
- Guardar datos sensibles en Markdown o JSONL por accidente.
- Saturar discos lentos con escrituras frecuentes.
- Crear comandos remotos inseguros si se habilita Master/Worker sin autorización estricta.
- Depender demasiado de Obsidian, que debe ser visor opcional, no requisito.
- Introducir cambios en Telegram, WebUI o `HANNA_MODE` durante fases tempranas.

## Fases de implementación

1. **Fase 0: congelar versión estable.** Registrar estado actual y pruebas de no regresión.
2. **Fase 1: memoria flat-file.** Implementar opciones, modelos, escritura JSONL y lectura Markdown opcional.
3. **Fase 2: Worker Service.** Crear servicio de fondo opcional y no intrusivo.
4. **Fase 3: comandos por JSON/tool calling.** Definir validación estricta y simulación antes de ejecutar.
5. **Fase 4: MQTT.** Añadir canal local solo con configuración explícita.
6. **Fase 5: NAS indexer.** Indexar inventario sin modificar archivos remotos.
7. **Fase 6: VPN/red.** Documentar y validar acceso seguro; no automatizar infraestructura sensible sin confirmación.
8. **Fase 7: serverless.** Evaluar offload puntual y webhooks externos.
