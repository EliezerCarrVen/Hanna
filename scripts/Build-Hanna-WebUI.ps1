$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$ui = Join-Path $root "HannaWebUI"

if (-not (Test-Path $ui)) {
    Write-Host "No se encontró HannaWebUI en: $ui" -ForegroundColor Red
    exit 1
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "No se encontró npm. Instala Node.js LTS antes de compilar la interfaz." -ForegroundColor Red
    exit 1
}

Push-Location $ui
try {
    npm install
    npm run build
}
finally {
    Pop-Location
}
