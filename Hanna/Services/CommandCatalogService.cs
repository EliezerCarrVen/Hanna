namespace Hanna.Services;

internal static class CommandCatalogService
{
    public static string GetCommandsText()
    {
        return """
COMANDOS ACTUALES DE HANNA

Motores:
- Hanna usa Ollama
- Hanna usa Groq
- Hanna usa Gemini
- Hanna usa OpenRouter
- Hanna usa modo híbrido
- /modo texto
- /modo audio
- /modo ambos

Personas:
- /personas
- /persona actual
- /senior o /architect
- /dev o /engineer
- /ops, /operator o /devops
- /analyst o /analista

Tokens:
- /tokens
- /tokens hoy
- /tokens texto TU_TEXTO
- /tokens archivo "C:\ruta\archivo.txt"

Telegram y autorización:
- /miid
- /auth
- /shadow
- /h
- /hd

Spotify:
- /spotify_status
- /dispositivos
- /dispositivo 1
- reproduce mi playlist NOMBRE
- reproduce la canción NOMBRE
- reproduce el álbum NOMBRE
- pausa Spotify
- siguiente canción

Voz y pantalla:
- F8: voz local sin ventana
- AltGr+Enter: voz local con ventana
- AltGr+Shift+H: voz local con ventana
- F9: analizar pantalla

Cámara:
- enciende cámara
- apaga cámara
- activa indicador de cámara
- desactiva indicador de cámara

Admin web:
- http://127.0.0.1:8787

API móvil:
- http://127.0.0.1:8790
""";
    }

    public static object GetCommandsJson()
    {
        return new
        {
            motores = new[]
            {
                "Hanna usa Ollama",
                "Hanna usa Groq",
                "Hanna usa Gemini",
                "Hanna usa OpenRouter",
                "Hanna usa modo híbrido",
                "/modo texto",
                "/modo audio",
                "/modo ambos"
            },
            personas = new[]
            {
                "/personas",
                "/persona actual",
                "/senior",
                "/architect",
                "/dev",
                "/engineer",
                "/ops",
                "/operator",
                "/devops",
                "/analyst",
                "/analista"
            },
            tokens = new[]
            {
                "/tokens",
                "/tokens hoy",
                "/tokens texto TU_TEXTO",
                "/tokens archivo \"C:\\ruta\\archivo.txt\""
            },
            telegram = new[]
            {
                "/h",
                "/hd",
                "/miid",
                "/auth",
                "/shadow"
            },
            spotify = new[]
            {
                "/spotify_status",
                "/dispositivos",
                "/dispositivo 1",
                "reproduce mi playlist NOMBRE",
                "reproduce la canción NOMBRE",
                "reproduce el álbum NOMBRE",
                "pausa Spotify",
                "siguiente canción"
            },
            vozPantalla = new[]
            {
                "F8 voz local sin ventana",
                "AltGr+Enter voz local con ventana",
                "AltGr+Shift+H voz local con ventana",
                "F9 analizar pantalla"
            },
            camara = new[]
            {
                "enciende cámara",
                "apaga cámara",
                "activa indicador de cámara",
                "desactiva indicador de cámara"
            }
        };
    }
}
