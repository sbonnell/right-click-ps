<#
@Name: Refresh Menus
@Extensions: *
@TargetType: Both
@RunAsAdmin: false
#>

# Refresh Menus - Re-scans scripts folder and updates context menu entries
# This script invokes RightClickPS.exe register to refresh the menu.

# Determine the app directory (where the exe is located)
# The script is executed via RightClickPS.exe, so we find the exe's directory
# by looking at the parent directory of the SystemScripts folder
$scriptDir = Split-Path -Parent $PSScriptRoot
$appDir = Split-Path -Parent $scriptDir

# Alternative: If we're in SystemScripts/_System, go up two levels
if ((Split-Path -Leaf $PSScriptRoot) -eq "_System") {
    $appDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

# Path to the RightClickPS executable
$exePath = Join-Path $appDir "RightClickPS.exe"

# Check if the executable exists
if (-not (Test-Path $exePath)) {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        "RightClickPS executable not found: $exePath",
        "RightClickPS - Error",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
    exit 1
}

# Run the register command to refresh context menu entries
try {
    $process = Start-Process -FilePath $exePath -ArgumentList "register" -Wait -PassThru -NoNewWindow
    $exitCode = $process.ExitCode

    if ($exitCode -eq 0) {
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.MessageBox]::Show(
            "Context menu entries have been refreshed successfully.",
            "RightClickPS - Refresh Complete",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    }
    else {
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.MessageBox]::Show(
            "Registration completed with exit code: $exitCode`nCheck the console output for details.",
            "RightClickPS - Warning",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Warning
        )
    }

    exit $exitCode
}
catch {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        "Failed to run register command: $_",
        "RightClickPS - Error",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
    exit 1
}
