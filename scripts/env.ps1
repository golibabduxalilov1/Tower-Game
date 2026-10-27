$ErrorActionPreference = 'Stop'
$script:ProjectRoot = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $script:ProjectRoot '.tools\cli'
$env:NUGET_PACKAGES = Join-Path $script:ProjectRoot '.tools\nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
$localDotnet = Join-Path $script:ProjectRoot '.tools\dotnet\dotnet.exe'
$script:Dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
# Android toolchain: project-local JDK if present, otherwise JAVA_HOME; SDK from ANDROID_HOME or the default location.
$localJdk = Join-Path $script:ProjectRoot '.tools\jdk'
if (Test-Path -LiteralPath (Join-Path $localJdk 'bin\java.exe')) { $env:JAVA_HOME = $localJdk }
if (-not $env:ANDROID_HOME) { $env:ANDROID_HOME = Join-Path $env:LOCALAPPDATA 'Android\Sdk' }
$script:AndroidArgs = @("-p:AndroidSdkDirectory=$env:ANDROID_HOME")
if ($env:JAVA_HOME) { $script:AndroidArgs += "-p:JavaSdkDirectory=$env:JAVA_HOME" }
