param(
    [string]$ProjectDir = (Resolve-Path "$PSScriptRoot\..").Path
)

$ErrorActionPreference = "Stop"

$startup = [Environment]::GetFolderPath("Startup")
$batPath = Join-Path $ProjectDir "Start-Hanna-Hidden.bat"
$vbsPath = Join-Path $startup "Hanna.vbs"

$bat = @"
@echo off
cd /d "$ProjectDir"
start "" /min dotnet run --project "$ProjectDir\Hanna.csproj"
"@

Set-Content -Path $batPath -Value $bat -Encoding ASCII

$vbs = @"
Set WshShell = CreateObject("WScript.Shell")
WshShell.Run chr(34) & "$batPath" & chr(34), 0
Set WshShell = Nothing
"@

Set-Content -Path $vbsPath -Value $vbs -Encoding ASCII

Write-Host "Hanna quedó configurada para iniciar con Windows."
Write-Host "Acceso creado en: $vbsPath"
Write-Host "BAT creado en: $batPath"
