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
- Hanna usa modo hÃ­brido
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

Telegram y autorizaciÃ³n:
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
- reproduce la canciÃ³n NOMBRE
- reproduce el Ã¡lbum NOMBRE
- pausa Spotify
- siguiente canciÃ³n

Voz y pantalla:
- F8: voz local sin ventana
- AltGr+Enter: voz local con ventana
- AltGr+Shift+H: voz local con ventana
- F9: analizar pantalla

CÃ¡mara:
- enciende cÃ¡mara
- apaga cÃ¡mara
- activa indicador de cÃ¡mara
- desactiva indicador de cÃ¡mara

Admin web:
- http://127.0.0.1:8787

API mÃ³vil:
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
                "Hanna usa modo hÃ­brido",
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
                "reproduce la canciÃ³n NOMBRE",
                "reproduce el Ã¡lbum NOMBRE",
                "pausa Spotify",
                "siguiente canciÃ³n"
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
                "enciende cÃ¡mara",
                "apaga cÃ¡mara",
                "activa indicador de cÃ¡mara",
                "desactiva indicador de cÃ¡mara"
            }
        };
    }
}
