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
$InformationPreference = 'Continue'

# Step 1: Check for git porcelain (clean working tree)
Write-Information "Checking for clean git working tree..."
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Error "Git working tree is not clean. Please commit or stash your changes before publishing."
    exit 1
}
Write-Information "Git working tree is clean."

# Step 2: Determine the Nerdbank git version
Write-Information "Determining Nerdbank.GitVersioning version..."
$versionOutput = nbgv get-version -f json 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to get Nerdbank.GitVersioning version. Ensure nbgv tool is installed: dotnet tool install -g nbgv"
    exit 1
}
$versionInfo = $versionOutput | ConvertFrom-Json
$version = $versionInfo.NuGetPackageVersion
Write-Information "Package version: $version"

# Step 3: Check that nuget-key.txt exists, has content and is gitignored
Write-Information "Checking nuget-key.txt..."
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
Write-Information "nuget-key.txt exists, has content, and is gitignored."

# Step 4: Run unit tests (unless -SkipTests is specified)
if (-not $SkipTests) {
    Write-Information "Running unit tests..."
    dotnet test --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Unit tests failed."
        exit 1
    }
    Write-Information "Unit tests passed."
} else {
    Write-Warning "Skipping unit tests."
}

# Step 5: Build and pack the project
Write-Information "Building and packing the project..."
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
Write-Information "Package created: $($packagePath.FullName)"

# Step 6: Publish to nuget.org
Write-Information "Publishing to nuget.org..."
dotnet nuget push $packagePath.FullName --api-key $nugetKey --source https://api.nuget.org/v3/index.json --skip-duplicate
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish to nuget.org."
    exit 1
}

Write-Information "Successfully published $version to nuget.org!"
exit 0
