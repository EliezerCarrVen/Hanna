param(
    [switch]$DeletePermanent,
    [switch]$CleanNode,
    [switch]$CleanDocs,
    [switch]$CleanLogs,
    [int]$KeepLogDays = 14
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

if (-not (Test-Path (Join-Path $projectDir "Hanna.csproj"))) {
    $projectDir = Get-Location
}

if (-not (Test-Path (Join-Path $projectDir "Hanna.csproj"))) {
    throw "No encuentro Hanna.csproj. Ejecuta este script desde la carpeta Hanna o deja el script dentro de Hanna\Scripts."
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = Join-Path $projectDir "_backup_limpieza_$timestamp"

if (-not $DeletePermanent) {
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
}

function Move-Or-Delete {
    param(
        [Parameter(Mandatory=$true)][string]$PathToClean
    )

    if (-not (Test-Path $PathToClean)) {
        return
    }

    $name = Split-Path -Leaf $PathToClean

    if ($DeletePermanent) {
        Write-Host "Eliminando: $PathToClean"
        Remove-Item -LiteralPath $PathToClean -Recurse -Force -ErrorAction SilentlyContinue
        return
    }

    $destination = Join-Path $backupDir $name
    $i = 1

    while (Test-Path $destination) {
        $destination = Join-Path $backupDir ("{0}_{1}" -f $name, $i)
        $i++
    }

    Write-Host "Moviendo a respaldo: $PathToClean"
    Move-Item -LiteralPath $PathToClean -Destination $destination -Force -ErrorAction SilentlyContinue
}

Write-Host "Proyecto detectado: $projectDir"
if ($DeletePermanent) {
    Write-Host "Modo: eliminación permanente"
} else {
    Write-Host "Modo: respaldo en $backupDir"
}

# Archivos/carpetas generados automáticamente por .NET
Move-Or-Delete (Join-Path $projectDir "bin")
Move-Or-Delete (Join-Path $projectDir "obj")

# Archivos de pruebas de voz y audios temporales
Get-ChildItem $projectDir -Force -File -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -like "prueba_*.mp3" -or
        $_.Name -like "prueba_*.wav" -or
        $_.Name -eq "generar_pruebas_voces.py" -or
        $_.Name -eq "repomix-output.xml"
    } |
    ForEach-Object { Move-Or-Delete $_.FullName }

Move-Or-Delete (Join-Path $projectDir "pruebas_voces_hanna")

# Archivos .env de ejemplo. NO borra HannaEnv.env.
Get-ChildItem $projectDir -Force -File -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -like "HannaEnv*.example.env" -or
        $_.Name -eq "personalidad.example.txt"
    } |
    ForEach-Object { Move-Or-Delete $_.FullName }

# Documentación vieja opcional
if ($CleanDocs) {
    Get-ChildItem $projectDir -Force -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -like "LEEME_*" -or
            $_.Name -like "README_*" -or
            $_.Name -like "GUIA_*" -or
            $_.Name -like "COMPARACION_*" -or
            $_.Name -like "MOTOR_*" -or
            $_.Name -like "PERSONALIDAD_SEGURA_*"
        } |
        ForEach-Object { Move-Or-Delete $_.FullName }
}

# Node solo si lo pides. El panel web C# no necesita node_modules.
if ($CleanNode) {
    Move-Or-Delete (Join-Path $projectDir "node_modules")
    Move-Or-Delete (Join-Path $projectDir "package.json")
    Move-Or-Delete (Join-Path $projectDir "package-lock.json")
}

# Logs antiguos opcional. No toca memoria, contexto, configuraciones ni tokens.
if ($CleanLogs) {
    $logsDir = Join-Path $projectDir "registros_conversacion"

    if (Test-Path $logsDir) {
        $limit = (Get-Date).AddDays(-$KeepLogDays)
        Get-ChildItem $logsDir -File -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -lt $limit } |
            ForEach-Object { Move-Or-Delete $_.FullName }
    }
}

Write-Host ""
Write-Host "Limpieza terminada."

if (-not $DeletePermanent) {
    Write-Host "Revisa el respaldo: $backupDir"
    Write-Host "Si todo compila bien, puedes borrar ese respaldo después."
}

Write-Host ""
Write-Host "Siguiente prueba recomendada:"
Write-Host "dotnet restore"
Write-Host "dotnet build"
