namespace Hanna.Models;

internal sealed class TokenEstimateResult
{
    public string Source { get; set; } = "";
    public long Characters { get; set; }
    public long EstimatedTokens { get; set; }
    public decimal EstimatedUsd { get; set; }
    public string Model { get; set; } = "";
    public bool ExceedsRecommendedLimit { get; set; }
    public string Recommendation { get; set; } = "";

    public string ToHumanText()
    {
        string risk = ExceedsRecommendedLimit ? "ALTO" : "NORMAL";
        return
            $"Estimación de tokens ({risk})\n" +
            $"Fuente: {Source}\n" +
            $"Caracteres: {Characters:N0}\n" +
            $"Tokens estimados: {EstimatedTokens:N0}\n" +
            $"Modelo base: {Model}\n" +
            $"Costo estimado: ${EstimatedUsd:0.0000} USD\n" +
            $"Recomendación: {Recommendation}";
    }
}
