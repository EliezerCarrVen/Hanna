param(
    [string]$ProjectDir = (Get-Location).Path,
    [switch]$Build
)

$ErrorActionPreference = "Stop"
$patchRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not (Test-Path (Join-Path $ProjectDir "Hanna.csproj"))) {
    throw "No encontré Hanna.csproj en $ProjectDir. Ejecuta este script desde la carpeta Hanna o pasa -ProjectDir."
}

$backup = Join-Path $ProjectDir ("_backup_patch_spotify_voice_" + (Get-Date -Format "yyyyMMdd_HHmmss"))
New-Item -ItemType Directory -Force -Path $backup | Out-Null

$files = @(
    "Program.cs",
    "Core\AppConfig.cs",
    "Services\RuntimeSettingsService.cs",
    "Services\MicrophoneRecorderService.cs",
    "Services\VoiceCommandService.cs",
    "Services\AdminWebServerService.cs",
    "Utilities\TextTools.cs",
    "Spotify\SpotifyLibraryService.cs",
    "Spotify\SpotifyPlaybackService.cs",
    "Spotify\SpotifySmartResolverService.cs",
    "Skills\SpotifySkill.cs"
)

foreach ($rel in $files) {
    $src = Join-Path $patchRoot $rel
    $dst = Join-Path $ProjectDir $rel
    if (-not (Test-Path $src)) { throw "Falta archivo de patch: $src" }
    if (Test-Path $dst) {
        $backupPath = Join-Path $backup $rel
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupPath) | Out-Null
        Copy-Item $dst $backupPath -Force
    }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dst) | Out-Null
    Copy-Item $src $dst -Force
    Write-Host "Aplicado: $rel"
}

$envSnippet = Join-Path $patchRoot "HannaEnv.spotify_voice_fix.example.env"
$envPath = Join-Path $ProjectDir "HannaEnv.env"
Write-Host ""
Write-Host "IMPORTANTE: revisa $envSnippet y copia esos ajustes a HannaEnv.env si quieres activar sensibilidad recomendada." -ForegroundColor Yellow
Write-Host "Backup creado en: $backup"

if ($Build) {
    Push-Location $ProjectDir
    dotnet clean
    dotnet restore
    dotnet build
    Pop-Location
}
