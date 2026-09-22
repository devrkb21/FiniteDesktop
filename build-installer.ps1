# Build script: publishes Finite as a single-file Windows exe and packages a zip.
# Usage:  powershell -ExecutionPolicy Bypass -File build-installer.ps1
# Output: installer/Finite-Desktop-<version>-win-x64.zip   (portable, no .NET needed)
#         installer/Finite-Desktop-<version>-setup.exe     (only if Inno Setup 6 is installed)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$version = "1.0.0"
$publishDir = Join-Path $root "publish"
$installerDir = Join-Path $root "installer"

Write-Host "==> Publishing self-contained single-file exe (win-x64)..."
dotnet publish (Join-Path $root "Finite.App/Finite.App.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -p:DebugType=none `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

New-Item -ItemType Directory -Force -Path $installerDir | Out-Null

$zipPath = Join-Path $installerDir "Finite-Desktop-$version-win-x64.zip"
Write-Host "==> Packaging $zipPath ..."
Compress-Archive -Path (Join-Path $publishDir "Finite.App.exe") -DestinationPath $zipPath -Force

# If Inno Setup 6 is installed, also build a real Setup.exe.
$iscc = @("C:\Program Files (x86)\Inno Setup 6\ISCC.exe", "C:\Program Files\Inno Setup 6\ISCC.exe") |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if ($iscc) {
    Write-Host "==> Building Setup.exe with Inno Setup..."
    & $iscc "/DAppVersion=$version" (Join-Path $installerDir "Finite.iss")
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }
} else {
    Write-Host "==> Inno Setup not found - skipped Setup.exe (portable exe + zip are ready)."
    Write-Host "    Install Inno Setup 6 (https://jrsoftware.org/isinfo.php) and rerun to also get a Setup.exe."
}

Write-Host "==> Done. Artifacts in $installerDir"
