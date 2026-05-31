using Hanna.Core;

namespace Hanna.Services;

internal enum QueryRoute
{
    Local,
    Routine,
    Technical,
    Creative,
    Administrative,
    General
}

internal sealed record QueryAnalysisResult(QueryRoute Route, string Reason, bool PreferLocal);

internal sealed class QueryAnalyzerService
{
    public QueryAnalysisResult Analyze(string text)
    {
        string normalized = (text ?? "").ToLowerInvariant();

        if (Regex.IsMatch(normalized, @"\b(spotify|volumen|abre|abrir|cierra|cerrar|apaga|enciende|dispositivo|estado|archivo|carpeta|recordatorio|rutina|clima)\b"))
            return new QueryAnalysisResult(QueryRoute.Local, "Comando administrativo o de sistema; debe resolverse localmente si existe una skill.", true);

        if (Regex.IsMatch(normalized, @"\b(tokens|costo|reporte|resumen|estadistica|estadística|datos|tabla|administrativo|presupuesto)\b"))
            return new QueryAnalysisResult(QueryRoute.Administrative, "Consulta administrativa o de análisis de datos.", true);

        if (Regex.IsMatch(normalized, @"\b(codigo|código|programa|api|endpoint|sql|base de datos|clase|método|metodo|función|funcion|debug|error|visual studio|vscode|docker|kubernetes)\b"))
            return new QueryAnalysisResult(QueryRoute.Technical, "Consulta técnica o de programación.", false);

        if (Regex.IsMatch(normalized, @"\b(redacta|documento|contrato|propuesta|correo|ensayo|historia|creativo|marketing|presentación|presentacion)\b"))
            return new QueryAnalysisResult(QueryRoute.Creative, "Consulta de redacción o creatividad.", false);

        if (Regex.IsMatch(normalized, @"\b(resumen|explica|analiza|compara|plan|estrategia|idea|arquitectura|refactoriza|optimiza)\b"))
            return new QueryAnalysisResult(QueryRoute.General, "Consulta general de razonamiento.", false);

        return new QueryAnalysisResult(QueryRoute.General, "Consulta general.", false);
    }
}
