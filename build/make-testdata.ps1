$ErrorActionPreference = 'Stop'
$root = 'C:\Temp\SVGTestData'

if (Test-Path $root) { Remove-Item $root -Recurse -Force }

# Structure:
#  A\Icons          -> 2 svg   (should be marked)
#  A\Empty          -> nothing
#  B\deep\deeper    -> 1 svg   (tests ancestor marking)
#  C               -> only .txt (must NOT be marked)
$folders = @(
    "$root\A\Icons",
    "$root\A\Empty",
    "$root\B\deep\deeper",
    "$root\C"
)
foreach ($f in $folders) { New-Item -ItemType Directory -Force -Path $f | Out-Null }

$svg = '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24"><circle cx="12" cy="12" r="10" fill="teal"/></svg>'

Set-Content -Path "$root\A\Icons\one.svg"          -Value $svg
Set-Content -Path "$root\A\Icons\two.svg"          -Value $svg
Set-Content -Path "$root\B\deep\deeper\buried.svg" -Value $svg
Set-Content -Path "$root\C\notes.txt"              -Value 'not an svg'
Set-Content -Path "$root\C\image.png"              -Value 'not an svg'

Write-Host "Test data created under $root"
Get-ChildItem $root -Recurse -File | ForEach-Object { Write-Host "  $($_.FullName)" }
