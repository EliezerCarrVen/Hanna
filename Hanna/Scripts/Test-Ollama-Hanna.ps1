$body = @{
    model = "qwen2.5-coder:3b"
    stream = $false
    messages = @(
        @{ role = "system"; content = "Responde siempre en espaÃ±ol, breve y claro." },
        @{ role = "user"; content = "Di: Hanna estÃ¡ respondiendo correctamente." }
    )
} | ConvertTo-Json -Depth 5

try {
    $res = Invoke-RestMethod -Method Post -Uri "http://localhost:11434/api/chat" -Body $body -ContentType "application/json" -TimeoutSec 60
    $res.message.content
} catch {
    Write-Host "Ollama no respondiÃ³: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Prueba: ollama pull qwen2.5-coder:3b"
}
