param(
    [string]$SourceRoot = 'C:\Users\bernz\Desktop\SGP Reversed',
    [string]$CMakePath
)
$ErrorActionPreference = 'Stop'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (!(Test-Path -LiteralPath $vswhere)) { throw 'Brak Visual Studio Installer/vswhere. Zainstaluj Desktop development with C++.' }
$vsRoot = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (!$vsRoot) { throw 'Brak MSVC C++. W Visual Studio Installer dodaj Desktop development with C++ wraz z Windows SDK i CMake tools for Windows.' }
if (!$CMakePath) {
    $command = Get-Command cmake -ErrorAction SilentlyContinue
    if ($command) { $CMakePath = $command.Source }
    else { $CMakePath = Join-Path $vsRoot 'Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe' }
}
if (!(Test-Path -LiteralPath $CMakePath)) { throw 'Brak CMake. Zainstaluj CMake tools for Windows lub podaj -CMakePath.' }
$SourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$buildDir = Join-Path $SourceRoot 'tools\physics-comparison\build-windows'
$outputDir = Join-Path $SourceRoot 'tools\physics-comparison\out'
& $CMakePath -S $SourceRoot -B $buildDir -G 'Visual Studio 17 2022' -A x64 '-DBUILD_TESTING=OFF' "-DSGN_RUNTIME_OUTPUT_ROOT=$outputDir"
if ($LASTEXITCODE -ne 0) { throw 'Konfiguracja CMake nie powiodla sie.' }
& $CMakePath --build $buildDir --config RelWithDebInfo --target StuntGP_NEW --parallel 4
if ($LASTEXITCODE -ne 0) { throw 'Kompilacja oryginalu nie powiodla sie.' }
Write-Host "Gotowe: $outputDir\RelWithDebInfo\StuntGP_NEW.exe"
