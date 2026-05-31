# Mejoras integradas en Hanna

## Incluido en esta versión

- Multi órdenes desde un mismo mensaje.
- Comando de interrupción: `Hanna para`, `Hanna detente`, `para de hablar`.
- Prioridad a mensaje nuevo mediante `InterruptionManager`.
- Archivos modulares:
  - `prompts_hanna/jarvis_rules.txt`
  - `prompts_hanna/modismos_mexicanos.txt`
  - `prompts_hanna/gustos_musicales.txt`
  - `prompts_hanna/spotify_playlists.txt`
  - `chat_profiles/owner/usuario.txt`
  - `hanna_self_knowledge/mejoras_solicitadas.txt`
- Contexto modular cargado en `ContextService`.
- Personalidad por chat con `PersonalityChatSkill`.
- Control de volumen por aplicación con `WindowsAudioSessionService`.
- Panel web con acceso a carpetas de personalidad, perfiles y self knowledge.
- Endpoint web para volumen por aplicación:
  - `GET /api/audio/sessions`
  - `POST /api/audio/app-volume`
- Si una imagen detecta canción y el caption pide reproducir/agregar, Telegram enlaza análisis visual con Spotify.

## Importante

No pude compilar aquí porque el entorno no tiene `dotnet` instalado. En tu equipo abre terminal en la carpeta `Hanna` y ejecuta:

```powershell
dotnet build
dotnet run
```

Si aparece error, pásame la captura exacta y lo corregimos sobre el proyecto real.
