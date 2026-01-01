<#
@Name: Edit Configuration
@Extensions: *
@TargetType: Both
@RunAsAdmin: false
#>

# Determine the app directory (where the exe is located)
# When this script runs, it's invoked by RightClickPS.exe, so we need to find the exe location
# The exe passes itself as $PSScriptRoot's parent context, but we need to find config.json
# which is in the same directory as the exe

# Get the directory where this script is located
$scriptDir = $PSScriptRoot

# Navigate up from SystemScripts\_System to the app root directory
# Script is in: AppDir\SystemScripts\_System\Open Config.ps1
# Config is in: AppDir\config.json
$appDir = Split-Path -Parent (Split-Path -Parent $scriptDir)

# Build the path to config.json
$configPath = Join-Path $appDir "config.json"

# Check if config.json exists
if (Test-Path $configPath) {
    # Open config.json with the default text editor
    Start-Process $configPath
} else {
    # Show error message if config.json is not found
    # Use Write-Error which will be captured by ScriptExecutor and displayed via MessageBox
    Write-Error "Configuration file not found at: $configPath"
    exit 1
}
