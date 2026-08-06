<#
.SYNOPSIS
    Measures the current optimized SVG-drive scan on a folder.

.DESCRIPTION
    Runs a small .NET 8 benchmark host, rather than loading the application
    assembly into PowerShell. It therefore works in both Windows PowerShell 5.1
    and PowerShell 7+. The selected folder is never modified.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Container })]
    [string]$Path,

    [ValidateRange(1, 10)]
    [int]$Iterations = 1,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$benchmarkProject = Join-Path $projectRoot 'tools\SVGViewer.ScanBenchmark\SVGViewer.ScanBenchmark.csproj'
$benchmarkOutput = Join-Path $projectRoot 'Builds\Benchmark\Host\'
$benchmarkAssembly = Join-Path $benchmarkOutput "$Configuration\net8.0-windows\SVGViewer.ScanBenchmark.dll"

& dotnet build $benchmarkProject --configuration $Configuration "-p:BaseOutputPath=$benchmarkOutput" --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Scan benchmark build failed with exit code $LASTEXITCODE."
}

& dotnet $benchmarkAssembly $Path $Iterations
if ($LASTEXITCODE -ne 0) {
    throw "Scan benchmark failed with exit code $LASTEXITCODE."
}
