$startup = [Environment]::GetFolderPath("Startup")
$vbsPath = Join-Path $startup "Hanna.vbs"
if (Test-Path $vbsPath) {
    Remove-Item $vbsPath -Force
    Write-Host "Inicio automático de Hanna eliminado."
} else {
    Write-Host "No encontré el acceso de inicio automático de Hanna."
}
