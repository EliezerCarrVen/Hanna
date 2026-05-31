# Integración V6 dentro del proyecto

Funciones agregadas:

1. Motores estrictos: `HANNA_ENGINE_ALLOW_CROSS_FALLBACK=false` evita que Gemini salte a Groq/OpenRouter/Ollama sin permiso, y lo mismo para los demás motores.
2. TTS no bloqueante: `HANNA_SEND_TEXT_BEFORE_TTS=true` y `HANNA_TTS_BACKGROUND=true` mandan texto primero y generan audio después.
3. WebChat: `extras/hanna_v6/HannaWebChat` permite hablar desde navegador y cambiar motor/fase.
4. HP Mini profile: runtime configurado para Ollama local, wake word apagada, visión apagada y límites de contexto más bajos.
5. Mantenimiento nocturno: `extras/hanna_v6/Maintenance/hanna-maintenance.js` crea resumen diario, índice local y auditoría simple.
6. Drive/rclone: scripts en `Scripts/Backup-Hanna-GDrive.sh` y `Scripts/Run-HannaV6-Maintenance-Windows.ps1 -UploadToDrive`.
7. Entrenamiento: intents, feedback y ejemplos de skills quedan en `extras/hanna_v6`.

Limitaciones honestas:

- No se incluye una app Android terminada; queda API móvil y WebChat.
- TV LG requiere emparejamiento/puente webOS real.
- RAG vectorial completo no se activó para no meter dependencias pesadas; se dejó memoria ligera por índice/resúmenes.
- No pude validar compilación aquí porque el entorno no tiene `dotnet` instalado.
