namespace Hanna.Models;

internal sealed record SpotifyTrack
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public double Score { get; set; }
    public string PendingAction { get; set; } = "";
}
