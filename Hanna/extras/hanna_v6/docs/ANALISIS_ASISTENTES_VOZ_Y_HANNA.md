# Análisis de asistentes de voz y comparación con Hanna

## Referencias usadas

- Home Assistant Assist / Wyoming: arquitectura local por pipeline de STT, conversación y TTS.
- Rhasspy: enfoque offline, intents locales, MQTT/HTTP y control domótico.
- OpenVoiceOS: framework modular, orientado a privacidad y skills.
- Piper/Whisper/Ollama: pila local recomendada para no depender al 100% de APIs.
- LG webOS: control por puente local o Home Assistant; no se deja activo sin configuración.

## Comparación directa

| Sistema | Fuerte en | Debilidad | Qué se aplicó a Hanna |
|---|---|---|---|
| Home Assistant Assist | Pipeline local y domótica | Requiere configurar integraciones | Fase multimedia/OPS y futura TV LG por webhook |
| Rhasspy | Intents offline y privacidad | Configuración técnica | Dataset de intenciones local y comandos offline |
| OpenVoiceOS | Skills modulares | Más pesado de instalar | Skills separadas y fases por flujo |
| Alexa/Siri/Cortana | UX simple | Dependencia nube/cerrado | Frases naturales + ejecución local primero |
| Hanna | Control PC/Telegram/IA | Creció desordenada por parches | Se separan fases, motores estrictos y feedback |

## Decisiones aplicadas

1. Los motores se vuelven estrictos: Gemini directo usa `GEMINI_API_KEY`; OpenRouter solo se usa cuando el motor activo es OpenRouter.
2. La respuesta de texto se envía antes del TTS para reducir sensación de retraso.
3. TTS se genera en segundo plano cuando `HANNA_TTS_BACKGROUND=true`.
4. Se agregan fases en lugar de depender de “personas” como concepto visible.
5. Se agregan comandos offline básicos: hora, fecha, notas, tareas, lista y estado local.
6. Se prepara integración con TV LG mediante `tv_lg_config.json`, pero no se ejecuta hasta habilitarla.

## Nota de seguridad

No se copió código externo de GitHub dentro del motor de Hanna. Se añadieron adaptadores propios y documentación para evitar meter licencias, dependencias pesadas o código no auditado.
