# Mejoras integradas: panel web, personalidad y verificación

## Archivos analizados antes de integrar
- Registro_Tecnico_Hanna_Eliezer.md
- Registro_Tecnico_Hanna_Eliezer.docx
- Capturas de configuración tipo ChatGPT enviadas por el usuario

## Cambios principales

### Panel web
Se agregaron secciones nuevas al Admin Web:
- Personalización
- Hanna
- Memoria
- Actualidad
- Dispositivos

Estas secciones siguen una lógica similar a configuración: estilo base, rasgos, instrucciones personalizadas, memoria, búsqueda, fuentes y módulos avanzados.

### Reglas de verdad
Se integró `prompts_hanna/reglas_verdad.txt` con reglas estrictas para evitar invenciones, pedir fuentes y declarar “No puedo confirmar esto” cuando no pueda verificar.

### Personalidad por chat
La personalidad de otros chats debe configurarse directamente desde ese chat. El panel solo administra el perfil del dueño y archivos globales de Hanna.

### Privacidad
El perfil del dueño está aislado en `chat_profiles/owner/usuario.txt`. Los demás chats deben usar `chat_profiles/{chatId}/`.

### Memoria técnica de Hanna
Se agregó el registro técnico como conocimiento interno para que Hanna pueda explicar su funcionamiento, mejoras, errores y estado.

### Actualidad
Se agregó editor de fuentes confiables en `prompts_hanna/trusted_sources.json`.

### Dispositivos
Se agregó panel preparado para skills de:
- volumen por aplicación
- Spotify al 30% sin bajar volumen general
- detectar TVs LG
- detectar pantallas
- clonar/extender pantalla

## Validación
No se pudo ejecutar `dotnet build` en este entorno porque no está instalado .NET SDK. Probar en Windows con:

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet run
```
