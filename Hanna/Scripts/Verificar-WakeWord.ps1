$ErrorActionPreference = "SilentlyContinue"
Write-Host "Buscando WakeWordService..."
Get-ChildItem -Recurse -Filter *.cs | Select-String "class WakeWordService|new WakeWordService|HANNA_WAKE_WORD" | Select-Object Path, LineNumber, Line
