param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectRoot "publish\$Runtime"
$issPath = Join-Path $PSScriptRoot "MemGuard.iss"
$payloadDir = Join-Path $PSScriptRoot "iexpress-payload"
$iexpressSed = Join-Path $PSScriptRoot "MemGuardIExpress.sed"
$iexpressOutputDir = Join-Path $PSScriptRoot "output"

Write-Host "Publishing MemGuard..."
dotnet publish (Join-Path $projectRoot "MemGuard.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishTrimmed=false `
    -o $publishDir

$pdbPath = Join-Path $publishDir "MemGuard.pdb"
if (Test-Path $pdbPath) {
    Remove-Item $pdbPath -Force
}

New-Item -ItemType Directory -Force -Path $payloadDir | Out-Null
New-Item -ItemType Directory -Force -Path $iexpressOutputDir | Out-Null
Copy-Item (Join-Path $publishDir "MemGuard.exe") (Join-Path $payloadDir "MemGuard.exe") -Force
Copy-Item (Join-Path $PSScriptRoot "install.ps1") (Join-Path $payloadDir "install.ps1") -Force

$iexpress = Get-Command iexpress -ErrorAction SilentlyContinue
if ($iexpress) {
    Write-Host "Building IExpress setup..."
    & $iexpress.Source /N $iexpressSed | Out-Host
}

$iscc = (Get-Command ISCC -ErrorAction SilentlyContinue)
if (-not $iscc) {
    $fallbackIscc = Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"
    if (Test-Path $fallbackIscc) {
        $iscc = @{ Source = $fallbackIscc }
    }
}
if (-not $iscc) {
    Write-Warning "Inno Setup compiler (ISCC.exe) was not found. Publish output is ready at: $publishDir"
    Write-Warning "Install Inno Setup, then run: ISCC `"$issPath`""
    exit 0
}

Write-Host "Building installer..."
Push-Location $PSScriptRoot
try {
    & $iscc.Source $issPath
}
finally {
    Pop-Location
}

Write-Host "Installer build complete."
