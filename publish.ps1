$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
& dotnet restore (Join-Path $taskRoot 'Poulet.slnx') --configfile (Join-Path $taskRoot 'NuGet.Config')
if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed.' }
Push-Location (Join-Path $taskRoot 'web')
try {
    & npm.cmd ci
    if ($LASTEXITCODE -ne 0) { throw 'npm install failed.' }
    & npm.cmd run build
    if ($LASTEXITCODE -ne 0) { throw 'React build failed.' }
}
finally { Pop-Location }
$taskWebRoot=Join-Path $taskRoot 'src/Poulet.Api/wwwroot'
New-Item -ItemType Directory -Path $taskWebRoot -Force | Out-Null
Copy-Item -Path (Join-Path $taskRoot 'web/dist/*') -Destination $taskWebRoot -Recurse -Force
& dotnet publish (Join-Path $taskRoot 'src/Poulet.Api') -c Release --no-restore -o (Join-Path $taskRoot 'artifacts/app')
if ($LASTEXITCODE -ne 0) { throw 'API publish failed.' }
Write-Host 'Local publish output: modern/artifacts/app. Configure HTTPS and Seed__AdminPassword before production startup.'
