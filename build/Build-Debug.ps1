<#
.SYNOPSIS
    Builds the current SVG Viewer development version into Builds\Debug.

.DESCRIPTION
    Use this as the fixed test location for the desktop application. It does
    not run tests and does not touch the installer or benchmark outputs.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $projectRoot 'src\SVGViewer\SVGViewer.csproj'
$outputPath = Join-Path $projectRoot 'Builds\Debug'

& dotnet build $projectPath --configuration Debug --output $outputPath --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Debug build failed with exit code $LASTEXITCODE."
}

Write-Host "Test build ready: $(Join-Path $outputPath 'SVGViewer.exe')" -ForegroundColor Green
