$ErrorActionPreference = 'Stop'
$visualStudio = 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/devenv.exe'
if (-not (Test-Path -LiteralPath $visualStudio)) { throw 'Visual Studio 2026 bulunamadi.' }
Start-Process -FilePath $visualStudio -ArgumentList ('"' + (Join-Path $PSScriptRoot 'TornMod.sln') + '"') -WindowStyle Hidden
