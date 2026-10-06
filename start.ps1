param([switch]$SkipInstall, [switch]$Reinstall)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskWebRoot = Join-Path $taskRoot 'web'
foreach ($taskPort in @(5080, 5173)) {
    $taskSocket = New-Object System.Net.Sockets.TcpClient
    $taskPortInUse = $false
    try { $taskSocket.Connect('127.0.0.1', $taskPort); $taskPortInUse = $true }
    catch [System.Net.Sockets.SocketException] { }
    finally { $taskSocket.Dispose() }
    if ($taskPortInUse) {
        throw "Port $taskPort is already in use. Stop the previous app with Ctrl+C before starting again."
    }
}
if (-not $SkipInstall) {
    & dotnet restore (Join-Path $taskRoot 'Poulet.slnx') --configfile (Join-Path $taskRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed.' }
    $taskLockHash = (Get-FileHash (Join-Path $taskWebRoot 'package-lock.json') -Algorithm SHA256).Hash
    $taskInstallStamp = Join-Path $taskWebRoot 'node_modules/.poulet-lock.sha256'
    $taskInstalledHash = if (Test-Path $taskInstallStamp) { (Get-Content $taskInstallStamp -Raw).Trim() } else { '' }
    if ($Reinstall -or $taskInstalledHash -ne $taskLockHash -or -not (Test-Path (Join-Path $taskWebRoot 'node_modules/vite/bin/vite.js'))) {
        Push-Location $taskWebRoot
        try {
            & npm.cmd ci
            if ($LASTEXITCODE -ne 0) { throw 'npm install failed.' }
            Set-Content -LiteralPath $taskInstallStamp -Value $taskLockHash -Encoding ascii -NoNewline
        }
        finally { Pop-Location }
    }
    else { Write-Host 'Frontend dependencies are up to date.' }
}
if ([string]::IsNullOrWhiteSpace($env:Jwt__Secret)) {
    $taskJwtBytes = New-Object byte[] 48
    $taskRandom = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $taskRandom.GetBytes($taskJwtBytes) }
    finally { $taskRandom.Dispose() }
    $env:Jwt__Secret = [Convert]::ToBase64String($taskJwtBytes)
    Write-Host 'A temporary development JWT secret was generated for this session.'
}
elseif ([System.Text.Encoding]::UTF8.GetByteCount($env:Jwt__Secret) -lt 32) {
    throw 'Jwt__Secret must contain at least 32 bytes.'
}
$taskSolution = Join-Path $taskRoot 'Poulet.slnx'
& dotnet build $taskSolution --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
$taskApiProject = '"' + (Join-Path $taskRoot 'src/Poulet.Api') + '"'
$taskLogRoot = Join-Path $taskRoot 'artifacts\logs'
New-Item -ItemType Directory -Path $taskLogRoot -Force | Out-Null
$taskRunId = Get-Date -Format 'yyyyMMdd-HHmmss'
$taskApiOut = Join-Path $taskLogRoot "api-$taskRunId.out.log"
$taskApiErr = Join-Path $taskLogRoot "api-$taskRunId.err.log"
$taskWebOut = Join-Path $taskLogRoot "web-$taskRunId.out.log"
$taskWebErr = Join-Path $taskLogRoot "web-$taskRunId.err.log"
$taskApi = Start-Process dotnet -ArgumentList @('run', '--project', $taskApiProject, '--no-build', '--no-restore') -WorkingDirectory $taskRoot -WindowStyle Hidden -RedirectStandardOutput $taskApiOut -RedirectStandardError $taskApiErr -PassThru
$taskWeb = Start-Process npm.cmd -ArgumentList @('run', 'dev') -WorkingDirectory (Join-Path $taskRoot 'web') -WindowStyle Hidden -RedirectStandardOutput $taskWebOut -RedirectStandardError $taskWebErr -PassThru
Write-Host 'App: http://127.0.0.1:5173 | Development login: admin / admin123'
Write-Host "API process: $($taskApi.Id). Web process: $($taskWeb.Id). Press Ctrl+C to stop."
try {
    while (-not $taskApi.HasExited -and -not $taskWeb.HasExited) { Start-Sleep -Seconds 1 }
    $taskStopped = if ($taskApi.HasExited) { 'API' } else { 'Frontend' }
    $taskExitCode = if ($taskApi.HasExited) { $taskApi.ExitCode } else { $taskWeb.ExitCode }
    Write-Host "$taskStopped stopped unexpectedly (exit code $taskExitCode)." -ForegroundColor Red
    Write-Host "API logs: $taskApiOut and $taskApiErr"
    Write-Host "Frontend logs: $taskWebOut and $taskWebErr"
    $taskErrorLog = if ($taskApi.HasExited) { $taskApiErr } else { $taskWebErr }
    if (Test-Path $taskErrorLog) {
        $taskErrorLines = Get-Content -LiteralPath $taskErrorLog -Tail 30
        if ($taskErrorLines) {
            Write-Host 'Last error lines:' -ForegroundColor Yellow
            $taskErrorLines | ForEach-Object { Write-Host $_ }
        }
    }
}
finally {
    foreach ($taskProcess in @($taskApi,$taskWeb)) {
        if (-not $taskProcess.HasExited) { & taskkill.exe /PID $taskProcess.Id /T /F | Out-Null }
    }
}
