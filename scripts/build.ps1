# Builds Windows, runs core + integration tests, optionally the native UI scripts and Android.
param([switch]$Android, [switch]$UI)
. "$PSScriptRoot\env.ps1"
Push-Location $ProjectRoot
try {
    & $Dotnet build src/Game.Windows/Game.Windows.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Windows build failed.' }
    & $Dotnet run --project src/Game.Tests/Game.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
    & $Dotnet run --project src/Game.IntegrationTests/Game.IntegrationTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Integration tests failed.' }
    & $Dotnet run --project src/Game.Tools/Game.Tools.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Content validation failed.' }
    if ($UI) { & "$PSScriptRoot\test-ui.ps1" }
    if ($Android) {
        & $Dotnet build src/Game.Android/Game.Android.csproj -c Debug @AndroidArgs
        if ($LASTEXITCODE -ne 0) { throw 'Android build failed.' }
    }
} finally { Pop-Location }
