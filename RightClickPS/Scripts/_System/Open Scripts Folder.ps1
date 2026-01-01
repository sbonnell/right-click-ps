<#
@Name: Open Scripts Folder
@Extensions: *
@TargetType: Both
@RunAsAdmin: false
#>

# Open Scripts Folder - Opens the user's scripts folder in Windows Explorer
# This script reads config.json from the application directory and opens
# the configured scriptsPath in Windows Explorer.

# Determine the app directory (where the exe is located)
# The script is executed via RightClickPS.exe, so we find the exe's directory
# by looking at the parent directory of the SystemScripts folder
$scriptDir = Split-Path -Parent $PSScriptRoot
$appDir = Split-Path -Parent $scriptDir

# Alternative: If we're in SystemScripts/_System, go up two levels
if ((Split-Path -Leaf $PSScriptRoot) -eq "_System") {
    $appDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

# Path to config.json
$configPath = Join-Path $appDir "config.json"

# Check if config.json exists
if (-not (Test-Path $configPath)) {
    [System.Windows.Forms.MessageBox]::Show(
        "Configuration file not found: $configPath",
        "RightClickPS - Error",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
    exit 1
}

# Read and parse config.json
try {
    $configContent = Get-Content -Path $configPath -Raw -Encoding UTF8
    $config = $configContent | ConvertFrom-Json
}
catch {
    [System.Windows.Forms.MessageBox]::Show(
        "Failed to read configuration file: $_",
        "RightClickPS - Error",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
    exit 1
}

# Get scriptsPath from config
$scriptsPath = $config.scriptsPath

# Validate scriptsPath is set
if ([string]::IsNullOrWhiteSpace($scriptsPath)) {
    [System.Windows.Forms.MessageBox]::Show(
        "scriptsPath is not configured in config.json",
        "RightClickPS - Error",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
    exit 1
}

# Expand environment variables in the path
$scriptsPath = [Environment]::ExpandEnvironmentVariables($scriptsPath)

# Resolve relative paths (relative to app directory)
if (-not [System.IO.Path]::IsPathRooted($scriptsPath)) {
    $scriptsPath = Join-Path $appDir $scriptsPath
}

# Normalize the path
$scriptsPath = [System.IO.Path]::GetFullPath($scriptsPath)

# Check if the scripts folder exists
if (-not (Test-Path $scriptsPath)) {
    $result = [System.Windows.Forms.MessageBox]::Show(
        "Scripts folder does not exist: $scriptsPath`n`nWould you like to create it?",
        "RightClickPS - Scripts Folder Not Found",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Question
    )

    if ($result -eq [System.Windows.Forms.DialogResult]::Yes) {
        try {
            New-Item -ItemType Directory -Path $scriptsPath -Force | Out-Null
        }
        catch {
            [System.Windows.Forms.MessageBox]::Show(
                "Failed to create scripts folder: $_",
                "RightClickPS - Error",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Error
            )
            exit 1
        }
    }
    else {
        exit 0
    }
}

# Open the scripts folder in Windows Explorer
try {
    Start-Process explorer.exe -ArgumentList "`"$scriptsPath`""
}
catch {
    [System.Windows.Forms.MessageBox]::Show(
        "Failed to open scripts folder: $_",
        "RightClickPS - Error",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
    exit 1
}
