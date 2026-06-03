# Dependencias Debian 12 i386/x86

## Mínimas

```bash
sudo apt update
sudo apt install -y nodejs npm git curl hostname iproute2 iputils-ping
```

## Recomendadas

```bash
sudo apt install -y ripgrep clamav mosquitto-clients systemd
```

## Opcionales

- Docker: `sudo apt install docker.io` si la máquina lo soporta.
- Node-RED: `sudo npm install -g --unsafe-perm node-red`.
- MQTT broker local: `sudo apt install mosquitto`.

## Estados esperados

Si falta una dependencia, Hanna.NodeLightweight reporta `missing_dependency`. Si falta broker, contraseña de vault, secreto TOTP o rutas NAS, reporta `missing_configuration`.
