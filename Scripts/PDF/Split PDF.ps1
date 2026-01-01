<#
@Name: Split PDF
@Description: Splits a PDF file into one PDF per page
@Extensions: .pdf
@TargetType: Files
@RunAsAdmin: false
#>

# Split a PDF file into individual pages

# Load PdfSharp from lib folder
$pdfSharpPath = Join-Path $PSScriptRoot "..\..\lib\PdfSharp.dll"
if (-not (Test-Path $pdfSharpPath)) {
    Write-Host "PdfSharp.dll not found at: $pdfSharpPath" -ForegroundColor Red
    Write-Host "Press any key to close..."
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
    exit 1
}
Add-Type -Path $pdfSharpPath

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
        $outputDir = Join-Path $fileInfo.DirectoryName "$baseName`_pages"

        Write-Host "Splitting: $($fileInfo.Name)" -ForegroundColor Cyan

        # Open source document
        $inputDoc = [PdfSharp.Pdf.IO.PdfReader]::Open($file, [PdfSharp.Pdf.IO.PdfDocumentOpenMode]::Import)
        $pageCount = $inputDoc.PageCount

        if ($pageCount -eq 1) {
            Write-Host "  PDF has only 1 page, nothing to split." -ForegroundColor Yellow
            continue
        }

        # Create output directory
        if (-not (Test-Path $outputDir)) {
            New-Item -ItemType Directory -Path $outputDir | Out-Null
        }

        Write-Host "  Extracting $pageCount pages..."

        # Calculate padding for page numbers
        $padWidth = $pageCount.ToString().Length

        for ($i = 0; $i -lt $pageCount; $i++) {
            $pageNum = ($i + 1).ToString().PadLeft($padWidth, '0')
            $outputPath = Join-Path $outputDir "$baseName`_page$pageNum.pdf"

            # Create new document with single page
            $outputDoc = New-Object PdfSharp.Pdf.PdfDocument
            $null = $outputDoc.AddPage($inputDoc.Pages[$i])
            $outputDoc.Save($outputPath)
            $outputDoc.Close()

            Write-Host "    Page $($i + 1)/$pageCount saved"
        }

        Write-Host "  Output folder: $outputDir" -ForegroundColor Green
        Write-Host ""
    }
    catch {
        Write-Error "Failed to split $file`: $_"
    }
}

Write-Host "Split complete. Press any key to close..."
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
