param(
    [string]$ProjectDir = "C:\Users\Eliezer\Downloads\proyectos\repos\Hanna Actual\Hanna",
    [switch]$Build
)

$ErrorActionPreference = "Stop"
$PatchRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

Write-Host "[Hanna Rescue] Cerrando procesos Hanna si existen..."
Get-Process Hanna -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 700

if (-not (Test-Path $ProjectDir)) {
    throw "No existe ProjectDir: $ProjectDir"
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = Join-Path $ProjectDir "_backup_rescue_$timestamp"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

$files = @(
    "Program.cs",
    "Core\AppConfig.cs",
    "Services\RuntimeSettingsService.cs",
    "Services\VoiceCommandService.cs",
    "Services\WakeWordService.cs",
    "Skills\SpotifySkill.cs",
    "Spotify\SpotifySmartResolverService.cs",
    "Spotify\SpotifyLibraryService.cs",
    "Spotify\SpotifyPlaybackService.cs",
    "Utilities\TextTools.cs"
)

foreach ($rel in $files) {
    $src = Join-Path $PatchRoot $rel
    $dst = Join-Path $ProjectDir $rel

    if (-not (Test-Path $src)) {
        throw "Falta archivo de patch: $src"
    }

    $dstDir = Split-Path -Parent $dst
    New-Item -ItemType Directory -Force -Path $dstDir | Out-Null

    if (Test-Path $dst) {
        $backupPath = Join-Path $backupDir $rel
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupPath) | Out-Null
        Copy-Item $dst $backupPath -Force
    }

    Copy-Item $src $dst -Force
    Write-Host "[Hanna Rescue] Aplicado: $rel"
}

Write-Host "[Hanna Rescue] Respaldo creado en: $backupDir"

# Limpieza de binarios bloqueados ya con proceso cerrado.
Remove-Item -Recurse -Force (Join-Path $ProjectDir "bin") -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force (Join-Path $ProjectDir "obj") -ErrorAction SilentlyContinue

if ($Build) {
    Push-Location $ProjectDir
    try {
        dotnet clean
        dotnet restore
        dotnet build
    }
    finally {
        Pop-Location
    }
}

Write-Host "[Hanna Rescue] Terminado."
