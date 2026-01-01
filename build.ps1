# Build script for RightClickPS
# Outputs to current working directory
$ErrorActionPreference = 'Stop'

$srcDir = $PSScriptRoot
$outDir = (Get-Location).Path

Write-Host "Building RightClickPS to: $outDir" -ForegroundColor Cyan
dotnet publish "$srcDir\src\RightClickPS\RightClickPS.csproj" -c Release -o $outDir

Write-Host "Copying resources..." -ForegroundColor Cyan
Copy-Item "$srcDir\config.json" $outDir -Force
if (Test-Path "$outDir\ExampleScripts") { Remove-Item "$outDir\ExampleScripts" -Recurse -Force }
if (Test-Path "$outDir\SystemScripts") { Remove-Item "$outDir\SystemScripts" -Recurse -Force }
Copy-Item "$srcDir\ExampleScripts" "$outDir\ExampleScripts" -Recurse -Force
Copy-Item "$srcDir\SystemScripts" "$outDir\SystemScripts" -Recurse -Force

Write-Host "`nBuild complete!" -ForegroundColor Green
Write-Host "Run: .\RightClickPS.exe register" -ForegroundColor Yellow
