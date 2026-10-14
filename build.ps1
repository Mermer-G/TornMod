$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.dev/dotnet-home'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.dev/nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
Push-Location $PSScriptRoot
try {
    dotnet build (Join-Path $PSScriptRoot 'TornMod.sln') --configuration Debug --nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $LASTEXITCODE" }
} finally {
    Pop-Location
}
