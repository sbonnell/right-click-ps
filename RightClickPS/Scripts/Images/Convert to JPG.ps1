<#
@Name: Convert to JPG
@Description: Converts image files to JPEG format
@Extensions: .png,.bmp,.gif,.webp
@TargetType: Files
@RunAsAdmin: false
#>

# Convert selected image files to JPEG format using .NET System.Drawing
# Requires Windows PowerShell or .NET runtime with System.Drawing support

Add-Type -AssemblyName System.Drawing

foreach ($file in $SelectedFiles) {
    if (-not (Test-Path $file)) {
        Write-Warning "File not found: $file"
        continue
    }

    try {
        # Get file info
        $fileInfo = Get-Item $file
        $extension = $fileInfo.Extension.ToLower()

        # Skip if already a JPEG
        if ($extension -eq '.jpg' -or $extension -eq '.jpeg') {
            Write-Host "Skipping (already JPEG): $file"
            continue
        }

        # Create output path with .jpg extension
        $outputPath = [System.IO.Path]::ChangeExtension($file, '.jpg')

        # Handle case where output file already exists
        if (Test-Path $outputPath) {
            $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file)
            $directory = [System.IO.Path]::GetDirectoryName($file)
            $counter = 1
            do {
                $outputPath = Join-Path $directory "$baseName`_$counter.jpg"
                $counter++
            } while (Test-Path $outputPath)
        }

        # Load the image
        $image = [System.Drawing.Image]::FromFile($file)

        # Set JPEG encoder parameters for high quality
        $jpegCodec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
            Where-Object { $_.MimeType -eq 'image/jpeg' }

        $encoderParams = New-Object System.Drawing.Imaging.EncoderParameters(1)
        $encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
            [System.Drawing.Imaging.Encoder]::Quality,
            [long]90
        )

        # Save as JPEG
        $image.Save($outputPath, $jpegCodec, $encoderParams)
        $image.Dispose()

        Write-Host "Converted: $file -> $outputPath"
    }
    catch {
        Write-Error "Failed to convert $file`: $_"
    }
}

Write-Host "`nConversion complete. Press any key to close..."
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
