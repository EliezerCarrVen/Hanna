# Plan de entrenamiento Hanna V5

## Objetivo

Hanna debe mejorar por etapas sin depender totalmente de APIs web. Para una HP Mini 110, la prioridad no es entrenar redes neuronales grandes localmente, sino usar una arquitectura ligera:

1. Detección local de intención.
2. Reglas y fuzzy matching.
3. Skills locales.
4. RAG/memoria antes del LLM.
5. LLM solo si la tarea no se resuelve localmente.
6. Retroalimentación guardada para mejorar ejemplos y comandos.

## 1. Manejo de datos personalizados

Guardar comandos reales del usuario, correcciones, preferencias y ejemplos en archivos JSONL o SQLite.

Ejemplo:

```json
{"intent":"media.spotify.play","text":"Hanna pon mi playlist de estudio","result":"ok","rating":1}
```

## 2. Aprendizaje profundo y redes neuronales

Para la HP Mini no se recomienda entrenar CNN/RNN localmente. Se deja como fase externa:

- Entrenar o ajustar modelos en una PC más fuerte o nube.
- Exportar modelos pequeños o reglas aprendidas.
- Usar en Hanna solo la inferencia ligera.

## 3. Aprendizaje por refuerzo

Aplicarlo como retroalimentación simple:

- `+1` si el comando funcionó.
- `-1` si falló.
- Penalizar acciones lentas o motores incorrectos.
- Premiar comandos resueltos sin internet.

## 4. Aprendizaje basado en muestras

Cada comando nuevo debe guardarse como muestra de entrenamiento:

- texto original
- intención detectada
- acción ejecutada
- motor usado
- fase activa
- resultado
- calificación del usuario

## 5. Optimización de hiperparámetros

En Hanna se traduce a ajustar valores operativos:

- umbral de fuzzy matching
- máximo de tokens
- motor por fase
- longitud máxima de TTS
- cada cuántos mensajes resumir contexto
- cuántos documentos revisar antes del LLM

## 6. Regularización y prevención de sobreajuste

No hacer que Hanna memorice una sola forma de pedir algo. Cada intent debe tener variaciones.

Ejemplo:

- “abre Netflix”
- “pon Netflix”
- “abre la plataforma de Netflix”
- “busca esta serie en Netflix”

## 7. Monitoreo continuo y ajuste

Crear logs de:

- tiempo de respuesta
- motor usado
- tokens aproximados
- si usó internet o no
- si usó TTS
- si el usuario corrigió la respuesta

## 8. Aprendizaje continuo e IA explicativa

Hanna debe poder explicar por qué eligió una acción:

```text
Usé Ollama porque estás en fase local y el comando fue reconocido como intent multimedia.
```

## 9. Integración con otros modelos y sistemas

Mantener rutas separadas:

- Gemini directo
- Groq directo
- OpenRouter explícito
- Ollama local
- skills locales
- APIs internas

## 10. Experimentación y pruebas A/B

Comparar configuraciones:

- TTS antes vs TTS en segundo plano
- fuzzy threshold 0.70 vs 0.80
- Ollama vs Gemini para estudio
- local intents antes vs LLM directo

## Resultado esperado

Hanna debe sentirse más rápida porque no manda todo al LLM. Debe ejecutar comandos comunes localmente y solo pedir ayuda a modelos externos cuando la tarea lo justifique.
