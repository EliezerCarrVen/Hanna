
param(
    [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

$telegramPath = Join-Path $ProjectRoot "Services\TelegramService.cs"
if (!(Test-Path $telegramPath)) {
    throw "No encontré Services\TelegramService.cs en $ProjectRoot"
}

$backupDir = Join-Path $ProjectRoot "_backup_fix_accesibilidad"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
Copy-Item $telegramPath (Join-Path $backupDir "TelegramService.cs.bak") -Force

$content = Get-Content $telegramPath -Raw
$content = $content.Replace("public sealed class TelegramService", "internal sealed class TelegramService")
$content = $content.Replace("public class TelegramService", "internal class TelegramService")
$content = $content.Replace("    public TelegramService(`r`n", "    internal TelegramService(`r`n")
$content = $content.Replace("    public TelegramService(`n", "    internal TelegramService(`n")
Set-Content -Path $telegramPath -Value $content -Encoding UTF8

Write-Host "Listo: TelegramService quedó internal para coincidir con los servicios internos." -ForegroundColor Green
Write-Host "Backup en: $backupDir" -ForegroundColor Yellow
Write-Host "Ahora ejecuta: dotnet clean; dotnet restore; dotnet build" -ForegroundColor Cyan
