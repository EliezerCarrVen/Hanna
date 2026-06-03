# Checklist de comportamiento original vs NodeLightweight

| Comportamiento original C# | Estado portado a Node | Pruebas que lo demuestran | Pendientes |
|---|---|---|---|
| Telegram usa router central y responde humano | partial | `npm test` valida `TelegramBotIntegration -> CommandRouter.run()` y `source=telegram` | Probar token real; voz/documentos pendientes |
| Texto normal no devuelve `unknown_command` | ported | `npm run once -- "hola"`, `npm test` | Fallback LLM real queda por configurar |
| Comandos slash funcionan | ported | `/status`, `/doctor`, `/deps`, `/auditoria verificar` | Skills C# avanzadas pendientes |
| Aliases naturales en español | ported | `estado`, `diagnostico`, `verifica auditoría`, `qué falta instalar` | Ampliar sinónimos |
| Respuesta humana por defecto | ported | `ResponseFormatterService`, pruebas manuales | Ninguno crítico |
| JSON explícito | ported | `/json /doctor`, `npm test` | Ninguno |
| Personalidad Hanna | partial | `hola`, `qué puedes hacer` | Cargar perfiles/personas completos sin exponer prompts |
| Memoria corta/persistente | ported | `guarda esto en memoria`, `busca en memoria`, `npm test` | Ranking avanzado |
| Rolling summary/índice | partial | `/summary`, `/indexar` | Índice jerárquico completo pendiente |
| Auditoría hash-chain | ported | `/auditoria verificar`, `npm test` | Más módulos avanzados |
| Diagnóstico real | ported | `/doctor`, `/deps` | Validación HP Mini física |
| Motor/fase | partial | `/motor actual`, `/fase actual` | Cambios persistentes y LLM real pendientes |
| Groq/Gemini/OpenRouter/Ollama | blocked_by_configuration | `npm test`, `/doctor` reporta configuración faltante | Adaptadores reales listos; requieren API keys/endpoints para generar |
| Spotify | blocked_by_configuration | `/spotify estado`, `estado de spotify`, `pausa spotify`, `siguiente canción`, `npm test` | Adapter Node funcional listo; llamadas reales requieren OAuth (`SPOTIFY_CLIENT_ID`, `SPOTIFY_CLIENT_SECRET`, `SPOTIFY_REDIRECT_URI`, `SPOTIFY_REFRESH_TOKEN`) y respetan dry-run |
| Admin Web/WebChat/Mobile API | ported | `npm run admin-web -- --self-test`, `npm run webchat -- --self-test`, `npm run mobile-api -- --self-test` | Interfaces mínimas HTTP funcionales; UI/JWT completo pendientes |
| Voz/TTS/hotkeys/desktop Windows | not_applicable_i386 | Matriz de paridad | No aplica al perfil HP Mini Debian i386 base |

## Elementos críticos

La experiencia crítica de Hanna como asistente NodeLightweight — Telegram texto, conversación natural, slash commands, ayuda, estado, diagnóstico, dependencias, auditoría, memoria, motor/fase, LLM adapters y HTTP mínimo para Admin/WebChat/Mobile — no queda en `missing_parity`. Lo restante está bloqueado solo por credenciales, dependencia externa, hardware o alcance de UI avanzada.
