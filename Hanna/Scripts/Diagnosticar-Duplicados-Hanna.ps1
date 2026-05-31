param(
    [string]$ProjectDir = (Get-Location).Path
)

Write-Host "Proyecto: $ProjectDir"
Write-Host "\nBuscando definiciones duplicadas..."

$patterns = @(
    "class AppConfig",
    "class TelegramService",
    "class OllamaService"
)

foreach ($pattern in $patterns) {
    Write-Host "\n=== $pattern ==="
    Get-ChildItem -Path $ProjectDir -Recurse -Filter *.cs |
        Select-String -Pattern $pattern |
        Select-Object Path, LineNumber, Line |
        Format-Table -AutoSize
}

Write-Host "\nSi aparece más de una definición por clase, borra o mueve la copia duplicada antes de compilar."
