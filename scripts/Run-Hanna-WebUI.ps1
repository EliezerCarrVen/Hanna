$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$ui = Join-Path $root "HannaWebUI"

if (-not (Test-Path $ui)) {
    Write-Host "No se encontró HannaWebUI en: $ui" -ForegroundColor Red
    exit 1
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "No se encontró npm. Instala Node.js LTS antes de correr la interfaz." -ForegroundColor Red
    exit 1
}

Push-Location $ui
try {
    if (-not (Test-Path "node_modules")) {
        Write-Host "Instalando dependencias de HannaWebUI..." -ForegroundColor Cyan
        npm install
    }

    Write-Host "Iniciando HannaWebUI en http://127.0.0.1:8788" -ForegroundColor Green
    npm run dev
}
finally {
    Pop-Location
}
