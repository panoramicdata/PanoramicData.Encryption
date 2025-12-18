<#
.SYNOPSIS
    Publishes the PanoramicData.Encryption package to nuget.org

.PARAMETER SkipTests
    Skip running unit tests before publishing

.EXAMPLE
    .\Publish.ps1
    .\Publish.ps1 -SkipTests
#>

param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

# Step 1: Check for git porcelain (clean working tree)
Write-Host "Checking for clean git working tree..." -ForegroundColor Cyan
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Error "Git working tree is not clean. Please commit or stash your changes before publishing."
    exit 1
}
Write-Host "Git working tree is clean." -ForegroundColor Green

# Step 2: Determine the Nerdbank git version
Write-Host "Determining Nerdbank.GitVersioning version..." -ForegroundColor Cyan
$versionOutput = nbgv get-version -f json 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to get Nerdbank.GitVersioning version. Ensure nbgv tool is installed: dotnet tool install -g nbgv"
    exit 1
}
$versionInfo = $versionOutput | ConvertFrom-Json
$version = $versionInfo.NuGetPackageVersion
Write-Host "Package version: $version" -ForegroundColor Green

# Step 3: Check that nuget-key.txt exists, has content and is gitignored
Write-Host "Checking nuget-key.txt..." -ForegroundColor Cyan
$nugetKeyPath = Join-Path $PSScriptRoot "nuget-key.txt"

if (-not (Test-Path $nugetKeyPath)) {
    Write-Error "nuget-key.txt does not exist. Create it in the solution root with your NuGet API key."
    exit 1
}

$nugetKey = (Get-Content $nugetKeyPath -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($nugetKey)) {
    Write-Error "nuget-key.txt is empty. Add your NuGet API key to the file."
    exit 1
}

# Check if nuget-key.txt is gitignored
$gitCheckIgnore = git check-ignore "nuget-key.txt" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "nuget-key.txt is not gitignored. Add it to .gitignore before publishing."
    exit 1
}
Write-Host "nuget-key.txt exists, has content, and is gitignored." -ForegroundColor Green

# Step 4: Run unit tests (unless -SkipTests is specified)
if (-not $SkipTests) {
    Write-Host "Running unit tests..." -ForegroundColor Cyan
    dotnet test --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Unit tests failed."
        exit 1
    }
    Write-Host "Unit tests passed." -ForegroundColor Green
} else {
    Write-Host "Skipping unit tests." -ForegroundColor Yellow
}

# Step 5: Build and pack the project
Write-Host "Building and packing the project..." -ForegroundColor Cyan
dotnet pack PanoramicData.Encryption\PanoramicData.Encryption.csproj --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to pack the project."
    exit 1
}

# Find the generated package
$packagePath = Get-ChildItem -Path "PanoramicData.Encryption\bin\Release" -Filter "*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $packagePath) {
    Write-Error "Could not find the generated NuGet package."
    exit 1
}
Write-Host "Package created: $($packagePath.FullName)" -ForegroundColor Green

# Step 6: Publish to nuget.org
Write-Host "Publishing to nuget.org..." -ForegroundColor Cyan
dotnet nuget push $packagePath.FullName --api-key $nugetKey --source https://api.nuget.org/v3/index.json --skip-duplicate
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish to nuget.org."
    exit 1
}

Write-Host "Successfully published $version to nuget.org!" -ForegroundColor Green
exit 0
