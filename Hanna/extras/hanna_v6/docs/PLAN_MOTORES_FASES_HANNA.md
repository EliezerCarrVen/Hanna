# Plan de motores y fases para Hanna

## Regla principal

El motor elegido por el usuario se respeta.

- `Hanna usa Gemini` -> Gemini directo con `GEMINI_API_KEY`.
- `Hanna usa Groq` -> Groq directo con `GROQ_API_KEY`.
- `Hanna usa Ollama` -> Ollama local.
- `Hanna usa OpenRouter` -> OpenRouter y presupuesto diario.

`HANNA_ENGINE_ALLOW_CROSS_FALLBACK=false` evita que Hanna cambie de motor automáticamente cuando falla una API.

## Fases

Las fases no reemplazan motores; configuran flujo de trabajo.

- `local`: máximo offline, bajo consumo.
- `estudio`: claridad y repaso.
- `programacion`: código y debugging.
- `ops`: sistema, logs y automatización segura.
- `multimedia`: música, video, PC/TV.
- `ahorro`: minimiza tokens y gasto.
- `nube`: permite APIs web respetando motor elegido.
- `architect`: prepara perfil de arquitectura, pero no activa OpenRouter solo.

## Latencia TTS

Para que el usuario no espere el audio:

```env
HANNA_SEND_TEXT_BEFORE_TTS=true
HANNA_TTS_BACKGROUND=true
HANNA_TTS_MAX_CHARS=650
```

Con esto Hanna manda el texto primero y el audio llega después.
