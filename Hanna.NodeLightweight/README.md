# Hanna.NodeLightweight

Versión paralela en Node.js/JavaScript directo para ejecutar Hanna en HP Mini 110 con Debian 12 i386/x86. No reemplaza a `Hanna/` ni a `Hanna.Lightweight/`.

## Principios

- Runtime: Node.js/npm de Debian; no requiere .NET.
- Dependencias npm: ninguna por defecto.
- Persistencia local: `../HannaData/` con JSONL y Markdown.
- Seguridad: filtros de secretos, PathGuard, audit log con hash-chain y módulos peligrosos en `dry_run=true`.
- Integraciones externas opcionales: ripgrep, Mosquitto/MQTT, ClamAV, Docker, Node-RED, systemd.

## Uso

```bash
npm install
npm start
npm run self-test
npm run once -- "/status"
npm run once -- "/doctor"
npm run once -- "/deps"
npm test
```

## Comandos CLI

Ejecuta `npm run once -- "/help"` para listar todos los comandos. Los módulos de red/despliegue reportan `missing_configuration`, `missing_dependency`, `service_unavailable` o `missing_hardware_or_network` en lugar de crashear.
