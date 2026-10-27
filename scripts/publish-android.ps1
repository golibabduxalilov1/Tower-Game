# Android ARM64 release bundle (AAB). Signing uses the owner's upload key supplied through
# environment variables; without them the bundle is signed with the debug key and is NOT uploadable:
#   $env:QALQON_KEYSTORE, $env:QALQON_KEY_ALIAS, $env:QALQON_STORE_PASS, $env:QALQON_KEY_PASS
. "$PSScriptRoot\env.ps1"
Push-Location $ProjectRoot
try {
    $sign = @()
    if ($env:QALQON_KEYSTORE) { $sign = @("-p:QALQON_KEYSTORE=$env:QALQON_KEYSTORE", "-p:QALQON_KEY_ALIAS=$env:QALQON_KEY_ALIAS") }
    else { Write-Warning 'No upload key configured: output is debug-signed and only suitable for local testing.' }
    & $Dotnet publish src/Game.Android/Game.Android.csproj -c Release -f net10.0-android -r android-arm64 @AndroidArgs @sign -o artifacts/android-arm64
    if ($LASTEXITCODE -ne 0) { throw 'Android publish failed.' }
    Write-Output 'Android build: artifacts/android-arm64'
} finally { Pop-Location }
