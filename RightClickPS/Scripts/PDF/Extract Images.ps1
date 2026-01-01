<#
@Name: Extract Images
@Description: Extracts all images from PDF files
@Extensions: .pdf
@TargetType: Files
@RunAsAdmin: false
#>

# Extract images from PDF files

Add-Type -AssemblyName System.Drawing

# Load PdfSharp from lib folder
$pdfSharpPath = Join-Path $PSScriptRoot "..\..\lib\PdfSharp.dll"
if (-not (Test-Path $pdfSharpPath)) {
    Write-Host "PdfSharp.dll not found at: $pdfSharpPath" -ForegroundColor Red
    Write-Host "Press any key to close..."
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
    exit 1
}
Add-Type -Path $pdfSharpPath

function Export-PdfImages {
    param (
        [string]$PdfPath,
        [string]$OutputDir
    )

    $doc = [PdfSharp.Pdf.IO.PdfReader]::Open($PdfPath, [PdfSharp.Pdf.IO.PdfDocumentOpenMode]::ReadOnly)
    $imageCount = 0

    for ($pageIndex = 0; $pageIndex -lt $doc.PageCount; $pageIndex++) {
        $page = $doc.Pages[$pageIndex]
        $resources = $page.Elements.GetDictionary("/Resources")

        if ($null -eq $resources) { continue }

        $xObjects = $resources.Elements.GetDictionary("/XObject")
        if ($null -eq $xObjects) { continue }

        foreach ($item in $xObjects.Elements.Keys) {
            $xObject = $xObjects.Elements.GetDictionary($item)
            if ($null -eq $xObject) { continue }

            $subtype = $xObject.Elements.GetString("/Subtype")
            if ($subtype -ne "/Image") { continue }

            try {
                # Get image stream
                $stream = $xObject.Stream
                if ($null -eq $stream -or $stream.Value.Length -eq 0) { continue }

                $imageCount++
                $filter = $xObject.Elements.GetString("/Filter")

                # Determine file extension based on filter
                $extension = ".bin"
                if ($filter -eq "/DCTDecode") {
                    $extension = ".jpg"
                } elseif ($filter -eq "/FlateDecode") {
                    $extension = ".png"
                } elseif ($filter -eq "/JPXDecode") {
                    $extension = ".jp2"
                }

                $outputPath = Join-Path $OutputDir "image_p$($pageIndex + 1)_$imageCount$extension"

                # For JPEG images, write directly
                if ($filter -eq "/DCTDecode") {
                    [System.IO.File]::WriteAllBytes($outputPath, $stream.Value)
                    Write-Host "    Extracted: $(Split-Path $outputPath -Leaf)"
                }
                else {
                    # For other formats, try to decode and save
                    $width = $xObject.Elements.GetInteger("/Width")
                    $height = $xObject.Elements.GetInteger("/Height")
                    $bitsPerComponent = $xObject.Elements.GetInteger("/BitsPerComponent")

                    if ($width -gt 0 -and $height -gt 0) {
                        # Try to create image from raw data
                        try {
                            $ms = New-Object System.IO.MemoryStream(,$stream.UnfilteredValue)
                            $bitmap = New-Object System.Drawing.Bitmap($width, $height)

                            # Save raw data for manual processing
                            $rawPath = Join-Path $OutputDir "image_p$($pageIndex + 1)_$imageCount.raw"
                            [System.IO.File]::WriteAllBytes($rawPath, $stream.UnfilteredValue)
                            Write-Host "    Extracted (raw): $(Split-Path $rawPath -Leaf) (${width}x${height})"
                        }
                        catch {
                            # Save as-is
                            [System.IO.File]::WriteAllBytes($outputPath, $stream.Value)
                            Write-Host "    Extracted: $(Split-Path $outputPath -Leaf)"
                        }
                    }
                    else {
                        [System.IO.File]::WriteAllBytes($outputPath, $stream.Value)
                        Write-Host "    Extracted: $(Split-Path $outputPath -Leaf)"
                    }
                }
            }
            catch {
                Write-Warning "    Could not extract image: $_"
            }
        }
    }

    $doc.Close()
    return $imageCount
}

foreach ($file in $SelectedFiles) {
    if (-not ($file -match '\.pdf$')) {
        Write-Warning "Skipping non-PDF file: $file"
        continue
    }

    if (-not (Test-Path $file)) {
        Write-Warning "File not found: $file"
        continue
    }

    try {
        $fileInfo = Get-Item $file
        $baseName = $fileInfo.BaseName
        $outputDir = Join-Path $fileInfo.DirectoryName "$baseName`_images"

        Write-Host "Processing: $($fileInfo.Name)" -ForegroundColor Cyan

        # Create output directory
        if (-not (Test-Path $outputDir)) {
            New-Item -ItemType Directory -Path $outputDir | Out-Null
        }

        $count = Export-PdfImages -PdfPath $file -OutputDir $outputDir

        if ($count -eq 0) {
            Write-Host "  No images found in PDF." -ForegroundColor Yellow
            Remove-Item $outputDir -Force -ErrorAction SilentlyContinue
        }
        else {
            Write-Host "  Extracted $count image(s) to: $outputDir" -ForegroundColor Green
        }
        Write-Host ""
    }
    catch {
        Write-Error "Failed to process $file`: $_"
    }
}

Write-Host "Extraction complete. Press any key to close..."
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
