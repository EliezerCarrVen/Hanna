$envPath = Join-Path (Get-Location) "HannaEnv.env"
if (-not (Test-Path $envPath)) {
  Write-Host "No encontré HannaEnv.env en: $(Get-Location)" -ForegroundColor Red
  exit 1
}

$tokenLine = Get-Content $envPath | Where-Object { $_ -match '^\s*TELEGRAM_TOKEN\s*=' } | Select-Object -Last 1
if (-not $tokenLine) {
  Write-Host "Falta TELEGRAM_TOKEN en HannaEnv.env" -ForegroundColor Yellow
  exit 0
}

$token = ($tokenLine -replace '^\s*TELEGRAM_TOKEN\s*=\s*', '').Trim().Trim('"').Trim("'")
if ([string]::IsNullOrWhiteSpace($token)) {
  Write-Host "TELEGRAM_TOKEN está vacío." -ForegroundColor Yellow
  exit 0
}

if ($token -match 'PEGA_AQUI|TU_TOKEN') {
  Write-Host "TELEGRAM_TOKEN todavía tiene placeholder. Pega el token real de BotFather." -ForegroundColor Yellow
  exit 0
}

if ($token -notmatch '^\d+:[A-Za-z0-9_-]{20,}$') {
  Write-Host "TELEGRAM_TOKEN no parece tener formato válido. Debe ser algo como 123456789:ABC..." -ForegroundColor Red
  exit 0
}

Write-Host "TELEGRAM_TOKEN parece tener formato válido." -ForegroundColor Green
