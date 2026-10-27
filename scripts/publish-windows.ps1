# Self-contained Windows x64 release.
. "$PSScriptRoot\env.ps1"
Push-Location $ProjectRoot
try {
    $out = 'artifacts/windows-x64'
    & $Dotnet publish src/Game.Windows/Game.Windows.csproj -c Release -r win-x64 --self-contained true -p:DebugType=none -o $out
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    Copy-Item -LiteralPath licenses -Destination "$out/licenses" -Recurse -Force
    Write-Output "Windows build: $out"
} finally { Pop-Location }
