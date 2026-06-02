# Modelo de Seguridad de Hanna.Lightweight

## Principios

1. Local-first y mínimo privilegio.
2. Sin secretos persistidos.
3. `DryRun=true` para módulos peligrosos.
4. Sin conexión con Hanna principal en esta fase.
5. Sin acciones de red o control externo reales.

## Datos prohibidos

Nunca persistir: `TELEGRAM_TOKEN`, `TELEGRAM_BOT_TOKEN`, `GEMINI_API_KEY`, `GROQ_API_KEY`, `OPENROUTER_API_KEY`, `SPOTIFY_CLIENT_SECRET`, `MYSQL_PASSWORD`, `HANNA_JWT_SECRET`, `HANNA_MOBILE_API_PAIRING_TOKEN`, contraseñas, prompts internos, system prompts, `HannaEnv` o configuraciones sensibles.

## Auditoría

La auditoría actual escribe eventos JSONL locales en `HannaData/logs/audit.log`. No es criptográficamente firmada. La auditoría inmutable y firmada queda planificada para una fase posterior.

## Módulos peligrosos

Requieren confirmación humana y revisión de PR antes de implementación real: MQTT, Wake-on-LAN, Docker, Node-RED, VPN, NAS, ClamAV, búnker cifrado, IoT, Serverless y cualquier ejecución de scripts.
