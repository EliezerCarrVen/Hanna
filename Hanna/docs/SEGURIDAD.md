# Seguridad

- `SecretSanitizer` oculta tokens, API keys, JWT, connection strings, passwords y rutas personales comunes.
- Los logs seguros se generan en `logs/`, carpeta ignorada por Git.
- `HannaEnv.env`, `.env`, memoria local, tokens y bases locales no deben versionarse.
- No guardar prompts internos completos si contienen secretos.

## Revisión manual

Ejecuta:

```powershell
scripts/Test-SecurityScan.ps1
```
