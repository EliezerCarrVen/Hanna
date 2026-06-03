# Paridad con Hanna original C#

Esta auditoría usa la Hanna C# como especificación funcional para `Hanna.NodeLightweight`. La versión Node existe porque Debian 12 i386/i686 en HP Mini 110 no puede usar .NET moderno como runtime principal.

## Auditoría funcional C# revisada

- `Hanna/Program.cs`: compone configuración, runtime settings, Ollama daemon, Mongo logs, almacenamiento, personalidad, contexto, prompts, motores Groq/Gemini/Ollama/OpenRouter, fase, memoria jerárquica, auditoría, respuesta, Spotify, Telegram, hotkeys, Admin Web, WebChat y Mobile API.
- `Hanna/Core/AppConfig.cs`: carga variables de entorno, rutas, tokens, flags de módulos, puertos, motores, Telegram, Admin Web, Mobile API y WebChat.
- `Hanna/Core/StartupProfile.cs`: decide qué módulos arrancan según perfil, configuración y credenciales.
- `Hanna/Services/TelegramService.cs`: canal principal; procesa texto, comandos, archivos/voz, llama `SkillRouter`, registra conversación y evita exponer fallos técnicos.
- `Hanna/Skills/SkillRouter.cs`: enruta comandos slash, multi-comandos, texto natural, name guard, auditoría y fallback a `GeneralChatSkill`.
- `Hanna/Skills/SystemSkill.cs`: ayuda, modo de respuesta, comandos de sistema, auth/status Spotify y utilidades.
- `Hanna/Services/HannaPersonaService.cs`: carga personas/prompts y arma identidad/reglas de verdad/eficiencia edge.
- `Hanna/Services/MemoryService.cs` y `TieredMemoryService.cs`: memoria local, búsqueda, índice y contexto recuperado.
- `GroqService`, `GeminiService`, `OpenRouterService`, `OllamaService`: motores LLM por configuración.
- `ResponseService`: modo texto/audio/ambos y respuesta al usuario.
- `Spotify*`, `AdminWebServerService`, `WebChatHostService`, `MobileApiServerService`, TTS/audio/hotkeys: funciones avanzadas o Windows/credenciales.

## Matriz de paridad

| Original C# feature | Archivo C# | Qué hace realmente | Comportamiento esperado en Node | Estado Node | Archivo Node | Prueba que lo valida | Pendiente |
|---|---|---|---|---|---|---|---|
| Arranque modular | `Hanna/Program.cs` | Crea servicios y decide canales/módulos | Crear HannaData, arrancar CLI/Telegram sin .NET | partial | `src/index.js`, `src/services/startupService.js`, `src/services/startupProfileService.js` | `npm run self-test`, `/status` | Falta orquestación completa de todos los servicios C# |
| Configuración | `Hanna/Core/AppConfig.cs` | Env vars, rutas, flags, tokens | Snapshot seguro sin secretos | partial | `src/services/appConfigService.js`, `src/core/config.js` | `/doctor` | Más variables C# por portar |
| Startup profile | `Hanna/Core/StartupProfile.cs` | Decide módulos por perfil/credenciales | Plan start/blocked/dry_run | partial | `src/services/startupProfileService.js` | `npm test` indirecto | Perfiles avanzados pendientes |
| Telegram principal | `Hanna/Services/TelegramService.cs` | Texto/comandos vía router, errores seguros | Polling Telegram, context, auditoría, human mode | partial | `src/integrations/telegramBot.js`, `src/services/telegramSecurityService.js` | `npm test` adapter | Voz/documentos/imágenes pendientes |
| Router de skills | `Hanna/Skills/SkillRouter.cs` | Slash, natural, fallback general | `CommandRouter.run/handle`, aliases, fallback local/LLM | partial | `src/cli/commandRouter.js`, `src/services/naturalCommandService.js` | `npm test`, frases manuales | Skills especializadas restantes |
| SystemSkill/help | `Hanna/Skills/SystemSkill.cs` | Ayuda, status, modo, utilidades | `/help`, `/status`, `/doctor`, `/deps`, `/modulos` y `/spotify ...` humanos | partial | `src/cli/commands.js`, `src/services/responseFormatterService.js` | `/help`, `/status`, `/doctor`, `/spotify estado` | Descargas/utilidades avanzadas pendientes |
| Personalidad | `Hanna/Services/HannaPersonaService.cs` | Carga personas/prompts | Identidad Hanna y capacidades reales sin exponer prompts | partial | `src/services/personaService.js` | `hola`, `qué puedes hacer` | Personas completas pendientes |
| Memoria corta | `Hanna/Services/MemoryService.cs` | Guardar/buscar recuerdos | JSONL sanitizado | ported | `src/services/shortMemoryService.js`, `src/services/memoryService.js` | `guarda...`, `busca...`, `npm test` | Mejor scoring |
| Memoria jerárquica | `TieredMemoryService.cs` | Índice, SQLite/JSONL, contexto | Markdown vault + búsqueda + rolling summary | partial | `src/services/tieredMemoryService.js`, `markdownVaultService.js` | `/summary`, `/indexar` | SQLite remoto/nocturno no portado |
| Auditoría | `AuditTrailService.cs` | Registro de acciones | JSONL hash-chain con source/user/chat/original/normalized | ported | `src/services/auditLogService.js` | `/auditoria verificar`, `npm test` | Más eventos por módulo avanzado |
| Sanitización/logs | logging/sanitizers C# | No filtrar secretos | SecretFilter + ZeroLeak + SafeLog | ported | `secretFilterService.js`, `zeroLeakSanitizerService.js`, `safeLogService.js` | `npm test` | Nuevos patrones conforme crezcan módulos |
| Diagnóstico | Startup/diagnósticos | Sistema, dependencias, config | Doctor real con system/deps/runtime/memoria/auditoría/motor/fase | ported | `doctorService.js`, `runtimeStatusService.js`, `diagnosticsService.js` | `/doctor`, `/deps` | Validar en HP Mini física |
| Motor | `ModelModeService.cs` | Motor activo/cambio | Estado y cambio dry-run seguro | partial | `engineStateService.js`, `llmRouterService.js` | `/motor actual` | Persistencia y selección real pendiente |
| Fase | `PhaseService.cs`, `PhaseSkill.cs` | Fase activa/cambio | Estado y cambio dry-run seguro | partial | `phaseStateService.js` | `/fase actual` | Persistencia pendiente |
| Groq/Gemini/OpenRouter/Ollama | `*Service.cs` | LLM externo/local | Adaptadores reales con `status()`, `isConfigured()` y `generate()`; sin claves reportan `missing_configuration` | blocked_by_configuration | `groqAdapterService.js`, `geminiAdapterService.js`, `openRouterAdapterService.js`, `ollamaAdapterService.js`, `llmRouterService.js` | `npm test`, `/json /doctor` | Requiere API keys/endpoints para generar; sin credenciales usa fallback local |
| Spotify | `Hanna/Spotify/*`, `SpotifySkill.cs` | OAuth, estado auth, búsqueda y playback | Adapter funcional con refresh token, búsqueda, play/pause/next/previous y dry-run seguro si no hay credenciales | blocked_by_configuration | `src/services/spotifyService.js`, `src/cli/commandRouter.js`, `src/services/naturalCommandService.js`, `src/services/responseFormatterService.js`, `src/services/doctorService.js` | `/spotify estado`, `estado de spotify`, `pausa spotify`, `siguiente canción`, `npm test` | Requiere `SPOTIFY_CLIENT_ID`, `SPOTIFY_CLIENT_SECRET`, `SPOTIFY_REDIRECT_URI` y `SPOTIFY_REFRESH_TOKEN` para llamadas reales; sin credenciales no guarda ni loggea secretos |
| Admin Web | `AdminWebServerService.cs` | Panel local | HTTP nativo con `/health`, `/status`, `/doctor`, `/modules`, `POST /chat`, auth token opcional | ported | `src/integrations/adminWebServer.js` | `npm run admin-web -- --self-test`, `npm test` | UI completa visual pendiente; API mínima funcional |
| WebChat | `WebChatHostService.cs`, `HannaWebUI/` | Chat web | HTTP nativo con `GET /` y `POST /chat` usando `CommandRouter.run()` | ported | `src/integrations/webChatServer.js` | `npm run webchat -- --self-test`, `npm test` | UI rica pendiente; chat mínimo funcional |
| Mobile API | `MobileApiServerService.cs` | API JWT móvil | HTTP nativo con `/api/health`, `/api/status`, `POST /api/chat`, token opcional | ported | `src/integrations/mobileApiServer.js` | `npm run mobile-api -- --self-test`, `npm test` | JWT completo C# pendiente; token local opcional funcional |
| TTS/voz/hotkeys | `TtsService`, `VoiceCommandService`, hotkeys | Voz/audio/atajos Windows | No aplicar por defecto en i386 | not_applicable_i386 | docs | Checklist | Solo si hardware/paquetes i386 lo permiten |
| Skills Windows/escritorio | `DesktopSkill`, audio/media | Control PC Windows | No aplicable HP Mini Debian | not_applicable_i386 | docs | Checklist | Ninguno para i386 base |
| NAS/MQTT/WOL/ClamAV/Docker/Node-RED | varios planes lightweight | Automatización opcional | Contratos seguros con dry-run/missing_dependency/config | partial | `mqttService.js`, `wakeOnLanService.js`, `nasIndexerService.js`, etc. | `/modulos`, `/doctor` | Integración real depende de paquetes/config/hardware |
