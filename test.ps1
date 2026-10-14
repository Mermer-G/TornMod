$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.dev/dotnet-home'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.dev/nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$tml = 'C:/Program Files (x86)/Steam/steamapps/common/tModLoader'
# Fresh scratch profile keeps checks independent of the user's balance settings.
$checkProfile = Join-Path $PSScriptRoot ('.dev/checks/' + [Guid]::NewGuid().ToString('N'))
Push-Location $PSScriptRoot
try {
    dotnet run --project Tests/HomingChecks/HomingChecks.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Homing math checks failed.' }
    dotnet build TornMod.sln --configuration Debug --nologo '-p:IntermediateOutputPath=obj/Checks/' '-p:OutputPath=bin/Checks/' "-p:DevelopmentSavePath=$checkProfile"
    if ($LASTEXITCODE -ne 0) { throw 'Check build failed.' }
    Push-Location $tml
    try {
        & (Join-Path $tml 'dotnet/dotnet.exe') tModLoader.dll -server -tmlsavedirectory $checkProfile -slipchecks -slipchecks-exit
        if ($LASTEXITCODE -ne 0) { throw 'tModLoader engine checks failed.' }
    } finally { Pop-Location }
} finally { Pop-Location }
