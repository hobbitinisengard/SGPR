param(
    [string]$SourceRoot = 'C:\Users\bernz\Desktop\SGP Reversed',
    [string]$CMakePath
)
$ErrorActionPreference = 'Stop'
$script = Join-Path $SourceRoot 'tools\physics-comparison\Build-Acceleration.ps1'
if (!(Test-Path -LiteralPath $script)) { throw "Missing original build script: $script" }
& $script -SourceRoot $SourceRoot -CMakePath $CMakePath
