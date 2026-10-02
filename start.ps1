param([switch]$SkipInstall)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
if (-not $SkipInstall) {
    & dotnet restore (Join-Path $taskRoot 'Poulet.slnx') --configfile (Join-Path $taskRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed.' }
    Push-Location (Join-Path $taskRoot 'web')
    try { & npm.cmd ci; if ($LASTEXITCODE -ne 0) { throw 'npm install failed.' } }
    finally { Pop-Location }
}
$taskApi = Start-Process dotnet -ArgumentList @('run', '--project', (Join-Path $taskRoot 'src/Poulet.Api'), '--no-restore') -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru
$taskWeb = Start-Process npm.cmd -ArgumentList @('run', 'dev') -WorkingDirectory (Join-Path $taskRoot 'web') -WindowStyle Hidden -PassThru
Write-Host 'App: http://127.0.0.1:5173 | Development login: admin / admin123'
Write-Host "API process: $($taskApi.Id). Web process: $($taskWeb.Id). Press Ctrl+C to stop."
try { while (-not $taskApi.HasExited -and -not $taskWeb.HasExited) { Start-Sleep -Seconds 1 } }
finally {
    foreach ($taskProcess in @($taskApi,$taskWeb)) {
        if (-not $taskProcess.HasExited) { & taskkill.exe /PID $taskProcess.Id /T /F | Out-Null }
    }
}
