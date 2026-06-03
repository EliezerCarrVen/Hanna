# Hotkeys para Hanna.NodeLightweight en Debian 12 i386

Esta fase recupera capacidades de escritorio sin módulos nativos de Node.js: voz con `espeak-ng`, grabación con `arecord`, captura con `scrot` y atajos externos con `xbindkeys`.

## Dependencias opcionales del sistema

Instálalas manualmente en la HP Mini si quieres activar estas funciones:

```bash
sudo apt install espeak-ng alsa-utils scrot xbindkeys
```

Hanna.NodeLightweight no ejecuta `sudo`, no modifica archivos del sistema y sigue arrancando aunque falte alguna herramienta. En ese caso los servicios devuelven `missing_dependency`.

## Comandos Hanna disponibles

- `/voz estado`: revisa `espeak-ng` y `arecord`.
- `/voz decir TEXTO`: envía texto sanitizado a `espeak-ng` en español.
- `/escuchar SEGUNDOS RUTA`: graba audio con `arecord`; si omites parámetros usa 5 segundos y `/tmp/hanna_record.wav`.
- `/pantalla estado`: revisa si `scrot` está disponible.
- `/pantalla capturar RUTA`: captura pantalla con calidad reducida; si omites ruta usa `/tmp/hanna_screen.jpg`.
- `/analizar_pantalla RUTA`: alias pensado para hotkeys; captura pantalla y devuelve ruta/base64 si funciona.

## Ejemplo de `~/.xbindkeysrc`

No lo crea Hanna automáticamente. Copia esto manualmente en tu usuario de Debian si quieres activar F8/F9:

```text
# F8 - Comando de voz
"curl -X POST http://localhost:8787/api/chat -H 'Content-Type: application/json' -d '{\"text\":\"/escuchar\"}'"
  F8

# F9 - Analizar Pantalla
"curl -X POST http://localhost:8787/api/chat -H 'Content-Type: application/json' -d '{\"text\":\"/analizar_pantalla\"}'"
  F9
```

Luego ejecuta `xbindkeys` en la sesión gráfica. Para hacerlo persistente, configura `xbindkeys` desde tu gestor de sesión o un servicio de usuario; no es necesario tocar systemd global.
