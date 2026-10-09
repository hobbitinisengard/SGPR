param(
    [string]$SourceRoot = 'C:\Users\bernz\Desktop\SGP Reversed',
    [string]$OutputDirectory,
    [string]$Executable,
    [string]$AssetsRoot
)
$ErrorActionPreference = 'Stop'
$SourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
if (!$Executable) { $Executable = Join-Path $SourceRoot 'tools\physics-comparison\out\RelWithDebInfo\StuntGP_NEW.exe' }
if (!(Test-Path -LiteralPath $Executable)) { throw 'Najpierw uruchom Build-Original.ps1. Stary StuntGP_NEW-R1/StuntGP_NEW.exe nie zawiera nowego pomiaru.' }
if (!$AssetsRoot) { $AssetsRoot = Join-Path $SourceRoot 'StuntGP_NEW-R1' }
if (!(Test-Path -LiteralPath (Join-Path $AssetsRoot 'data\retail\wads'))) { throw 'Podaj -AssetsRoot: katalog zawierajacy data/retail/wads.' }
if (!$OutputDirectory) { $OutputDirectory = Join-Path $SourceRoot 'tools\physics-comparison\captures' }
$previous = $env:SGPR_COMPARISON_DIR
try {
    $env:SGPR_COMPARISON_DIR = $OutputDirectory
    # This is an interactive game launched by the user; use its normal window.
    Start-Process -FilePath $Executable -ArgumentList @('--root', ('"' + $AssetsRoot + '"')) -WorkingDirectory $AssetsRoot -Wait
} finally { $env:SGPR_COMPARISON_DIR = $previous }
Write-Host "CSV: $OutputDirectory\original-*\camera.csv oraz tyres.csv"
