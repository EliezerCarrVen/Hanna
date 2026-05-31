Write-Host "Instalando edge-tts para Hanna..."
$cmds = @(
    "py -m pip install --user --upgrade edge-tts",
    "python -m pip install --user --upgrade edge-tts"
)

foreach ($cmd in $cmds) {
    Write-Host "Ejecutando: $cmd"
    cmd /c $cmd
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] edge-tts instalado."
        exit 0
    }
}

Write-Host "[ERROR] No pude instalar edge-tts. Revisa Python."
exit 1
