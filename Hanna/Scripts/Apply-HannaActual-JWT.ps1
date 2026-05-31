param(
    [string]$ProjectDir = "C:\Users\Eliezer\Downloads\proyectos\repos\Hanna Actual\Hanna",
    [switch]$Build
)

$ErrorActionPreference = "Stop"

$patchRoot = Split-Path -Parent $PSScriptRoot
$project = (Resolve-Path $ProjectDir).Path
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backup = Join-Path $project "_backup_jwt_$timestamp"

Write-Host "[Hanna JWT] Proyecto: $project" -ForegroundColor Cyan
Write-Host "[Hanna JWT] Respaldo: $backup" -ForegroundColor Cyan

taskkill /IM Hanna.exe /F 2>$null | Out-Null

New-Item -ItemType Directory -Force $backup | Out-Null

$files = @(
    "Core\AppConfig.cs",
    "Services\AdminWebServerService.cs",
    "Services\MobileApiServerService.cs",
    "Services\JwtTokenService.cs"
)

foreach ($file in $files) {
    $src = Join-Path $patchRoot $file
    $dst = Join-Path $project $file

    if (-not (Test-Path $src)) {
        throw "Falta archivo del patch: $src"
    }

    if (Test-Path $dst) {
        $backupFile = Join-Path $backup $file
        New-Item -ItemType Directory -Force (Split-Path -Parent $backupFile) | Out-Null
        Copy-Item $dst $backupFile -Force
    }

    New-Item -ItemType Directory -Force (Split-Path -Parent $dst) | Out-Null
    Copy-Item $src $dst -Force

    Write-Host "[OK] $file"
}

$envExample = Join-Path $patchRoot "HannaEnv.jwt.example.env"
$envPath = Join-Path $project "HannaEnv.env"

if (Test-Path $envExample) {
    $envText = Get-Content $envPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue

    if ($envText -notmatch "HANNA_JWT_ENABLED") {
        Add-Content -Path $envPath -Value "`r`n# --- JWT agregado por patch ---" -Encoding UTF8
        Get-Content $envExample -Encoding UTF8 | Add-Content $envPath -Encoding UTF8
        Write-Host "[OK] Configuración JWT agregada al final de HannaEnv.env"
    }
    else {
        Write-Host "[INFO] HannaEnv.env ya tenía variables HANNA_JWT_*; no las dupliqué."
    }
}

$csprojPath = Join-Path $project "Hanna.csproj"
if (Test-Path $csprojPath) {
    $csproj = Get-Content $csprojPath -Raw -Encoding UTF8

    if ($csproj -notmatch "_backup_\*\*\\\*\*\\\*\.cs") {
        $itemGroup = @"

  <ItemGroup>
    <Compile Remove="_backup_**\**\*.cs" />
    <Compile Remove="backup\**\*.cs" />
    <Compile Remove="Backups\**\*.cs" />
    <Compile Remove="HannaBackups\**\*.cs" />
    <None Include="_backup_**\**\*" />
  </ItemGroup>
"@
        $csproj = $csproj -replace "</Project>", "$itemGroup`r`n</Project>"
        Set-Content -Path $csprojPath -Value $csproj -Encoding UTF8
        Write-Host "[OK] Hanna.csproj actualizado para ignorar respaldos internos."
    }
}

if ($Build) {
    Push-Location $project
    try {
        Remove-Item -Recurse -Force ".\bin" -ErrorAction SilentlyContinue
        Remove-Item -Recurse -Force ".\obj" -ErrorAction SilentlyContinue
        dotnet clean
        dotnet restore
        dotnet build
    }
    finally {
        Pop-Location
    }
}

Write-Host "[Hanna JWT] Patch aplicado." -ForegroundColor Green
Write-Host "IMPORTANTE: abre HannaEnv.env y cambia HANNA_JWT_SECRET y HANNA_MOBILE_API_PAIRING_TOKEN." -ForegroundColor Yellow
