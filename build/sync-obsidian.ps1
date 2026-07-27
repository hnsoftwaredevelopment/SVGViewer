<#
.SYNOPSIS
    Copies all Markdown documentation from the SVGViewer project into the
    Obsidian vault so the docs stay in sync.

.DESCRIPTION
    Run after every milestone/user story. Mirrors:
      - README.md
      - docs\**\*.md
    into the vault target, preserving the relative folder structure.
#>

[CmdletBinding()]
param(
    [string]$ProjectRoot,
    [string]$VaultTarget = 'c:\DevOps\hnsoftwaredevelopment\Obsidian\Development\HNSoftwareDevelopment\SVGViewer'
)

$ErrorActionPreference = 'Stop'

# Determine the project root (parent of this /build folder) with safe fallbacks.
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $scriptDir = if ($PSScriptRoot) { $PSScriptRoot }
                 elseif ($PSCommandPath) { Split-Path -Parent $PSCommandPath }
                 else { 'c:\DevOps\hnsoftwaredevelopment\SVGViewer\build' }
    $ProjectRoot = Split-Path -Parent $scriptDir
}

Write-Host "Project root : $ProjectRoot"
Write-Host "Vault target : $VaultTarget"

# Ensure the vault target exists
New-Item -ItemType Directory -Force -Path $VaultTarget | Out-Null

# 1) README.md at the vault root
Copy-Item -Path (Join-Path $ProjectRoot 'README.md') `
          -Destination (Join-Path $VaultTarget 'README.md') -Force

# 2) All markdown under docs\, preserving structure
$docsRoot = Join-Path $ProjectRoot 'docs'
$mdFiles  = Get-ChildItem -Path $docsRoot -Recurse -Filter *.md -File

foreach ($file in $mdFiles) {
    $relative = $file.FullName.Substring($docsRoot.Length).TrimStart('\')
    $dest     = Join-Path (Join-Path $VaultTarget 'docs') $relative
    $destDir  = Split-Path -Parent $dest
    New-Item -ItemType Directory -Force -Path $destDir | Out-Null
    Copy-Item -Path $file.FullName -Destination $dest -Force
    Write-Host "  synced: docs\$relative"
}

# 3) Copy images referenced by the docs so they render in Obsidian
$imgSource = Join-Path $docsRoot 'images'
if (Test-Path $imgSource) {
    $imgDest = Join-Path $VaultTarget 'docs\images'
    New-Item -ItemType Directory -Force -Path $imgDest | Out-Null
    Copy-Item -Path (Join-Path $imgSource '*') -Destination $imgDest -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Obsidian sync complete." -ForegroundColor Green
