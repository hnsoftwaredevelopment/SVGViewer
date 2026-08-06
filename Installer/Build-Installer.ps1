[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$InnoCompilerPath = $env:INNO_SETUP_COMPILER,

    [switch]$SkipPublish,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repoRoot 'src\SVGViewer\SVGViewer.csproj'
$buildRoot = Join-Path $repoRoot 'Builds'
$publishDir = Join-Path $buildRoot 'Publish\win-x64'
$installerDir = Join-Path $buildRoot 'Installer'
$releaseDir = Join-Path $buildRoot 'Release'
$mainScript = Join-Path $PSScriptRoot 'Main.iss'

if ([string]::IsNullOrWhiteSpace($InnoCompilerPath)) {
    $defaultCompiler = 'C:\Program Files\Inno Setup 7\ISCC.exe'
    if (Test-Path -LiteralPath $defaultCompiler -PathType Leaf) {
        $InnoCompilerPath = $defaultCompiler
    }
}
if ([string]::IsNullOrWhiteSpace($InnoCompilerPath) -or -not (Test-Path -LiteralPath $InnoCompilerPath -PathType Leaf)) {
    throw 'Inno Setup 7 compiler not found. Supply -InnoCompilerPath or set INNO_SETUP_COMPILER.'
}
if ($Clean) {
    foreach ($directory in @($publishDir, $installerDir, $releaseDir)) {
        if (Test-Path -LiteralPath $directory) {
            Remove-Item -LiteralPath $directory -Recurse -Force
        }
    }
}
New-Item -ItemType Directory -Force -Path $publishDir, $installerDir, $releaseDir | Out-Null

if (-not $SkipPublish) {
    & dotnet publish $projectPath `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained false `
        --output $publishDir `
        /p:PublishSingleFile=true `
        /p:PublishReadyToRun=true
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }
}

$mainExecutable = 'SVGViewer.exe'
if (-not (Test-Path -LiteralPath (Join-Path $publishDir $mainExecutable) -PathType Leaf)) {
    throw "Expected executable '$mainExecutable' was not found in '$publishDir'. Run without -SkipPublish first."
}

[xml]$project = Get-Content -LiteralPath $projectPath
$version = @($project.Project.PropertyGroup.FileVersion | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })[0]
if ([string]::IsNullOrWhiteSpace($version)) { throw 'FileVersion is missing in SVGViewer.csproj.' }

$isccArguments = @(
    "/DPublishDir=$publishDir",
    "/DOutputDir=$installerDir",
    "/DProductVersion=$version",
    "/DMainExecutable=$mainExecutable",
    $mainScript
)
& $InnoCompilerPath @isccArguments
if ($LASTEXITCODE -ne 0) { throw "ISCC.exe failed with exit code $LASTEXITCODE." }

$installer = Get-ChildItem -LiteralPath $installerDir -Filter "SVGViewerSetup-$version.exe" -File | Select-Object -First 1
if ($null -eq $installer) { throw "Expected installer SVGViewerSetup-$version.exe was not created." }

$hash = Get-FileHash -LiteralPath $installer.FullName -Algorithm SHA256
"$($hash.Hash) *$($installer.Name)" | Set-Content -LiteralPath (Join-Path $releaseDir "$($installer.Name).sha256") -Encoding ascii
$manifest = [ordered]@{
    product = 'SVGViewer'
    version = $version
    runtimeIdentifier = 'win-x64'
    deployment = 'framework-dependent'
    dotNetDesktopRuntime = '8.0.29'
    installer = $installer.Name
    sha256 = $hash.Hash
    createdUtc = [DateTime]::UtcNow.ToString('o')
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $releaseDir 'release-manifest.json') -Encoding utf8
Copy-Item -LiteralPath $installer.FullName -Destination (Join-Path $releaseDir $installer.Name) -Force

Write-Host "Installer created: $($installer.FullName)"
Write-Host "Release manifest: $(Join-Path $releaseDir 'release-manifest.json')"
