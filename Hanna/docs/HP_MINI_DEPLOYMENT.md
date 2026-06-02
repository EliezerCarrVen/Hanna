# Despliegue Hanna.Lightweight en HP Mini 110

## Objetivo

Instalar Hanna.Lightweight como runtime local-first en hardware limitado. Hanna principal permanece separada en `Hanna/Hanna.csproj`.

## Pasos base Debian 12

```bash
sudo apt update
sudo apt install -y git curl ripgrep
# instalar .NET SDK/runtime desde Microsoft según arquitectura disponible
# opcional: sudo apt install -y clamav clamav-daemon docker.io nodejs npm mosquitto mosquitto-clients openssh-client iputils-ping
# opcional Node-RED: sudo npm install -g --unsafe-perm node-red
```

## Ejecución

```bash
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj -- --self-test
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj -- --once "/doctor"
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj
```

## HannaData recomendado

Usar un directorio local con permisos del usuario que ejecuta Hanna, por ejemplo `/opt/hanna-data` o `~/HannaData`. Configurarlo en `Hanna.Lightweight/appsettings.local.json` o con `HANNA_LIGHTWEIGHT_DATAROOT`. No subir `HannaData/` a GitHub.

## systemd opcional

Crear un servicio que ejecute `dotnet run --project ... -- --self-test` para verificación o publicar el binario con `dotnet publish` cuando el SDK esté disponible. Mantener `DryRun=true` hasta validar dependencias.

## BIOS/failsafe

El failsafe eléctrico no se modifica por software. Revisar manualmente si la BIOS ofrece `Restore on AC Power Loss` y documentar el estado con `/failsafe estado`.
