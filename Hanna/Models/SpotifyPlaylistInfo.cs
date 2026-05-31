namespace Hanna.Models;

internal sealed record SpotifyPlaylistInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Public { get; set; }
    public int TracksTotal { get; set; }
}
