# Pruebas realizadas

En el entorno del agente no se pudo ejecutar `dotnet` porque no está instalado. Pruebas ejecutadas aquí:

- `git status --short`
- revisión de estructura con `rg --files`
- búsqueda de secretos con `rg`
- `git diff --check`

Pruebas esperadas en entorno local:

```powershell
dotnet restore Hanna/Hanna.csproj
dotnet build Hanna/Hanna.csproj --no-incremental
dotnet list Hanna/Hanna.csproj package --vulnerable
scripts/Probar-Hanna-Final.ps1
```
