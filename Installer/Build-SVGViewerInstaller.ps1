[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$InnoCompilerPath = '',
    [switch]$SkipPublish,
    [switch]$Clean
)

& (Join-Path $PSScriptRoot 'Build-Installer.ps1') @PSBoundParameters
