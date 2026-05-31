# Corrección Hanna V6.5 - motor, memoria y modismos

Cambios integrados:

1. Se agregó soporte real para `/motor gemini`, `/motor groq`, `/motor ollama`, `/motor openrouter`, `/motor hibrido`, `/motor original`, `/motor actual` y `/motores`.
2. Se corrigió el mensaje de Gemini para que no diga que es motor de respaldo cuando se selecciona como principal.
3. La fase local ya no responde automáticamente mostrando memoria local. La memoria se usa como contexto interno y el motor activo sigue respondiendo.
4. La memoria jerárquica filtra contenido interno: prompts, reglas, personalidad, Spotify interno, tokens, claves, pairing token y configuración.
5. La memoria deduplica por hash y por resumen normalizado.
6. Se agregó filtro de salida para reducir modismos mexicanos cuando `HANNA_MEXICANISMS_LEVEL=0`.
7. Se agregaron reglas de español neutro a `personalidad.txt` y al prompt modular.
8. Se mantiene `HANNA_WEBCHAT_ENABLED=false` porque el chat principal vive dentro del panel 8787.

Variables añadidas al `.env`:

```env
HANNA_LANGUAGE_STYLE=neutral
HANNA_MEXICANISMS_LEVEL=0
HANNA_DRAMA_LEVEL=20
HANNA_FORMALITY_LEVEL=70
HANNA_HUMOR_LEVEL=15
HANNA_BLOCK_INTERNAL_MEMORY_OUTPUT=true
```
