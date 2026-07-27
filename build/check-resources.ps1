<#
.SYNOPSIS
    Checks the .resx resources for completeness.

.DESCRIPTION
    Two classes of bug show up as "!SomeKey!" in the running UI:
      1. a key present in the Dutch (neutral) file but missing in en/de;
      2. a key referenced from XAML or C# that exists in no resource file.
    This script catches both. Exit code 1 when anything is wrong.
#>

[CmdletBinding()]
param(
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $scriptDir = if ($PSScriptRoot) { $PSScriptRoot }
                 elseif ($PSCommandPath) { Split-Path -Parent $PSCommandPath }
                 else { 'c:\DevOps\hnsoftwaredevelopment\SVGViewer\build' }
    $ProjectRoot = Split-Path -Parent $scriptDir
}

$appDir       = Join-Path $ProjectRoot 'src\SVGViewer'
$resourcesDir = Join-Path $appDir 'Resources'
$problems     = @()

function Get-ResxKeys {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return @() }
    $xml = [xml](Get-Content -Path $Path -Raw)
    return @($xml.root.data | ForEach-Object { $_.name } | Where-Object { $_ })
}

$neutralPath = Join-Path $resourcesDir 'Strings.resx'
$neutral     = Get-ResxKeys $neutralPath

if ($neutral.Count -eq 0) {
    Write-Host "FATAL: no keys found in Strings.resx" -ForegroundColor Red
    exit 1
}

Write-Host "Neutral (nl) keys: $($neutral.Count)"

# --- 1. Every neutral key must exist in the translated files -----------------
foreach ($culture in @('en', 'de')) {
    $path = Join-Path $resourcesDir "Strings.$culture.resx"
    $keys = Get-ResxKeys $path

    $missing = @($neutral | Where-Object { $keys -notcontains $_ })
    $extra   = @($keys    | Where-Object { $neutral -notcontains $_ })

    Write-Host "  $culture : $($keys.Count) keys"

    foreach ($key in $missing) { $problems += "[$culture] missing key: $key" }
    foreach ($key in $extra)   { $problems += "[$culture] key not in neutral file: $key" }
}

# --- 2. Every key used in XAML / C# must exist -------------------------------
$sourceFiles = Get-ChildItem -Path $appDir -Recurse -Include *.xaml, *.cs -File |
               Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$usedKeys = New-Object System.Collections.Generic.HashSet[string]

foreach ($file in $sourceFiles) {
    $text = Get-Content -Path $file.FullName -Raw

    # XAML indexer bindings:  Path=[SomeKey]
    foreach ($match in [regex]::Matches($text, 'Path=\[(\w+)\]')) {
        [void]$usedKeys.Add($match.Groups[1].Value)
    }

    # C# lookups:  Loc.Get("SomeKey") / Loc.Format("SomeKey", ...)
    foreach ($match in [regex]::Matches($text, 'Loc\.(?:Get|Format)\("(\w+)"')) {
        [void]$usedKeys.Add($match.Groups[1].Value)
    }

    # Status messages:  SetStatus("SomeKey", ...)
    foreach ($match in [regex]::Matches($text, 'SetStatus\("(\w+)"')) {
        [void]$usedKeys.Add($match.Groups[1].Value)
    }

    # Dropdown entries:  new(PreviewSize.Large, "SizeLarge")
    foreach ($match in [regex]::Matches($text, 'new\((?:PreviewSize|FolderFilterMode)\.\w+,\s*"(\w+)"\s*\)')) {
        [void]$usedKeys.Add($match.Groups[1].Value)
    }
}

Write-Host "Keys referenced in source: $($usedKeys.Count)"

foreach ($key in $usedKeys) {
    if ($neutral -notcontains $key) {
        $problems += "[source] key used but not defined: $key"
    }
}

# --- 3. Report ---------------------------------------------------------------
$unused = @($neutral | Where-Object { -not $usedKeys.Contains($_) })
if ($unused.Count -gt 0) {
    Write-Host "Note: $($unused.Count) defined key(s) not referenced in source (may be intentional):" -ForegroundColor DarkYellow
    $unused | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkYellow }
}

if ($problems.Count -gt 0) {
    Write-Host ""
    Write-Host "PROBLEMS FOUND:" -ForegroundColor Red
    $problems | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

Write-Host ""
Write-Host "Resources OK: all cultures complete and all referenced keys defined." -ForegroundColor Green
