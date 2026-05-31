param(
    [string]$ProjectRoot = (Resolve-Path "$PSScriptRoot\..").Path
)

Write-Host "Aplicando mejoras Hanna Jarvis..." -ForegroundColor Cyan

$folders = @(
    "prompts_hanna",
    "chat_profiles\owner",
    "hanna_self_knowledge",
    "registros_acciones"
)

foreach ($folder in $folders) {
    $path = Join-Path $ProjectRoot $folder
    if (!(Test-Path $path)) {
        New-Item -ItemType Directory -Path $path | Out-Null
        Write-Host "Creado: $path" -ForegroundColor Green
    }
}

$files = @{
    "prompts_hanna\jarvis_rules.txt" = @"
Hanna debe comportarse como un asistente personal tipo Jarvis, sin copiar frases exactas.
- Ejecutar órdenes con precisión.
- Separar multi órdenes.
- Detenerse con "Hanna para".
- Confirmar breve.
- No mezclar datos del dueño con otros chats.
"@
    "prompts_hanna\modismos_mexicanos.txt" = @"
pedo = problema, situación o asunto.
qué pedo = qué pasa.
va = de acuerdo.
arre = de acuerdo.
simón = sí.
nel = no.
chido = bueno.
gacho = malo.
"@
    "prompts_hanna\gustos_musicales.txt" = @"
Edita aquí tus gustos musicales.
generos_favoritos:
-
artistas_favoritos:
-
moods:
- gym:
- noche:
- estudio:
"@
    "prompts_hanna\spotify_playlists.txt" = @"
playlist_principal =
playlist_gym =
playlist_noche =
playlist_estudio =
playlist_favoritas =
"@
    "chat_profiles\owner\usuario.txt" = @"
Nombre: Eliezer
Rol: dueño principal de Hanna
Preferencias:
- Español mexicano natural.
- Respuestas claras y directas.
- Saludo según hora en el chat principal.
Privacidad:
- Este perfil solo debe usarse para el dueño.
"@
}

foreach ($relative in $files.Keys) {
    $path = Join-Path $ProjectRoot $relative
    if (!(Test-Path $path)) {
        Set-Content -Path $path -Value $files[$relative] -Encoding UTF8
        Write-Host "Creado: $relative" -ForegroundColor Green
    }
    else {
        Write-Host "Existe, no se sobrescribe: $relative" -ForegroundColor Yellow
    }
}

Write-Host "Listo. Abre Hanna y revisa el panel web." -ForegroundColor Cyan
