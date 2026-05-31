$env:HANNA_TTS_ENABLED = "true"
$env:TTS_PROVIDER = "edge"
$env:TTS_VOICE = "es-PE-CamilaNeural"

$tmp = Join-Path $env:TEMP ("hanna_test_tts_" + [Guid]::NewGuid().ToString("N") + ".mp3")

Write-Host "Probando edge-tts..."
edge-tts --voice $env:TTS_VOICE --text "Hola, soy Hanna. La voz ya estÃ¡ funcionando." --write-media $tmp

if (Test-Path $tmp) {
    Write-Host "[OK] Audio generado: $tmp"
    Start-Process $tmp
} else {
    Write-Host "[ERROR] No se generÃ³ audio. Ejecuta Scripts\Install-TTS-Edge.ps1"
}
