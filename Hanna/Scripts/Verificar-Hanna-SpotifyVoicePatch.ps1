$ErrorActionPreference = "Continue"
Write-Host "Verificando métodos y archivos clave..."
$checks = @(
    @{ File="Skills\SpotifySkill.cs"; Pattern="PlaySmart" },
    @{ File="Spotify\SpotifySmartResolverService.cs"; Pattern="ResolveForPlay" },
    @{ File="Spotify\SpotifyPlaybackService.cs"; Pattern="PlayAlbum" },
    @{ File="Utilities\TextTools.cs"; Pattern="DramaticNameScold" },
    @{ File="Services\MicrophoneRecorderService.cs"; Pattern="BufferMilliseconds = 35" }
)
foreach ($c in $checks) {
    if (Test-Path $c.File) {
        $found = Select-String -Path $c.File -Pattern $c.Pattern -Quiet
        Write-Host ($c.File + " -> " + $c.Pattern + ": " + ($found ? "OK" : "FALTA"))
    } else {
        Write-Host ($c.File + " no existe") -ForegroundColor Red
    }
}
