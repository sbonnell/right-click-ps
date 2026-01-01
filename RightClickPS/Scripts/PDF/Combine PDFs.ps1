<#
@Name: Combine PDFs
@Description: Merges selected PDF files into a single PDF (sorted by filename)
@Extensions: .pdf
@TargetType: Files
@RunAsAdmin: false
#>

# Combine multiple PDF files into one, sorted by filename ascending

# Load PdfSharp from lib folder
$pdfSharpPath = Join-Path $PSScriptRoot "..\..\lib\PdfSharp.dll"
if (-not (Test-Path $pdfSharpPath)) {
    Write-Host "PdfSharp.dll not found at: $pdfSharpPath" -ForegroundColor Red
    Write-Host "Press any key to close..."
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
    exit 1
}
Add-Type -Path $pdfSharpPath

# Filter to only PDF files and sort by name
$pdfFiles = $SelectedFiles | Where-Object { $_ -match '\.pdf$' } | Sort-Object

if ($pdfFiles.Count -lt 2) {
    Write-Host "Please select at least 2 PDF files to combine." -ForegroundColor Yellow
    Write-Host "Press any key to close..."
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
    exit 1
}

Write-Host "Combining $($pdfFiles.Count) PDF files..." -ForegroundColor Cyan
Write-Host ""

try {
    # Create output document
    $outputDoc = New-Object PdfSharp.Pdf.PdfDocument

    foreach ($pdfFile in $pdfFiles) {
        Write-Host "  Adding: $(Split-Path $pdfFile -Leaf)"

        # Open source document
        $inputDoc = [PdfSharp.Pdf.IO.PdfReader]::Open($pdfFile, [PdfSharp.Pdf.IO.PdfDocumentOpenMode]::Import)

        # Copy all pages
        for ($i = 0; $i -lt $inputDoc.PageCount; $i++) {
            $page = $inputDoc.Pages[$i]
            $null = $outputDoc.AddPage($page)
        }
    }

    # Generate output filename in same directory as first file
    $firstFile = Get-Item $pdfFiles[0]
    $outputDir = $firstFile.DirectoryName
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $outputPath = Join-Path $outputDir "Combined_$timestamp.pdf"

    # Save the combined document
    $outputDoc.Save($outputPath)
    $outputDoc.Close()

    Write-Host ""
    Write-Host "Successfully combined $($pdfFiles.Count) files into:" -ForegroundColor Green
    Write-Host "  $outputPath" -ForegroundColor Green
}
catch {
    Write-Error "Failed to combine PDFs: $_"
}

Write-Host ""
Write-Host "Press any key to close..."
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
