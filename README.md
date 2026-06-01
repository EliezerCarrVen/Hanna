# Hanna

Hanna es un asistente C#/.NET con Telegram, motores IA, memoria, fases, Ollama, OpenRouter, Groq, Gemini, MongoDB, panel web, WebChat, TTS, Spotify, análisis de pantalla y automatización local.

## Estado

Proyecto en desarrollo activo con enfoque de entrega profesional. No se incluyen secretos reales en el repositorio.

## Arranque rápido

```powershell
dotnet restore Hanna/Hanna.csproj
dotnet build Hanna/Hanna.csproj --no-incremental
$env:HANNA_MODE="full"
dotnet run --project Hanna/Hanna.csproj
```

Perfiles soportados:

- `HANNA_MODE=full`: comportamiento completo.
- `HANNA_MODE=hybrid`: Telegram como canal principal, servicios opcionales por flags.
- `HANNA_MODE=telegram_only`: mínimo para Telegram texto, motores, memoria y comandos.

## Comandos profesionales

- `/status`, `/health`, `/diagnostico`, `/servicios`
- `/demo`, `/showcase`, `/resumen_sistema`, `/proyecto_estado`, `/siguiente_paso`
- `/logs`, `/errores`, `/ultimo_error`
- `/motor actual`, `/fase actual`

## Seguridad

Crea `HannaEnv.env` solo localmente. No subas tokens, logs, memoria local, bases locales ni archivos de credenciales.
