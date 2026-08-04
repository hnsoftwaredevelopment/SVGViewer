param(
    [string]$Configuration = "Release",
    [string]$InnoCompilerPath = "",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\SVGViewer\SVGViewer.csproj"
$publishDir = Join-Path $repoRoot "src\SVGViewer\bin\Publish"
$installerDir = Join-Path $repoRoot "src\SVGViewer\bin\Installer"
$scriptPath = Join-Path $PSScriptRoot "SVGViewer.iss"

New-Item -ItemType Directory -Force $publishDir, $installerDir | Out-Null

if (-not $SkipPublish) {
    dotnet publish $projectPath `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained false `
        --output $publishDir `
        /p:PublishSingleFile=true `
        /p:PublishReadyToRun=true
}

if (-not (Test-Path (Join-Path $publishDir "SVGViewer.exe"))) {
    throw "Publish output was not found. Run without -SkipPublish first."
}

if ([string]::IsNullOrWhiteSpace($InnoCompilerPath)) {
    $candidates = @(
        "c:\Program Files\Inno Setup 7\ISCC.exe",
        "C:\Users\hnijk\OneDrive\DevOps\hnsoftwaredevelopment\InnoSetup\Inno-All-in-One-Setup-master\IsPack_5_5_2\isfiles-unicode\ISCC.exe",
        "C:\Users\hnijk\OneDrive\DevOps\hnsoftwaredevelopment\InnoSetup\Inno-All-in-One-Setup-master\IsPack_5_5_2\isfiles\ISCC.exe"
    )

    $InnoCompilerPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not (Test-Path $InnoCompilerPath)) {
    throw "ISCC.exe was not found. Install Inno Setup 6 or pass -InnoCompilerPath."
}

& $InnoCompilerPath $scriptPath

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compiler failed with exit code $LASTEXITCODE."
}

Get-ChildItem $installerDir -Filter "SVGViewerSetup-*.exe" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
