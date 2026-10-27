param([string]$OutputDirectory = 'artifacts/design-refresh', [switch]$SkipBuild)
. "$PSScriptRoot\env.ps1"
Push-Location $ProjectRoot
try {
    if (-not $SkipBuild) {
        & $Dotnet build src/Game.Windows/Game.Windows.csproj -c Release -v quiet
        if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    }
    $gameExe = (Resolve-Path 'src/Game.Windows/bin/Release/net10.0/win-x64/AGS Tower.exe').Path
    $captures = @(
        @{ Name='menu'; Scene='menu'; Size='1280x720'; Lang='uz' },
        @{ Name='campaign'; Scene='maps'; Size='1280x720'; Lang='uz' },
        @{ Name='briefing'; Scene='setup'; Size='1280x720'; Lang='uz' },
        @{ Name='battle'; Scene='battle'; Size='1280x720'; Lang='uz' },
        @{ Name='tower'; Scene='tower'; Size='1280x720'; Lang='en' },
        @{ Name='armory'; Scene='armory'; Size='1920x1080'; Lang='en' },
        @{ Name='enemies'; Scene='help2'; Size='1920x1080'; Lang='uz' },
        @{ Name='loading'; Scene='loading'; Size='1280x720'; Lang='uz'; Frames=1 },
        @{ Name='restore'; Scene='restore'; Size='1280x720'; Lang='uz' },
        @{ Name='phone-16x9'; Scene='build'; Size='1920x1080'; Lang='uz'; Touch='3' },
        @{ Name='phone-wide'; Scene='tower'; Size='1200x540'; Lang='en'; Touch='1.4' }
        @{ Name='phone-settings'; Scene='settings'; Size='1920x1080'; Lang='uz'; Touch='3' }
        @{ Name='achievements'; Scene='achievements'; Size='1280x720'; Lang='uz' }
        @{ Name='pause'; Scene='pause'; Size='1280x720'; Lang='uz' }
        @{ Name='phone-victory'; Scene='victory'; Size='1920x1080'; Lang='en'; Touch='3' }
    )
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $outputRoot = (Resolve-Path $OutputDirectory).Path
    foreach ($capture in $captures) {
        $target = Join-Path $outputRoot ($capture.Name + '.png')
        $frameCount = if ($capture.Frames) { $capture.Frames } else { 8 }
        $gameArgs = @("--screenshot=$target", "--scene=$($capture.Scene)", "--size=$($capture.Size)", "--lang=$($capture.Lang)", "--frames=$frameCount")
        if ($capture.Touch) { $gameArgs += "--touch=$($capture.Touch)" }
        $captureTemp = Join-Path ([IO.Path]::GetTempPath()) ('QalqonCapture-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $captureTemp | Out-Null
        $p = Start-Process -FilePath $gameExe -ArgumentList $gameArgs -Environment @{TMP=$captureTemp;TEMP=$captureTemp} -WindowStyle Hidden -PassThru -Wait
        if ($p.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $target)) { throw "Capture failed: $($capture.Name)" }
        Write-Output "$($capture.Name): $target"
    }
} finally { Pop-Location }
