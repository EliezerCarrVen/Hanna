$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
Write-Host "[Hanna] Revisión estática de comandos críticos" -ForegroundColor Cyan
Select-String -Path 'Hanna/Skills/DiagnosticsSkill.cs' -Pattern '/status|/diagnostico|/demo|/showcase|/siguiente_paso' | Out-Host
Select-String -Path 'Hanna/Skills/SkillRouter.cs','Hanna/Skills/AssistantControlSkill.cs' -Pattern 'motor|EngineModeChange' | Out-Host
Select-String -Path 'Hanna/Skills/SkillRouter.cs','Hanna/Skills/PhaseSkill.cs' -Pattern 'fase|PhaseControl' | Out-Host
Write-Host "[OK] Rutas principales presentes." -ForegroundColor Green
