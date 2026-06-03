# Despliegue en HP Mini 110 Debian 12 i386

1. Instala Debian 12 i386/x86 con red funcional.
2. Verifica arquitectura: `dpkg --print-architecture` debe indicar `i386`.
3. Verifica SSE2: `grep -i sse2 /proc/cpuinfo`.
4. Clona o copia el repositorio.
5. Ejecuta:

```bash
cd Hanna.NodeLightweight
./scripts/install-debian12-i386.sh
npm run self-test
npm run once -- "/doctor"
```

## Systemd opcional

Edita `systemd/hanna-node.service` si tu ruta no es `/opt/hanna/Hanna.NodeLightweight`, copia el servicio y habilítalo:

```bash
sudo cp systemd/hanna-node.service /etc/systemd/system/hanna-node.service
sudo systemctl daemon-reload
sudo systemctl enable --now hanna-node
```

## Seguridad

No guardes secretos en git. Usa `.env` local o variables de entorno. Los módulos peligrosos arrancan en dry-run.
