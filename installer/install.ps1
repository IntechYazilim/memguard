param()

$ErrorActionPreference = "Stop"

$publisher = "Intech Yazilim"
$appName = "MemGuard"
$sourceExe = Join-Path $PSScriptRoot "MemGuard.exe"
$targetDir = Join-Path ${env:ProgramFiles} "$publisher\$appName"
$targetExe = Join-Path $targetDir "MemGuard.exe"
$startMenuDir = Join-Path ${env:ProgramData} "Microsoft\Windows\Start Menu\Programs\$publisher"
$startMenuShortcut = Join-Path $startMenuDir "$appName.lnk"
$desktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "$appName.lnk"

if (-not (Test-Path $sourceExe)) {
    throw "MemGuard.exe was not found in the installer payload."
}

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null

Copy-Item $sourceExe $targetExe -Force

$shell = New-Object -ComObject WScript.Shell

$shortcut = $shell.CreateShortcut($startMenuShortcut)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $targetDir
$shortcut.IconLocation = $targetExe
$shortcut.Save()

$desktop = $shell.CreateShortcut($desktopShortcut)
$desktop.TargetPath = $targetExe
$desktop.WorkingDirectory = $targetDir
$desktop.IconLocation = $targetExe
$desktop.Save()

Start-Process -FilePath $targetExe
