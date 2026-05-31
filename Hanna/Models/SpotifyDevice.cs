namespace Hanna.Models;

internal sealed record SpotifyDevice
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsActive { get; set; }
}
