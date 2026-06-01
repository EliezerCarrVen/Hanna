$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$ui = Join-Path $root "HannaWebUI"

if (-not (Test-Path $ui)) {
  throw "No existe HannaWebUI."
}

Set-Location $ui
Write-Host "[HannaWebUI] Instalando dependencias..." -ForegroundColor Cyan
npm install
Write-Host "[HannaWebUI] Compilando UI..." -ForegroundColor Cyan
npm run build
Write-Host "[HannaWebUI] Build completado. dist/ es generado y no debe versionarse." -ForegroundColor Green
