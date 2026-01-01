<#
@Name: Copy Path
@Description: Copies the full path of selected files to clipboard
@Extensions: *
@TargetType: Both
@RunAsAdmin: false
#>

# Copy the full paths of all selected files/folders to the clipboard
# Each path is on a separate line

if ($SelectedFiles.Count -eq 0) {
    Write-Host "No files or folders selected."
    exit
}

# Join all paths with newlines
$paths = $SelectedFiles -join "`r`n"

# Copy to clipboard
Set-Clipboard -Value $paths

# Display confirmation
Write-Host "Copied $($SelectedFiles.Count) path(s) to clipboard:"
Write-Host ""
foreach ($file in $SelectedFiles) {
    Write-Host "  $file"
}
Write-Host ""
Write-Host "Press any key to close..."
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
