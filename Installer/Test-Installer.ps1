[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ -PathType Leaf })]
    [string]$InstallerPath
)

$ErrorActionPreference = 'Stop'
$installer = Get-Item -LiteralPath $InstallerPath
if ($installer.Extension -ne '.exe') { throw 'InstallerPath must point to an .exe file.' }

$hash = Get-FileHash -LiteralPath $installer.FullName -Algorithm SHA256
Write-Host "Installer: $($installer.FullName)"
Write-Host "Size: $($installer.Length) bytes"
Write-Host "SHA-256: $($hash.Hash)"
Write-Host 'Static validation succeeded. Test installation, upgrade and uninstall in a clean VM before release.'
