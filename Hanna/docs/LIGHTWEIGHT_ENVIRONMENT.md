# Configuración de entorno Hanna.Lightweight

## Fuentes de configuración

1. Valores por defecto en código.
2. `Hanna.Lightweight/appsettings.example.json` como plantilla sin secretos.
3. `Hanna.Lightweight/appsettings.local.json`, ignorado por Git.
4. Variables de entorno `HANNA_LIGHTWEIGHT_*`.

## Opciones principales

- `DataRoot`
- `DryRun`
- `RequireConfirmation`
- `AllowedNasRoots`
- `AllowedVaultImportRoots`
- `MqttBroker`, `MqttPort`, `MqttUsername`, `MqttUseTls`
- `DockerEnabled`, `ClamAvEnabled`
- `NodeRedBaseUrl`, `ServerlessWebhookUrl`
- `WolBroadcastAddress`
- `TailscaleExpected`, `NtpExpectedServer`, `PublicIpCheckEnabled`
- `MaxRotatedLogs`, `LogRetentionDays`

## Secretos

No guardar passwords, tokens ni API keys en appsettings. Usar variables de entorno o entrada interactiva cuando un módulo lo requiera. `SecretFilterService` redacta contenido sensible antes de persistir.
