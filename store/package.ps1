param(
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$publishDir = Join-Path $root 'dist\store\app-x64'
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Write-Host 'Publishing the x64 release build for the Store package...'
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
& dotnet publish 'src\DisposableScreenshots\DisposableScreenshots.csproj' `
    -c Release `
    -p:Platform=x64 `
    -p:PlatformTarget=x64 `
    -p:DebugType=None `
    -p:AllowedReferenceRelatedFileExtensions=none `
    -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host 'Restoring MSIX build tools...'
& dotnet restore 'store\BuildTools.csproj' --nologo
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}

$nugetLine = (& dotnet nuget locals global-packages --list | Select-Object -First 1)
$nugetRoot = (($nugetLine -split ':\s*', 2)[1]).Trim()
$buildToolsVersion = '10.0.26100.9169'
$sdkVersion = '10.0.26100.0'
$makeAppx = Join-Path $nugetRoot "microsoft.windows.sdk.buildtools\$buildToolsVersion\bin\$sdkVersion\x64\makeappx.exe"

if (-not (Test-Path $makeAppx)) {
    throw "makeappx.exe was not found at $makeAppx. Restore store\BuildTools.csproj and try again."
}

$storeDir = Join-Path $root 'dist\store'
$stageDir = Join-Path $storeDir 'stage'
$packagePath = Join-Path $storeDir 'dispoSS_1.0.0.0_x64.msix'

if (Test-Path $stageDir) {
    Remove-Item $stageDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $stageDir | Out-Null

Write-Host 'Staging the published application...'
Get-ChildItem $publishDir -File | ForEach-Object {
    Copy-Item $_.FullName $stageDir -Force
}

Copy-Item 'store\Package.appxmanifest' (Join-Path $stageDir 'AppxManifest.xml') -Force
Copy-Item 'store\Assets' $stageDir -Recurse -Force

Write-Host "Packing $packagePath ..."
& $makeAppx pack /d $stageDir /p $packagePath /o
if ($LASTEXITCODE -ne 0) {
    throw "makeappx failed with exit code $LASTEXITCODE."
}

$package = Get-Item $packagePath
$hash = (Get-FileHash $package.FullName -Algorithm SHA256).Hash
Write-Host ''
Write-Host "Package: $($package.FullName)"
Write-Host "Size:    $($package.Length) bytes"
Write-Host "SHA256:  $hash"
