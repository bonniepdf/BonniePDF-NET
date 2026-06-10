# Generates badges/coverage.svg (flat-square) from a local test + coverage run.
# No CI, no upload, no package publish, no extra tooling: it reads coverlet's
# cobertura output directly and writes the badge. Commit badges/coverage.svg;
# the raw TestResults/ output is git-ignored.
#
# Usage:  powershell -File coverage.ps1   (or)   pwsh ./coverage.ps1
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Remove-Item TestResults -Recurse -Force -ErrorAction SilentlyContinue

dotnet test BonniePDF.Tests/BonniePDF.Tests.csproj `
    --collect:"XPlat Code Coverage" --results-directory TestResults --nologo

$cobertura = Get-ChildItem -Path TestResults -Recurse -Filter coverage.cobertura.xml | Select-Object -First 1
if (-not $cobertura) { throw "No coverage.cobertura.xml found under TestResults/" }

[xml]$xml = Get-Content $cobertura.FullName
$pct = [math]::Round(([double]$xml.coverage.'line-rate') * 100, 1)

function Get-Color([double] $p) {
    if ($p -ge 90) { '#4c1' }
    elseif ($p -ge 80) { '#97ca00' }
    elseif ($p -ge 70) { '#a4a61d' }
    elseif ($p -ge 60) { '#dfb317' }
    elseif ($p -ge 50) { '#fe7d37' }
    else { '#e05d44' }
}

$label = 'coverage'
$message = "$pct%"
# Approximate text widths (Verdana 11px ~ 6.5px/char) + 10px horizontal padding.
$labelW = [int][math]::Round($label.Length * 6.5) + 10
$msgW = [int][math]::Round($message.Length * 6.5) + 10
$total = $labelW + $msgW
$color = Get-Color $pct
$labelMid = $labelW / 2
$msgMid = $labelW + $msgW / 2

# flat-square style: square corners, no gradient, no text shadow.
$svg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="$total" height="20" role="img" aria-label="${label}: $message">
  <title>${label}: $message</title>
  <g shape-rendering="crispEdges">
    <rect width="$labelW" height="20" fill="#555"/>
    <rect x="$labelW" width="$msgW" height="20" fill="$color"/>
  </g>
  <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" font-size="11">
    <text x="$labelMid" y="14">$label</text>
    <text x="$msgMid" y="14">$message</text>
  </g>
</svg>
"@

New-Item -ItemType Directory -Force -Path badges | Out-Null
[System.IO.File]::WriteAllText("$PSScriptRoot\badges\coverage.svg", $svg, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "coverage badge -> badges/coverage.svg ($message lines, flat-square)"
