namespace Hanna.Models;

internal sealed record SpotifyCatalogItem
{
    public string Id { get; set; } = "";
    public string Uri { get; set; } = "";
    public string Name { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Type { get; set; } = "";
    public int TracksTotal { get; set; }
    public double Score { get; set; }
}