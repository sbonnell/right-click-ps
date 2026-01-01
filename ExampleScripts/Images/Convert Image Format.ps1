<#
@Name: Convert Image Format
@Description: Convert images between formats (PNG, JPG, BMP, GIF, TIFF)
@Extensions: .png,.jpg,.jpeg,.bmp,.gif,.tiff,.tif,.webp
@TargetType: Files
@RunAsAdmin: false
#>

# Convert images between formats using .NET System.Drawing
# Presents a dialog to choose the target format

try {
    Add-Type -AssemblyName System.Drawing
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.Application]::EnableVisualStyles()
} catch {
    Write-Host "Error loading assemblies: $_"
    Read-Host "Press Enter to exit"
    exit 1
}

# Define supported output formats
$formats = @{
    'PNG'  = @{ Extension = '.png';  MimeType = 'image/png' }
    'JPEG' = @{ Extension = '.jpg';  MimeType = 'image/jpeg' }
    'BMP'  = @{ Extension = '.bmp';  MimeType = 'image/bmp' }
    'GIF'  = @{ Extension = '.gif';  MimeType = 'image/gif' }
    'TIFF' = @{ Extension = '.tiff'; MimeType = 'image/tiff' }
}

# Create format selection dialog
$form = New-Object System.Windows.Forms.Form
$form.Text = 'Convert Image Format'
$form.ClientSize = New-Object System.Drawing.Size(300, 200)
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$form.MinimizeBox = $false
$form.TopMost = $true

$label = New-Object System.Windows.Forms.Label
$label.Location = New-Object System.Drawing.Point(15, 15)
$label.Size = New-Object System.Drawing.Size(270, 20)
$label.Text = "Select output format for $($SelectedFiles.Count) file(s):"
$form.Controls.Add($label)

$listBox = New-Object System.Windows.Forms.ListBox
$listBox.Location = New-Object System.Drawing.Point(15, 40)
$listBox.Size = New-Object System.Drawing.Size(270, 70)
$listBox.SelectionMode = 'One'
foreach ($fmt in $formats.Keys | Sort-Object) {
    $listBox.Items.Add($fmt) | Out-Null
}
$listBox.SelectedIndex = 0
$form.Controls.Add($listBox)

# Quality slider for JPEG
$qualityLabel = New-Object System.Windows.Forms.Label
$qualityLabel.Location = New-Object System.Drawing.Point(15, 118)
$qualityLabel.Size = New-Object System.Drawing.Size(110, 20)
$qualityLabel.Text = 'JPEG Quality: 90%'
$form.Controls.Add($qualityLabel)

$qualityTrack = New-Object System.Windows.Forms.TrackBar
$qualityTrack.Location = New-Object System.Drawing.Point(125, 115)
$qualityTrack.Size = New-Object System.Drawing.Size(160, 30)
$qualityTrack.Minimum = 10
$qualityTrack.Maximum = 100
$qualityTrack.Value = 90
$qualityTrack.TickFrequency = 10
$qualityTrack.Add_ValueChanged({
    $qualityLabel.Text = "JPEG Quality: $($qualityTrack.Value)%"
})
$form.Controls.Add($qualityTrack)

$okButton = New-Object System.Windows.Forms.Button
$okButton.Location = New-Object System.Drawing.Point(110, 160)
$okButton.Size = New-Object System.Drawing.Size(80, 28)
$okButton.Text = 'Convert'
$okButton.DialogResult = [System.Windows.Forms.DialogResult]::OK
$form.AcceptButton = $okButton
$form.Controls.Add($okButton)

$cancelButton = New-Object System.Windows.Forms.Button
$cancelButton.Location = New-Object System.Drawing.Point(200, 160)
$cancelButton.Size = New-Object System.Drawing.Size(80, 28)
$cancelButton.Text = 'Cancel'
$cancelButton.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
$form.CancelButton = $cancelButton
$form.Controls.Add($cancelButton)

# Show dialog
$result = $form.ShowDialog()

if ($result -ne [System.Windows.Forms.DialogResult]::OK) {
    exit 0
}

$selectedFormat = $listBox.SelectedItem
$formatInfo = $formats[$selectedFormat]
$jpegQuality = $qualityTrack.Value

# Get the encoder for the selected format
$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
    Where-Object { $_.MimeType -eq $formatInfo.MimeType }

if (-not $codec) {
    [System.Windows.Forms.MessageBox]::Show(
        "Encoder not found for format: $selectedFormat",
        "RightClickPS - Error",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
    exit 1
}

# Process each file
$converted = 0
$skipped = 0
$failed = 0

foreach ($file in $SelectedFiles) {
    if (-not (Test-Path $file)) {
        Write-Warning "File not found: $file"
        $failed++
        continue
    }

    try {
        $fileInfo = Get-Item $file
        $currentExt = $fileInfo.Extension.ToLower()
        $targetExt = $formatInfo.Extension

        # Skip if already in target format
        if ($currentExt -eq $targetExt -or
            ($currentExt -eq '.jpeg' -and $targetExt -eq '.jpg') -or
            ($currentExt -eq '.jpg' -and $targetExt -eq '.jpeg') -or
            ($currentExt -eq '.tif' -and $targetExt -eq '.tiff') -or
            ($currentExt -eq '.tiff' -and $targetExt -eq '.tif')) {
            Write-Host "Skipping (already $selectedFormat): $file"
            $skipped++
            continue
        }

        # Create output path
        $outputPath = [System.IO.Path]::ChangeExtension($file, $targetExt)

        # Handle existing files
        if (Test-Path $outputPath) {
            $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file)
            $directory = [System.IO.Path]::GetDirectoryName($file)
            $counter = 1
            do {
                $outputPath = Join-Path $directory "$baseName`_$counter$targetExt"
                $counter++
            } while (Test-Path $outputPath)
        }

        # Load and convert
        $image = [System.Drawing.Image]::FromFile($file)

        # Set encoder parameters
        $encoderParams = New-Object System.Drawing.Imaging.EncoderParameters(1)

        if ($selectedFormat -eq 'JPEG') {
            # Quality parameter for JPEG
            $encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
                [System.Drawing.Imaging.Encoder]::Quality,
                [long]$jpegQuality
            )
        } else {
            # Default compression for other formats
            $encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
                [System.Drawing.Imaging.Encoder]::Quality,
                [long]100
            )
        }

        $image.Save($outputPath, $codec, $encoderParams)
        $image.Dispose()

        Write-Host "Converted: $($fileInfo.Name) -> $([System.IO.Path]::GetFileName($outputPath))"
        $converted++
    }
    catch {
        Write-Error "Failed to convert $file`: $_"
        $failed++
    }
}

# Show summary
$summary = "Conversion complete!`n`nConverted: $converted`nSkipped: $skipped`nFailed: $failed"
[System.Windows.Forms.MessageBox]::Show(
    $summary,
    "RightClickPS - Convert Image Format",
    [System.Windows.Forms.MessageBoxButtons]::OK,
    [System.Windows.Forms.MessageBoxIcon]::Information
)
