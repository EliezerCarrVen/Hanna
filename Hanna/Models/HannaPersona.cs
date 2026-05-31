namespace Hanna.Models;

internal sealed class HannaPersona
{
    public string Id { get; set; } = "operator";
    public string DisplayName { get; set; } = "Operator";
    public string ModelName { get; set; } = "google/gemini-2.0-flash";
    public string SystemPrompt { get; set; } = "";
    public bool EnableHighComplexityTools { get; set; }
    public bool PreferLocalFirst { get; set; } = true;
    public decimal EstimatedUsdPer1KTokens { get; set; } = 0.001m;
    public int MaxInputTokens { get; set; } = 12000;
    public int MaxOutputTokens { get; set; } = 1200;
    public string TemperatureProfile { get; set; } = "balanced";
}
