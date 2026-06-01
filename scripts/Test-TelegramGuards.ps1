$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
Write-Host "[Hanna] Revisando guards de Telegram" -ForegroundColor Cyan
$files = @('Hanna/Services/TelegramService.cs','Hanna/Skills/SystemSkill.cs','Hanna/Services/ResponseService.cs')
foreach ($file in $files) {
  if (!(Test-Path $file)) { throw "Falta $file" }
}
Select-String -Path 'Hanna/Services/TelegramService.cs' -Pattern 'string.IsNullOrWhiteSpace\(text\)' | Out-Host
Select-String -Path 'Hanna/Skills/SystemSkill.cs' -Pattern 'string.IsNullOrWhiteSpace\(text\)' | Out-Host
Select-String -Path 'Hanna/Services/ResponseService.cs' -Pattern 'SecretSanitizer.Sanitize' | Out-Host
Write-Host "[OK] Revisión estática completada." -ForegroundColor Green
