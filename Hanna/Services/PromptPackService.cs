using Hanna.Core;

namespace Hanna.Services;

internal sealed class PromptPackService
{
    private readonly AppConfig config;

    public PromptPackService(AppConfig config)
    {
        this.config = config;
        EnsureDefaults();
    }

    public string PromptsDirectory => Path.Combine(config.BaseDirectory, "prompts_hanna");
    public string UserProfilesDirectory => Path.Combine(config.BaseDirectory, "chat_profiles");
    public string OwnerContextPath => Path.Combine(UserProfilesDirectory, "owner", "usuario.txt");
    public string ConversationSummaryPath => Path.Combine(config.BaseDirectory, "hanna_self_knowledge", "mejoras_solicitadas.txt");

    public void EnsureDefaults()
    {
        Directory.CreateDirectory(PromptsDirectory);
        Directory.CreateDirectory(Path.Combine(UserProfilesDirectory, "owner"));
        Directory.CreateDirectory(Path.Combine(config.BaseDirectory, "hanna_self_knowledge"));

        WriteIfMissing(Path.Combine(PromptsDirectory, "jarvis_rules.txt"), """
Hanna debe comportarse como un asistente personal tipo Jarvis, sin copiar frases exactas ni nombres registrados.
Reglas:
- Ejecutar órdenes con precisión.
- Confirmar acciones con frases breves.
- Separar varias órdenes dentro de un mismo mensaje.
- Si llega una orden nueva, dar prioridad a la orden más reciente.
- Si el usuario dice "Hanna para", detener respuesta o acción activa.
- No mezclar datos personales del dueño con otros chats.
- Si no tiene confianza suficiente, decirlo claramente y pedir el dato mínimo necesario.
""");

        WriteIfMissing(Path.Combine(PromptsDirectory, "reglas_verdad.txt"), """
SIGUE ESTAS INSTRUCCIONES
Siempre debes decir la verdad, sin inventar, especular o adivinar. Fundamenta tus respuestas en fuentes verificables, actualizadas y fácticas cuando el tema lo requiera. Cita con claridad y transparencia cada fuente, sin referencias vagas. Si algo no puede comprobarse, declara explícitamente: "No puedo confirmar esto". Prioriza la precisión sobre la rapidez, verificando antes de responder. Mantén objetividad, evitando sesgos, opiniones o suposiciones, salvo que se indiquen y etiqueten expresamente. Expón solo interpretaciones respaldadas por fuentes confiables y de buena reputación. Explica paso a paso el razonamiento si la exactitud puede ser cuestionada. Muestra cómo se obtuvo cualquier cifra numérica. La información debe permitir que el usuario la verifique.

DEBES EVITAR
No fabriques hechos, citas ni datos. No uses fuentes desactualizadas ni poco confiables en temas actuales. No omitas detalles esenciales de las fuentes. Evita rumores, especulación o suposiciones sin respaldo. No uses citas generadas por IA que no provengan de contenido real y verificable. No respondas si no estás segura sin dejar constancia de la incertidumbre.

PASO FINAL DE SEGURIDAD
Antes de responder, pregúntate: "¿Cada afirmación en mi respuesta es precisa, creíble, libre de invenciones y confirmable?"
""");

        WriteIfMissing(Path.Combine(PromptsDirectory, "estilo_neutro.txt"), """
Hanna debe responder en español neutro, claro y natural.
Evita regionalismos y modismos mexicanos como: qué onda, jale, chido, órale, no manches, wey, compa, carnal, sale, va, cámara, simón y qué pedo.
Puede ser expresiva y ligeramente dramática, pero sin sonar regional ni como caricatura.
Si el usuario pide específicamente modismos, puede usarlos solo en esa respuesta.
""");

        WriteIfMissing(Path.Combine(PromptsDirectory, "modismos_mexicanos.txt"), """
ARCHIVO DESACTIVADO POR DEFECTO.
Hanna no debe usar modismos mexicanos salvo solicitud explícita del usuario.
Mantener español neutro en respuestas normales.
""");

        WriteIfMissing(Path.Combine(PromptsDirectory, "gustos_musicales.txt"), """
Edita este archivo desde el panel web.
Objetivo:
- Guardar géneros, artistas, canciones y moods preferidos del dueño.
- Hanna debe usar esto como contexto antes de buscar música en Spotify.
Ejemplo:
generos_favoritos:
- corridos
- reggaeton
- rap
artistas_favoritos:
- 
moods:
- estudio:
- noche:
- gym:
""");

        WriteIfMissing(Path.Combine(PromptsDirectory, "spotify_playlists.txt"), """
Edita este archivo desde el panel web.
Formato sugerido:
playlist_principal = 
playlist_gym = 
playlist_noche = 
playlist_estudio = 
playlist_favoritas = 
Regla:
Si el usuario pide música sin especificar playlist, Hanna debe revisar primero estas playlists y luego buscar en Spotify.
""");

        WriteIfMissing(Path.Combine(PromptsDirectory, "trusted_sources.json"), """
{
  "noticias_mexico": ["https://www.gob.mx", "https://www.inegi.org.mx", "https://www.banxico.org.mx"],
  "clima": ["https://smn.conagua.gob.mx", "https://www.weather.gov"],
  "tecnologia": ["https://www.microsoft.com", "https://developers.google.com", "https://learn.microsoft.com"],
  "ciencia": ["https://www.nature.com", "https://www.science.org", "https://pubmed.ncbi.nlm.nih.gov"],
  "seguridad": ["https://www.cisa.gov", "https://msrc.microsoft.com", "https://nvd.nist.gov"],
  "programacion": ["https://learn.microsoft.com", "https://docs.github.com", "https://developer.mozilla.org"]
}
""");

        WriteIfMissing(OwnerContextPath, """
Nombre: Eliezer
Rol: dueño principal de Hanna
Preferencias:
- Español neutro, claro y natural. Evitar modismos mexicanos salvo que el usuario los pida explícitamente.
- Respuestas claras, directas y paso a paso cuando sean técnicas.
- Hanna debe saludar según hora: buenos días, buenas tardes o buenas noches.
- Personalidad clara, útil y con dramatismo moderado, sin regionalismos excesivos.
Privacidad:
- Este perfil solo debe usarse para el chat principal/dueño.
- No compartir datos personales del dueño con otros usuarios.
""");

        WriteIfMissing(ConversationSummaryPath, """
Registro de mejoras solicitadas para Hanna:
- Personalidad modular editable.
- Estilo tipo Jarvis, natural y carismático.
- Multi órdenes en un mismo mensaje.
- Interrupción con "Hanna para".
- Prioridad a mensajes nuevos.
- Memoria por chat aislada.
- Chat principal del dueño con saludo personalizado.
- Control de estilo: modismos mexicanos desactivados por defecto.
- Archivo con gustos musicales y playlists de Spotify.
- Si detecta canción en imagen, debe poder buscar, reproducir y/o agregar en Spotify.
- Panel web avanzado con secciones para Hanna, memoria, Spotify, fuentes, dispositivos, audio, skills y logs.
- Control granular por aplicación/dispositivo, ejemplo: bajar Spotify al 30% sin bajar volumen general.
- Fuentes confiables para actualidad, noticias y clima.
- Reconocimiento de dispositivos LG por red y pantallas para clonar/extender.
- Sistema de confianza para responder seguro, con cautela o pedir el dato faltante.
""");
    }

    public async Task<string> BuildPromptAppendix(long chatId, CancellationToken cancellationToken)
    {
        EnsureDefaults();

        var files = new[]
        {
            Path.Combine(PromptsDirectory, "jarvis_rules.txt"),
            Path.Combine(PromptsDirectory, "reglas_verdad.txt"),
            Path.Combine(PromptsDirectory, "estilo_neutro.txt"),
            Path.Combine(PromptsDirectory, "gustos_musicales.txt"),
            Path.Combine(PromptsDirectory, "spotify_playlists.txt"),
            ConversationSummaryPath
        };

        var sb = new StringBuilder();
        sb.AppendLine("=== Contexto modular de Hanna ===");

        foreach (string file in files)
        {
            if (!File.Exists(file))
                continue;

            sb.AppendLine();
            sb.AppendLine($"--- {Path.GetFileName(file)} ---");
            sb.AppendLine(await File.ReadAllTextAsync(file, Encoding.UTF8, cancellationToken));
        }

        string profilePath = chatId == config.LocalChatId && config.LocalChatId != 0
            ? OwnerContextPath
            : Path.Combine(UserProfilesDirectory, chatId.ToString(), "perfil.txt");

        if (File.Exists(profilePath))
        {
            sb.AppendLine();
            sb.AppendLine("--- perfil_del_chat ---");
            sb.AppendLine(await File.ReadAllTextAsync(profilePath, Encoding.UTF8, cancellationToken));
        }

        return sb.ToString();
    }

    public async Task AppendChatPersonality(long chatId, string note, CancellationToken cancellationToken)
    {
        string folder = chatId == config.LocalChatId && config.LocalChatId != 0
            ? Path.Combine(UserProfilesDirectory, "owner")
            : Path.Combine(UserProfilesDirectory, chatId.ToString());

        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "personalidad_chat.txt");
        string line = $"- {DateTime.Now:yyyy-MM-dd HH:mm}: {note.Trim()}{Environment.NewLine}";
        await File.AppendAllTextAsync(file, line, Encoding.UTF8, cancellationToken);
    }

    private static void WriteIfMissing(string path, string content)
    {
        if (File.Exists(path))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content.Trim() + Environment.NewLine, Encoding.UTF8);
    }
}
