param(
    [string]$BaseUrl = "http://127.0.0.1:8790",
    [string]$TelegramChatId = "5112232887",
    [string]$PairingToken = "CAMBIA_ESTE_PAIRING_TOKEN"
)

Write-Host "Solicitando JWT a $BaseUrl/api/mobile/auth/telegram-login" -ForegroundColor Cyan

$body = @{
    telegramChatId = $TelegramChatId
    pairingToken = $PairingToken
} | ConvertTo-Json

$response = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/mobile/auth/telegram-login" -ContentType "application/json" -Body $body

$response | ConvertTo-Json -Depth 10

$token = $response.accessToken

Write-Host "`nProbando /api/mobile/message con Authorization: Bearer TOKEN" -ForegroundColor Cyan

$msg = @{
    text = "Hola Hanna, esta es una prueba con JWT desde PowerShell."
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/mobile/message" -ContentType "application/json" -Headers @{ Authorization = "Bearer $token" } -Body $msg
