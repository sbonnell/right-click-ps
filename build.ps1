# Build script for RightClickPS
# Outputs exe to current working directory, resources to RightClickPS subfolder
$ErrorActionPreference = 'Stop'

$srcDir = $PSScriptRoot
$outDir = (Get-Location).Path
$resourceDir = Join-Path $outDir "RightClickPS"
$tempPublish = Join-Path $env:TEMP "RightClickPS_publish_$(Get-Random)"

Write-Host "Building RightClickPS to: $outDir" -ForegroundColor Cyan

# Publish to temp location first (single-file for standalone exe)
Write-Host "  Publishing to temp location..." -ForegroundColor Gray
dotnet publish "$srcDir\src\RightClickPS\RightClickPS.csproj" -c Release -o $tempPublish --nologo -v q -p:PublishSingleFile=true -p:SelfContained=false --runtime win-x64

# Create resource directory
Write-Host "  Creating folder structure..." -ForegroundColor Gray
if (Test-Path $resourceDir) { Remove-Item $resourceDir -Recurse -Force }
New-Item -ItemType Directory -Path $resourceDir -Force | Out-Null

# Copy exe to root
Write-Host "  Copying executable to root..." -ForegroundColor Gray
Copy-Item "$tempPublish\RightClickPS.exe" $outDir -Force

# Copy all other published files to RightClickPS subfolder
Write-Host "  Copying dependencies to RightClickPS folder..." -ForegroundColor Gray
Get-ChildItem $tempPublish -Exclude "RightClickPS.exe" | Copy-Item -Destination $resourceDir -Recurse -Force

# Copy resources to RightClickPS subfolder
Write-Host "  Copying resources..." -ForegroundColor Gray
Copy-Item "$srcDir\config.json" $resourceDir -Force
if (Test-Path "$srcDir\Scripts") {
    Copy-Item "$srcDir\Scripts" "$resourceDir\Scripts" -Recurse -Force
}

# Copy icon files
Write-Host "  Copying icons..." -ForegroundColor Gray
$icons = Get-ChildItem $srcDir -Filter "stu-icon*" -ErrorAction SilentlyContinue
foreach ($icon in $icons) {
    Copy-Item $icon.FullName $resourceDir -Force
}

# Clean up temp
Remove-Item $tempPublish -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`nBuild complete!" -ForegroundColor Green
Write-Host "Output structure:" -ForegroundColor Cyan
Write-Host "  $outDir" -ForegroundColor White
Write-Host "    RightClickPS.exe" -ForegroundColor Gray
Write-Host "    RightClickPS\" -ForegroundColor Gray
Write-Host "      config.json, icons, scripts, dependencies" -ForegroundColor DarkGray
Write-Host "`nRun: .\RightClickPS.exe register" -ForegroundColor Yellow
