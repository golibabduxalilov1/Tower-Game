# Native UI tests: drives the real MonoGame window through tests/ui/*.txt scripts.
# Each child process receives a unique temporary root; player saves and earlier runs remain intact.
param([switch]$SkipBuild)
. "$PSScriptRoot\env.ps1"
Push-Location $ProjectRoot
try {
    if (-not $SkipBuild) {
        & $Dotnet build src/Game.Windows/Game.Windows.csproj -c Release -v q
        if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    }
    $exe = Get-ChildItem src/Game.Windows/bin/Release -Recurse -Filter "AGS Tower.exe" | Select-Object -First 1
    $runs = @(
        @{ Script = 'tests/ui/desktop-flow.txt'; Args = @('--lang=uz', '--size=1280x720') },
        @{ Script = 'tests/ui/touch-flow.txt'; Args = @('--lang=en', '--touch=1.4', '--size=1200x540') }
        @{ Script = 'tests/ui/supplied-ui-flow.txt'; Args = @('--lang=en', '--size=1280x720') }
    )
    $failed = 0
    foreach ($run in $runs) {
        $testTemp = Join-Path ([IO.Path]::GetTempPath()) ('QalqonUI-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $testTemp | Out-Null
        $p = Start-Process $exe.FullName -ArgumentList (@("--script=$($run.Script)") + $run.Args) -Environment @{TMP=$testTemp;TEMP=$testTemp} -WindowStyle Hidden -Wait -PassThru -WorkingDirectory $ProjectRoot
        $result = Get-Content "$($run.Script).log" -Encoding UTF8 | Select-Object -Last 1
        Write-Output "$($run.Script): $result (exit $($p.ExitCode))"
        if ($p.ExitCode -ne 0) { $failed++; Get-Content "$($run.Script).log" -Encoding UTF8 | Select-String 'FAIL' }
    }
    if ($failed -gt 0) { throw "$failed UI script(s) failed." }
} finally { Pop-Location }
