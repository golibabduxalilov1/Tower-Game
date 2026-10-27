. "$PSScriptRoot\env.ps1"
Push-Location $ProjectRoot
try {
    & $Dotnet run --project src/Game.Windows/Game.Windows.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Game launch failed. See build output.' }
} finally { Pop-Location }
