$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
Write-Host "[Hanna] Revisión estática de StartupProfile" -ForegroundColor Cyan
Select-String -Path 'Hanna/Core/StartupProfile.cs' -Pattern 'telegram_only|hybrid|full|DecideAdminWeb|DecideTelegram|DecideOllama' | Out-Host
Write-Host "Para prueba runtime: `$env:HANNA_MODE='telegram_only'; dotnet run --project Hanna/Hanna.csproj" -ForegroundColor Yellow
