#!/usr/bin/env sh
set -eu
ARCH="$(dpkg --print-architecture 2>/dev/null || uname -m)"
echo "Arquitectura detectada: $ARCH"
case "$ARCH" in i386|i686|x86) echo "OK: arquitectura i386/x86." ;; *) echo "WARN: script pensado para Debian 12 i386/x86; detectado $ARCH." ;; esac
if [ -r /proc/cpuinfo ]; then
  if grep -qi sse2 /proc/cpuinfo; then echo "OK: CPU reporta SSE2."; else echo "WARN: no se detectó SSE2; Node.js puede no arrancar en CPUs muy viejos."; fi
fi
echo "Este instalador no guarda secretos ni modifica BIOS/sistema sin confirmación."
printf "¿Instalar dependencias apt mínimas (nodejs npm git ripgrep curl iproute2 iputils-ping clamav mosquitto-clients)? [y/N] "
read ans
if [ "${ans:-N}" = "y" ] || [ "${ans:-N}" = "Y" ]; then
  sudo apt update
  sudo apt install -y nodejs npm git ripgrep curl iproute2 iputils-ping clamav mosquitto-clients hostname
fi
printf "¿Ejecutar npm install en Hanna.NodeLightweight? [y/N] "
read ans
if [ "${ans:-N}" = "y" ] || [ "${ans:-N}" = "Y" ]; then npm install; fi
printf "¿Crear usuario local opcional 'hanna' si no existe? [y/N] "
read ans
if [ "${ans:-N}" = "y" ] || [ "${ans:-N}" = "Y" ]; then id hanna >/dev/null 2>&1 || sudo useradd --system --create-home --shell /usr/sbin/nologin hanna; fi
printf "¿Instalar servicio systemd hanna-node.service? [y/N] "
read ans
if [ "${ans:-N}" = "y" ] || [ "${ans:-N}" = "Y" ]; then sudo cp systemd/hanna-node.service /etc/systemd/system/hanna-node.service; sudo systemctl daemon-reload; echo "Servicio instalado; habilita con: sudo systemctl enable --now hanna-node"; fi
echo "Listo. Configura variables locales en .env si necesitas MQTT/NAS/Vault/TOTP."
