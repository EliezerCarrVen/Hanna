# Hanna WebChat V5

Chat online local para Hanna.

## Arranque

```powershell
node server.js
```

Luego abre:

```text
http://127.0.0.1:8789/
```

## Configuración

Edita:

```text
config/config.json
```

Campos importantes:

- `mobileApiBase`: URL de la API móvil de Hanna.
- `chatEndpoints`: endpoints que el proxy intentará usar para enviar mensajes.
- `engineEndpoints`: endpoints que intentará usar para cambiar motor.
- `engines`: lista desplegable de motores.
- `phases`: lista desplegable de fases.

No usa Express ni dependencias externas. Usa solo módulos internos de Node.js.
