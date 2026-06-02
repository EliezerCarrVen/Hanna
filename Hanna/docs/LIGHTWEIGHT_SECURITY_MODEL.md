# Modelo de Seguridad de Hanna.Lightweight

## Estado

Hanna.Lightweight mantiene seguridad local-first: no se conecta con Hanna principal, no ejecuta acciones externas reales y conserva módulos peligrosos en `DryRun=true` o `planned_not_implemented`.

## Datos prohibidos

Nunca persistir: `TELEGRAM_TOKEN`, `TELEGRAM_BOT_TOKEN`, `GEMINI_API_KEY`, `GROQ_API_KEY`, `OPENROUTER_API_KEY`, `SPOTIFY_CLIENT_SECRET`, `MYSQL_PASSWORD`, `HANNA_JWT_SECRET`, `HANNA_MOBILE_API_PAIRING_TOKEN`, contraseñas, prompts internos, system prompts, `HannaEnv` o configuraciones sensibles.

## PathGuard

`PathGuardService` protege todas las escrituras nuevas de Hanna.Lightweight. Bloquea rutas absolutas externas, rutas con `..`, rutas vacías, `.env`, `HannaEnv`, appsettings sensibles, configuraciones con secretos y cualquier destino fuera de `HannaData/`. Los bloqueos generan eventos seguros en `security.log` y `audit.log` sin registrar secretos ni rutas sensibles completas.

## SecretFilter

`SecretFilterService` redacta `api_key=`, `apikey=`, `token=`, `bearer`, `password=`, `pwd=`, `secret=`, `client_secret=`, `refresh_token=`, nombres de tokens conocidos, conexiones MySQL/Postgres con password, URLs con credenciales, JWT compactos, prefijos `sk-or-v1`, `gsk_`, `AIza` y cadenas largas tipo token. El contenido sensible se reemplaza por `[REDACTED]`. Cuando redacta algo, registra un evento en `security.log` sin guardar el valor original.

## Log rotation

`LogRotationService` rota `lightweight.log`, `audit.log` y `security.log` al superar `MaxLogFileBytes`, renombrando a `nombre.yyyyMMddHHmmss.log`. No borra logs en esta fase.

## Auditoría

`AuditLogService` registra comandos ejecutados, self-test, doctor, PathGuard, redacciones indirectas, summary, indexación, creación de notas y acciones dry-run. La auditoría firmada criptográficamente sigue `planned_not_implemented`.

## Confirmación humana requerida

MQTT, Wake-on-LAN, Docker, Node-RED, VPN, NAS, ClamAV, búnker cifrado, IoT, Serverless y ejecución de scripts requieren confirmación humana y PR futura antes de implementación real.
