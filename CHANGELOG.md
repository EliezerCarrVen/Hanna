# Changelog

## Unreleased

- Agregado saneamiento centralizado con `SecretSanitizer` para respuestas, logs y previsualizaciones.
- Agregados logs seguros por canal en `logs/*.log` generados en runtime.
- Agregados comandos profesionales de diagnóstico: `/status`, `/health`, `/diagnostico`, `/servicios`, `/demo`, `/showcase`, `/resumen_sistema`, `/proyecto_estado`, `/siguiente_paso`, `/logs`, `/errores`, `/ultimo_error`, `/costo`, `/presupuesto`, `/limite`, `/modelos`.
- Agregados scripts seguros de prueba en `scripts/`.
- Documentada revisión de vulnerabilidades. Se agregó referencia directa a `SharpCompress` 0.48.0 para forzar una versión posterior a la transitive 0.30.1 reportada por NU1902.
