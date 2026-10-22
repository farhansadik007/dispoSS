param(
    [string]$IdentityName = 'PaperCraneLabs.DispoSSTest',
    [string]$PfxPath,
    [string]$PfxPassword,
    [switch]$Install,
    [switch]$SkipBuild,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not $SkipBuild) {
    Write-Host 'Building the Store staging folder...'
    & .\package.cmd
    if ($LASTEXITCODE -ne 0) {
        throw "package.cmd failed with exit code $LASTEXITCODE."
    }
}

$stage = Join-Path $root 'dist\store\stage'
if (-not (Test-Path $stage)) {
    throw "The staging folder was not found at $stage. Run .\package.cmd first."
}

$testRoot = Join-Path $root 'dist\store\test'
$testStage = Join-Path $testRoot 'stage'
if (Test-Path $testStage) {
    Remove-Item $testStage -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $testRoot | Out-Null
Copy-Item $stage $testStage -Recurse -Force

$manifestPath = Join-Path $testStage 'AppxManifest.xml'
$manifestText = Get-Content $manifestPath -Raw
$storeIdentity = 'PaperCraneLabs.DispoSS'
$identityPattern = 'Name="' + [regex]::Escape($storeIdentity) + '"'
if (([regex]::Matches($manifestText, $identityPattern)).Count -ne 1) {
    throw ('Expected exactly one Name="{0}" in the staged manifest.' -f $storeIdentity)
}
$manifestText = $manifestText.Replace("Name=`"$storeIdentity`"", "Name=`"$IdentityName`"")
Set-Content $manifestPath $manifestText -Encoding UTF8

[xml]$testManifest = Get-Content $manifestPath -Raw
$version = $testManifest.Package.Identity.Version
$applicationId = $testManifest.Package.Applications.Application.Id
if (-not $applicationId) {
    $applicationId = 'App'
}

$nugetLine = (& dotnet nuget locals global-packages --list | Select-Object -First 1)
$nugetRoot = (($nugetLine -split ':\s*', 2)[1]).Trim()
$buildToolsVersion = '10.0.26100.9169'
$sdkVersion = '10.0.26100.0'
$makeAppx = Join-Path $nugetRoot "microsoft.windows.sdk.buildtools\$buildToolsVersion\bin\$sdkVersion\x64\makeappx.exe"
$signTool = Join-Path $nugetRoot "microsoft.windows.sdk.buildtools\$buildToolsVersion\bin\$sdkVersion\x64\signtool.exe"

if (-not (Test-Path $makeAppx)) {
    throw "makeappx.exe was not found at $makeAppx."
}

$unsignedPath = Join-Path $testRoot "dispoSSTest_${version}_x64.msix"
$signedPath = Join-Path $testRoot "dispoSSTest_${version}_x64.test-signed.msix"

Write-Host "Packing the test package as $IdentityName ..."
& $makeAppx pack /d $testStage /p $unsignedPath /o
if ($LASTEXITCODE -ne 0) {
    throw "makeappx failed with exit code $LASTEXITCODE."
}

$installPath = $unsignedPath

if ($PfxPath) {
    if (-not $PfxPassword) {
        throw 'PfxPassword is required when PfxPath is provided.'
    }
    if (-not (Test-Path $PfxPath)) {
        throw "The PFX file was not found at $PfxPath."
    }
    if (-not (Test-Path $signTool)) {
        throw "signtool.exe was not found at $signTool."
    }

    Copy-Item $unsignedPath $signedPath -Force
    Write-Host 'Signing the test package...'
    & $signTool sign /fd SHA256 /a /f $PfxPath /p $PfxPassword $signedPath
    if ($LASTEXITCODE -ne 0) {
        throw "signtool failed with exit code $LASTEXITCODE."
    }
    $installPath = $signedPath
}
elseif ($Install) {
    throw 'Install requires -PfxPath and -PfxPassword so the package can be signed.'
}

$package = Get-Item $installPath
$hash = (Get-FileHash $package.FullName -Algorithm SHA256).Hash
Write-Host ''
Write-Host "Test identity: $IdentityName"
Write-Host "Package:       $($package.FullName)"
Write-Host "Size:          $($package.Length) bytes"
Write-Host "SHA256:        $hash"

if ($Install) {
    $existing = Get-AppxPackage -Name $IdentityName -ErrorAction SilentlyContinue
    if ($existing) {
        if (-not $Force) {
            throw "The test package is already installed. Re-run with -Force to replace it."
        }
        Write-Host "Removing the existing test package..."
        Remove-AppxPackage -Package $existing.PackageFullName
        Start-Sleep -Milliseconds 500
    }

    Write-Host 'Installing the test package...'
    Add-AppxPackage -Path $installPath -ForceApplicationShutdown
    $installed = Get-AppxPackage -Name $IdentityName
    $aumid = "$($installed.PackageFamilyName)!$applicationId"
    Write-Host ''
    Write-Host "Installed: $($installed.PackageFullName)"
    Write-Host "Launch:    explorer.exe shell:AppsFolder\$aumid"
}
else {
    Write-Host ''
    Write-Host 'The test package was built but not installed.'
    Write-Host 'Use -Install -Force after trusting the test certificate to install it.'
}
