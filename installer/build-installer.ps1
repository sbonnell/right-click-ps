# Build script for RightClickPS installer
# Requires: .NET 8 SDK, Inno Setup 6

param(
    [switch]$SkipBuild,
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

Write-Host "=== RightClickPS Installer Build ===" -ForegroundColor Cyan
Write-Host ""

# Check for Inno Setup
$innoSetupPath = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $innoSetupPath) {
    Write-Host "ERROR: Inno Setup 6 not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please download and install Inno Setup from:" -ForegroundColor Yellow
    Write-Host "https://jrsoftware.org/isdl.php" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "Found Inno Setup: $innoSetupPath" -ForegroundColor Green

# Step 1: Build/Publish the application
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Step 1: Publishing application..." -ForegroundColor Yellow

    $publishArgs = @(
        "publish",
        "$projectRoot\src\RightClickPS\RightClickPS.csproj",
        "-c", "Release",
        "-r", "win-x64",
        "-o", "$projectRoot\publish"
    )

    if ($SelfContained) {
        $publishArgs += "--self-contained", "true"
        Write-Host "  Mode: Self-contained (includes .NET runtime)" -ForegroundColor Gray
    } else {
        $publishArgs += "--self-contained", "false"
        Write-Host "  Mode: Framework-dependent (requires .NET 8 runtime)" -ForegroundColor Gray
    }

    & dotnet @publishArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: dotnet publish failed!" -ForegroundColor Red
        exit 1
    }

    Write-Host "  Published successfully to: $projectRoot\publish" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Step 1: Skipping build (using existing publish folder)" -ForegroundColor Yellow
}

# Step 2: Ensure dist folder exists
$distFolder = Join-Path $projectRoot "dist"
if (-not (Test-Path $distFolder)) {
    New-Item -ItemType Directory -Path $distFolder | Out-Null
}

# Step 3: Compile the installer
Write-Host ""
Write-Host "Step 2: Compiling installer..." -ForegroundColor Yellow

$issFile = Join-Path $scriptDir "RightClickPS.iss"

& $innoSetupPath $issFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Inno Setup compilation failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Installer created at:" -ForegroundColor White
Get-ChildItem "$distFolder\*.exe" | ForEach-Object {
    Write-Host "  $($_.FullName)" -ForegroundColor Cyan
    Write-Host "  Size: $([math]::Round($_.Length / 1MB, 2)) MB" -ForegroundColor Gray
}
Write-Host ""
