# Dependencias Debian 12 para Hanna.Lightweight

## Obligatorias

- .NET SDK/runtime compatible con `net10.0`.
- `git` para despliegue desde repositorio.

## Recomendadas

```bash
sudo apt install ripgrep curl openssh-client iputils-ping iproute2
```

## Opcionales por módulo

```bash
sudo apt install clamav clamav-daemon
sudo apt install docker.io
sudo apt install nodejs npm
sudo npm install -g --unsafe-perm node-red
sudo apt install mosquitto mosquitto-clients
```

## Diagnóstico

```bash
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj -- --once "/deps"
dotnet run --project Hanna.Lightweight/Hanna.Lightweight.csproj -- --once "/doctor"
```

Si una dependencia falta, Hanna.Lightweight debe reportar `missing_dependency` y continuar.
