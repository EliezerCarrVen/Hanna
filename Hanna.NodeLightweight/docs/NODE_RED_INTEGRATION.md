# Integración Node-RED

Node-RED es un orquestador opcional. Hanna.NodeLightweight no depende de Node-RED para arrancar.

## Instalar

```bash
sudo npm install -g --unsafe-perm node-red
```

## Servicio opcional

El archivo `systemd/nodered.service` ofrece una plantilla para ejecutar Node-RED con usuario `hanna`.

## Flows

`nodered/flows.example.json` incluye un flujo mínimo de ejemplo. Importa el JSON desde la interfaz de Node-RED y adapta nodos HTTP/MQTT según tu red local.
