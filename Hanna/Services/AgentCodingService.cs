using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class AgentCodingService
{
    private readonly OllamaService ollama;
    private readonly ContextService context;
    private readonly ProjectContextService projects;
    private readonly CodeOutputService codeOutput;

    public AgentCodingService(
        OllamaService ollama,
        ContextService context,
        ProjectContextService projects,
        CodeOutputService codeOutput)
    {
        this.ollama = ollama;
        this.context = context;
        this.projects = projects;
        this.codeOutput = codeOutput;
    }

    public async Task<(string Response, string? FilePath)> GenerateCodeFromRequest(long chatId, string request, CancellationToken cancellationToken)
    {
        HannaContext hannaContext = await context.BuildContext(chatId, cancellationToken);
        string projectContext = projects.BuildContextForRequest(request);

        string prompt =
            "Actúa como Hanna en modo agente programador local. " +
            "RESPONDE SIEMPRE Y ÚNICAMENTE EN ESPAÑOL. No uses inglés aunque el modelo base tienda a hacerlo. " +
            "No empieces con frases como 'Based on the code provided' ni hagas resúmenes en inglés. " +
            "El usuario quiere que programes, generes SQL o prepares código. " +
            "Usa el contexto de proyectos solo si ayuda. " +
            "Entrega una respuesta breve en español, máximo 3 frases, y después el código completo en un bloque Markdown. " +
            "Si el usuario pidió SQL, genera SQL listo para ejecutar. " +
            "No abras Visual Studio ni VS Code. No modifiques archivos existentes. Solo crea una propuesta en archivo de texto. " +
            "Si falta información, genera una versión funcional mínima y explica brevemente qué se puede ajustar.\n\n" +
            "PETICIÓN DEL USUARIO:\n" + request + "\n\n" +
            projectContext;

        string answer = await ollama.GenerateChat(prompt, hannaContext, cancellationToken);
        string? path = await codeOutput.SaveIfContainsCode(request, answer, cancellationToken);

        // Por defecto Hanna solo guarda el archivo de texto en segundo plano.
        // Si HANNA_AGENT_OPEN_GENERATED_CODE=true, CodeOutputService lo abrirá de forma opcional.
        if (!string.IsNullOrWhiteSpace(path))
            codeOutput.OpenGeneratedFile(path);

        return (EnsureSpanishUserFacingAnswer(answer, path), path);
    }


    private static string EnsureSpanishUserFacingAnswer(string answer, string? path)
    {
        string suffix = string.IsNullOrWhiteSpace(path) ? "" : $"\n\nArchivo generado: {path}";

        if (string.IsNullOrWhiteSpace(answer))
            return "Listo. Generé la propuesta solicitada." + suffix;

        string lower = answer.ToLowerInvariant();
        bool looksEnglish = lower.Contains("based on the code") ||
                            lower.Contains("here is") ||
                            lower.Contains("here's") ||
                            lower.Contains("the application") ||
                            lower.Contains("this code") ||
                            lower.Contains("overall,");

        if (!looksEnglish)
            return answer + suffix;

        return "Listo. Generé el código solicitado y lo guardé en un archivo de texto para que lo revises sin abrir Visual Studio." + suffix;
    }
}
