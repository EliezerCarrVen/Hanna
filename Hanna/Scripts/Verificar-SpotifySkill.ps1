$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$file = Join-Path $root "Skills\SpotifySkill.cs"
if (!(Test-Path $file)) { throw "No encontré Skills\SpotifySkill.cs" }
Write-Host "Revisando $file"
Select-String -Path $file -Pattern "ExtractPlaylistPlayQuery|NormalizeSpotifySpeech|SpotifyPlayAlbum" | ForEach-Object {
    "{0}:{1}: {2}" -f $_.Path, $_.LineNumber, $_.Line.Trim()
}
