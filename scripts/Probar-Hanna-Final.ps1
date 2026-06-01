$ErrorActionPreference = "Continue"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
Write-Host "[Hanna] Prueba final segura" -ForegroundColor Cyan
& dotnet restore Hanna/Hanna.csproj
& dotnet build Hanna/Hanna.csproj --no-incremental
& dotnet list Hanna/Hanna.csproj package --vulnerable
& git diff --check
& git status --short
& $PSScriptRoot/Test-SecurityScan.ps1
& $PSScriptRoot/Test-StartupProfiles.ps1
& $PSScriptRoot/Test-TelegramGuards.ps1
& $PSScriptRoot/Test-CommandsRouting.ps1
