# Arquitectura

- `Program.cs`: composición de servicios y arranque por perfil.
- `Core/StartupProfile.cs`: decisiones de arranque.
- `Services/*`: motores, memoria, Telegram, TTS, logs, diagnóstico y utilidades.
- `Skills/*`: comandos y capacidades enrutable por intención.
- `Prompts/` y `personalidad_modular/`: material de personalidad y reglas.

Los servicios opcionales deben fallar de forma aislada y no tumbar el proceso general.
