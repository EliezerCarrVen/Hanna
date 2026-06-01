param(
  [switch]$Backend,
  [string]$ApiBaseUrl = "http://127.0.0.1:8790",
  [int]$Port = 5173
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$ui = Join-Path $root "HannaWebUI"

if (-not (Test-Path $ui)) {
  throw "No existe HannaWebUI. Ejecuta este script desde el repositorio actualizado."
}

Set-Location $ui
if (-not (Test-Path "node_modules")) {
  Write-Host "[HannaWebUI] Instalando dependencias npm..." -ForegroundColor Cyan
  npm install
}

$env:VITE_HANNA_API_BASE_URL = $ApiBaseUrl
$env:VITE_HANNA_DEMO_MODE = if ($Backend) { "false" } else { "true" }

Write-Host "[HannaWebUI] URL: http://127.0.0.1:$Port" -ForegroundColor Green
if ($Backend) {
  Write-Host "[HannaWebUI] Backend real activado: $ApiBaseUrl" -ForegroundColor Yellow
  Write-Host "[HannaWebUI] Si el backend no responde, la UI mostrará backend desconectado sin exigir API activa." -ForegroundColor Yellow
} else {
  Write-Host "[HannaWebUI] Modo demo por defecto. No se llamará al backend y no habrá ECONNREFUSED." -ForegroundColor Yellow
  Write-Host "[HannaWebUI] Para backend real: scripts\Run-Hanna-WebUI.ps1 -Backend -ApiBaseUrl http://127.0.0.1:8790" -ForegroundColor Yellow
}

npm run dev -- --port $Port
