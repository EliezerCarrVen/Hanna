param(
    [string]$ProjectDir = "C:\Users\Eliezer\Downloads\proyectos\repos\Hanna Actual\Hanna",
    [switch]$Build
)

$ErrorActionPreference = "Stop"

function New-SecretBase64 {
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $bytes = New-Object byte[] 32
    $rng.GetBytes($bytes)
    return [Convert]::ToBase64String($bytes)
}

function Read-EnvMap($path) {
    $map = @{}
    if (-not (Test-Path $path)) { return $map }

    Get-Content $path -Encoding UTF8 | ForEach-Object {
        $line = $_.Trim()
        if ($line -eq "" -or $line.StartsWith("#")) { return }
        $idx = $line.IndexOf("=")
        if ($idx -le 0) { return }
        $key = $line.Substring(0, $idx).Trim()
        $value = $line.Substring($idx + 1).Trim()
        $map[$key] = $value
    }

    return $map
}

function Is-Placeholder($value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return $true }
    return $value -match "CAMBIA|CAMB|PEGA_AQUI|TU_|TOKEN|SECRET|MINIMO|CARACTERES"
}

function Set-UniqueEnvValue($path, $key, $value) {
    $lines = New-Object System.Collections.Generic.List[string]

    if (Test-Path $path) {
        Get-Content $path -Encoding UTF8 | ForEach-Object {
            if ($_ -notmatch "^\s*$([regex]::Escape($key))\s*=") {
                $lines.Add($_)
            }
        }
    }

    $lines.Add("$key=$value")
    $lines | Set-Content $path -Encoding UTF8
}

$project = (Resolve-Path $ProjectDir).Path
$envPath = Join-Path $project "HannaEnv.env"
$runtimePath = Join-Path $project "configuracion_chats\runtime_settings.json"
$backupDir = Join-Path $project "_backup_fix_motor_voz_$(Get-Date -Format yyyyMMdd_HHmmss)"
New-Item -ItemType Directory -Force $backupDir | Out-Null

Write-Host "[Hanna Fix] Cerrando Hanna si está abierta..."
taskkill /IM Hanna.exe /F 2>$null | Out-Null

if (Test-Path $envPath) {
    Copy-Item $envPath (Join-Path $backupDir "HannaEnv.env.bak") -Force
}

if (Test-Path $runtimePath) {
    Copy-Item $runtimePath (Join-Path $backupDir "runtime_settings.json.bak") -Force
}

$env = Read-EnvMap $envPath

$jwt = $env["HANNA_JWT_SECRET"]
$pairing = $env["HANNA_MOBILE_API_PAIRING_TOKEN"]

if (Is-Placeholder $jwt) {
    $jwt = New-SecretBase64
    Write-Host "[OK] HANNA_JWT_SECRET generado de nuevo."
} else {
    Write-Host "[OK] HANNA_JWT_SECRET conservado."
}

if (Is-Placeholder $pairing) {
    $pairing = New-SecretBase64
    Write-Host "[OK] HANNA_MOBILE_API_PAIRING_TOKEN generado de nuevo."
} else {
    Write-Host "[OK] HANNA_MOBILE_API_PAIRING_TOKEN conservado."
}

# Motor estable.
Set-UniqueEnvValue $envPath "TELEGRAM_ENABLED" "true"
Set-UniqueEnvValue $envPath "HANNA_DEFAULT_ENGINE_MODE" "hybrid"
Set-UniqueEnvValue $envPath "HANNA_PC_ENGINE" "OllamaLocal"
Set-UniqueEnvValue $envPath "HANNA_TELEGRAM_ENGINE" "Hybrid"
Set-UniqueEnvValue $envPath "HANNA_PREFER_LOCAL_FOR_COMPUTER" "true"
Set-UniqueEnvValue $envPath "HANNA_PREFER_HYBRID_FOR_TELEGRAM" "true"

# Seguridad/accesos.
Set-UniqueEnvValue $envPath "HANNA_JWT_SECRET" $jwt
Set-UniqueEnvValue $envPath "HANNA_MOBILE_API_PAIRING_TOKEN" $pairing
Set-UniqueEnvValue $envPath "HANNA_ALLOWED_CHAT_IDS" "5112232887"
Set-UniqueEnvValue $envPath "TELEGRAM_ALLOWED_CHAT_IDS" "5112232887"
Set-UniqueEnvValue $envPath "HANNA_LOCAL_CHAT_ID" "5112232887"

# Ollama.
Set-UniqueEnvValue $envPath "OLLAMA_AUTO_START" "true"
Set-UniqueEnvValue $envPath "OLLAMA_BASE_URL" "http://localhost:11434"
Set-UniqueEnvValue $envPath "OLLAMA_MODEL" "qwen2.5-coder:3b"
Set-UniqueEnvValue $envPath "OLLAMA_CONTEXT_SIZE" "2048"

# Voz estable: baja umbrales y desactiva cámara como indicador.
Set-UniqueEnvValue $envPath "HANNA_VOICE_RECORD_IMMEDIATELY" "true"
Set-UniqueEnvValue $envPath "HANNA_LOCAL_VOICE_CAMERA_LED_ENABLED" "false"
Set-UniqueEnvValue $envPath "HANNA_LOCAL_VOICE_SILENCE_MS" "1500"
Set-UniqueEnvValue $envPath "HANNA_LOCAL_VOICE_NO_VOICE_TIMEOUT_MS" "12000"
Set-UniqueEnvValue $envPath "HANNA_LOCAL_VOICE_MAX_SECONDS" "30"
Set-UniqueEnvValue $envPath "HANNA_LOCAL_VOICE_MIN_SECONDS" "1"
Set-UniqueEnvValue $envPath "HANNA_LOCAL_VOICE_START_RMS" "420"
Set-UniqueEnvValue $envPath "HANNA_LOCAL_VOICE_STOP_RMS" "240"

# Funciones pesadas apagadas temporalmente.
Set-UniqueEnvValue $envPath "HANNA_WAKE_WORD_ENABLED" "false"
Set-UniqueEnvValue $envPath "GOOGLE_INTEGRATION_ENABLED" "false"
Set-UniqueEnvValue $envPath "HANNA_ASSIGNMENTS_ENABLED" "false"
Set-UniqueEnvValue $envPath "HANNA_STARTUP_GREETING_ENABLED" "false"
Set-UniqueEnvValue $envPath "HANNA_STARTUP_GREETING_TELEGRAM_ENABLED" "false"

# Funciones base activas.
Set-UniqueEnvValue $envPath "HANNA_ADMIN_WEB_ENABLED" "true"
Set-UniqueEnvValue $envPath "HANNA_ADMIN_WEB_PORT" "8787"
Set-UniqueEnvValue $envPath "HANNA_MOBILE_API_ENABLED" "true"
Set-UniqueEnvValue $envPath "HANNA_MOBILE_API_PORT" "8790"
Set-UniqueEnvValue $envPath "HANNA_DYNAMIC_SKILLS_ENABLED" "true"
Set-UniqueEnvValue $envPath "HANNA_SCREEN_ANALYSIS_ENABLED" "true"
Set-UniqueEnvValue $envPath "HANNA_MIRROR_LOCAL_TO_TELEGRAM" "true"

Write-Host "[OK] HannaEnv.env limpiado: claves duplicadas importantes fueron reemplazadas por una sola entrada."

if (Test-Path $runtimePath) {
    try {
        $json = Get-Content $runtimePath -Raw -Encoding UTF8 | ConvertFrom-Json

        $json.DefaultEngineMode = "hybrid"
        $json.PreferLocalForComputer = $true
        $json.PreferHybridForTelegram = $true
        $json.MirrorLocalToTelegram = $true

        $json.OllamaBaseUrl = "http://localhost:11434"
        $json.OllamaModel = "qwen2.5-coder:3b"
        $json.OllamaContextSize = 2048
        $json.OllamaAutoStart = $true

        $json.StartupGreetingEnabled = $false

        $json.LocalVoiceCameraLedEnabled = $false
        $json.LocalVoiceSilenceMs = 1500
        $json.LocalVoiceNoVoiceTimeoutMs = 12000
        $json.LocalVoiceMaxSeconds = 30
        $json.LocalVoiceMinSeconds = 1
        $json.LocalVoiceStartRms = 420
        $json.LocalVoiceStopRms = 240
        $json.VoiceRecordImmediately = $true
        $json.VoiceInitialGraceMs = 900

        $json.WakeWordEnabled = $false
        $json.AssignmentsEnabled = $false
        $json.GoogleIntegrationEnabled = $false

        $json.AdminWebEnabled = $true
        $json.MobileApiEnabled = $true
        $json.DynamicSkillsEnabled = $true
        $json.ScreenAnalysisEnabled = $true

        $json | ConvertTo-Json -Depth 12 | Set-Content $runtimePath -Encoding UTF8
        Write-Host "[OK] runtime_settings.json alineado con modo estable."
    }
    catch {
        Write-Host "[WARN] No pude editar runtime_settings.json. Lo renombraré para que no sobreescriba HannaEnv.env."
        Rename-Item $runtimePath "runtime_settings.desactivado_$(Get-Date -Format yyyyMMdd_HHmmss).json" -Force
    }
}

Write-Host ""
Write-Host "PAIRING TOKEN ACTUAL:"
Write-Host $pairing
Write-Host ""
Write-Host "Guárdalo. Lo necesitas para vincular la app móvil o probar /api/mobile/auth/telegram-login."

if ($Build) {
    Push-Location $project
    try {
        Remove-Item -Recurse -Force ".\bin" -ErrorAction SilentlyContinue
        Remove-Item -Recurse -Force ".\obj" -ErrorAction SilentlyContinue
        dotnet clean
        dotnet restore
        dotnet build
    }
    finally {
        Pop-Location
    }
}

Write-Host ""
Write-Host "[Hanna Fix] Terminado. Ejecuta:"
Write-Host "cd `"$project`""
Write-Host "dotnet run"
