# Generates badges/coverage.svg from a local test + coverage run.
# No CI, no upload, no package publish. Commit badges/coverage.svg; the raw
# TestResults/ output is git-ignored.
#
# Usage:  pwsh ./coverage.ps1   (or)   powershell -File coverage.ps1
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Remove-Item TestResults -Recurse -Force -ErrorAction SilentlyContinue

dotnet test BonniePDF.Tests/BonniePDF.Tests.csproj `
    --collect:"XPlat Code Coverage" --results-directory TestResults --nologo

# ReportGenerator runs as a local tool (see .config/dotnet-tools.json).
dotnet tool restore | Out-Null
dotnet reportgenerator "-reports:TestResults/**/coverage.cobertura.xml" `
    "-targetdir:TestResults/badges" "-reporttypes:Badges" | Out-Null

New-Item -ItemType Directory -Force -Path badges | Out-Null
Copy-Item TestResults/badges/badge_linecoverage.svg badges/coverage.svg -Force
Write-Host "coverage badge -> badges/coverage.svg"
