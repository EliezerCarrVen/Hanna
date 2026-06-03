# Migración desde Hanna .NET

`Hanna.NodeLightweight` es una implementación paralela. No reemplaza ni modifica `Hanna/Program.cs` ni `Hanna/Hanna.csproj`.

## Equivalencias

- Memoria corta: JSONL en `HannaData/runtime/short_memory.jsonl`.
- Vault: Markdown en `HannaData/vault/`.
- Auditoría: JSONL con hash-chain en `HannaData/logs/audit.log`.
- Módulos Enterprise peligrosos: adaptadores Node con `dry_run=true` por defecto.

## Recomendación

Mantén Hanna C# para PC moderna y usa Hanna.NodeLightweight como runtime real para HP Mini i386. Usa documentación y comandos comunes para validar ambos mundos sin mezclar secretos ni rutas personales.
