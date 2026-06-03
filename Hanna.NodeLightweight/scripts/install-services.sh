#!/usr/bin/env sh
set -eu
ROOT="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
echo "Instalación opcional de servicios systemd de Hanna.NodeLightweight"
echo "No se copian secretos ni .env. Ajusta WorkingDirectory si no instalas en /opt/hanna."
if [ "$(id -u)" -ne 0 ]; then echo "Ejecuta con sudo para instalar systemd."; exit 1; fi
cp "$ROOT/systemd/hanna-core.service" /etc/systemd/system/hanna-core.service
cp "$ROOT/systemd/hanna-telegram.service" /etc/systemd/system/hanna-telegram.service
cp "$ROOT/systemd/hanna-web.service" /etc/systemd/system/hanna-web.service
systemctl daemon-reload
systemctl enable hanna-core hanna-telegram hanna-web
echo "Servicios habilitados. Usa scripts/start-all.sh para arrancar."
