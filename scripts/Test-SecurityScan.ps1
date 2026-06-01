$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$patterns = @(
  'TELEGRAM_TOKEN','TELEGRAM_BOT_TOKEN','GROQ_API_KEY','GEMINI_API_KEY','OPENROUTER_API_KEY',
  'SPOTIFY_CLIENT_SECRET','MYSQL_PASSWORD','HANNA_JWT_SECRET','HANNA_MOBILE_API_PAIRING_TOKEN',
  'sk-or-v1','gsk_','AIza'
)
$blockedFiles = @('HannaEnv.env','.env','appsettings.Development.json','google_client_secret.json')
Write-Host "[Hanna] Security scan" -ForegroundColor Cyan
foreach ($file in $blockedFiles) {
  if (Test-Path $file) { Write-Host "[WARN] Archivo sensible presente en working tree: $file" -ForegroundColor Yellow }
}
$rg = Get-Command rg -ErrorAction SilentlyContinue
if ($rg) {
  $expr = ($patterns -join '|')
  & rg -n --hidden -S $expr -g '!bin' -g '!obj' -g '!node_modules' -g '!*.dll' -g '!*.exe' .
  if ($LASTEXITCODE -le 1) { exit 0 }
  exit $LASTEXITCODE
}
Write-Host "rg no está instalado; usando Select-String limitado." -ForegroundColor Yellow
Get-ChildItem -Recurse -File | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules)\\' } | Select-String -Pattern $patterns -SimpleMatch
