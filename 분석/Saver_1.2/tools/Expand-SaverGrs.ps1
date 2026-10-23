param(
    [string]$InputDirectory = (Join-Path $PSScriptRoot '..\extracted\Saver_Files\Saver\MAP'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\unpacked\grs'),
    [string]$ManifestPath = (Join-Path $PSScriptRoot '..\catalogs\grs_images.csv'),
    [string]$CellsManifestPath = (Join-Path $PSScriptRoot '..\catalogs\grs_cells.csv')
)

function Write-IndexedBmp {
    param([string]$Path, [byte[]]$Palette, [byte[]]$Pixels, [int]$Width, [int]$Height)
    $stride = ($Width + 3) -band -4
    $imageSize = $stride * $Height
    $stream = [IO.File]::Create($Path)
    $writer = [IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([byte]0x42); $writer.Write([byte]0x4D)
        $writer.Write([int](14 + 40 + 1024 + $imageSize))
        $writer.Write([int]0); $writer.Write([int](14 + 40 + 1024))
        # GRS pixel rows are stored bottom-up, matching a positive-height Windows BMP.
        $writer.Write([int]40); $writer.Write([int]$Width); $writer.Write([int]$Height)
        $writer.Write([int16]1); $writer.Write([int16]8); $writer.Write([int]0)
        $writer.Write([int]$imageSize); $writer.Write([int]0); $writer.Write([int]0)
        $writer.Write([int]256); $writer.Write([int]256)
        # GRS stores Win32 PALETTEENTRY as R,G,B,flags; BMP RGBQUAD requires B,G,R,0.
        for ($index = 0; $index -lt 256; $index++) {
            $offset = $index * 4
            $writer.Write($Palette[$offset + 2])
            $writer.Write($Palette[$offset + 1])
            $writer.Write($Palette[$offset])
            $writer.Write([byte]0)
        }
        $padding = [byte[]]::new($stride - $Width)
        for ($y = 0; $y -lt $Height; $y++) {
            $writer.Write($Pixels, $y * $Width, $Width)
            if ($padding.Length) { $writer.Write($padding) }
        }
    } finally { $writer.Dispose(); $stream.Dispose() }
}

$rows = [Collections.Generic.List[object]]::new()
$cellRows = [Collections.Generic.List[object]]::new()
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

foreach ($file in Get-ChildItem -LiteralPath $InputDirectory -Filter '*.grs' -File | Sort-Object Name) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $position = 0
    function Read-I32 { $value = [BitConverter]::ToInt32($bytes, $script:position); $script:position += 4; $value }
    function Read-U8 { $value = $bytes[$script:position]; $script:position++; $value }
    function Read-Image([string]$category, [int]$groupIndex, [int]$imageIndex, [byte[]]$palette, [string]$folder) {
        $width = Read-I32; $height = Read-I32; $dataSize = Read-I32
        $reservedWord = [BitConverter]::ToUInt16($bytes, $script:position); $script:position += 2
        $pixelCount = $width * $height
        if ($width -le 0 -or $height -le 0 -or ($dataSize -ne $pixelCount -and $dataSize -ne $pixelCount + 2)) {
            throw "Invalid image record in $($file.Name): ${width}x${height}, data=$dataSize"
        }
        $pixels = [byte[]]::new($pixelCount)
        [Array]::Copy($bytes, $script:position, $pixels, 0, $pixelCount)
        $script:position += $dataSize
        $name = '{0}_{1:D3}_{2:D3}.bmp' -f $category, $groupIndex, $imageIndex
        $outputPath = Join-Path $folder $name
        Write-IndexedBmp $outputPath $palette $pixels $width $height
        $rows.Add([pscustomobject]@{
            source = $file.Name; category = $category; group = $groupIndex; image = $imageIndex
            width = $width; height = $height; dataSize = $dataSize
            trailingBytes = $dataSize - $pixelCount; reservedWord = $reservedWord
            output = (Join-Path $file.BaseName $name).Replace('\', '/')
        })
    }

    $tileWidth = Read-I32; $tileHeight = Read-I32; $primaryCount = Read-I32
    $folder = Join-Path $OutputDirectory $file.BaseName
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
    for ($group = 0; $group -lt $primaryCount; $group++) {
        $null = Read-I32
        $columns = Read-I32; $rowsCount = Read-I32
        $palette = [byte[]]::new(1024); [Array]::Copy($bytes, $position, $palette, 0, 1024); $position += 1024
        for ($cell = 0; $cell -lt $columns * $rowsCount; $cell++) {
            $field0 = Read-U8; $field1 = Read-U8; $frameCount = Read-U8
            $frames = [byte[]]::new($frameCount)
            if ($frameCount) { [Array]::Copy($bytes, $position, $frames, 0, $frameCount) }
            $position += $frameCount
            $cellRows.Add([pscustomobject]@{
                source = $file.Name; group = $group; cell = $cell; field0 = $field0; field1 = $field1
                frameCount = $frameCount; imageIndexes = ($frames -join ';')
            })
        }
        $imageCount = Read-U8
        for ($image = 0; $image -lt $imageCount; $image++) { Read-Image 'primary' $group $image $palette $folder }
    }
    $secondaryCount = Read-I32
    for ($group = 0; $group -lt $secondaryCount; $group++) {
        $null = Read-I32; $null = Read-I32
        $palette = [byte[]]::new(1024); [Array]::Copy($bytes, $position, $palette, 0, 1024); $position += 1024
        $null = Read-U8; $null = Read-U8; $imageCount = Read-U8
        for ($image = 0; $image -lt $imageCount; $image++) { Read-Image 'secondary' $group $image $palette $folder }
    }
    if ($position -ne $bytes.Length) { throw "Trailing data in $($file.Name): $($bytes.Length - $position) bytes" }
}

$rows | Export-Csv -NoTypeInformation -Encoding utf8 -LiteralPath $ManifestPath
$cellRows | Export-Csv -NoTypeInformation -Encoding utf8 -LiteralPath $CellsManifestPath
Write-Output ("files={0}, images={1}, cells={2}" -f (Get-ChildItem -LiteralPath $InputDirectory -Filter '*.grs' -File).Count, $rows.Count, $cellRows.Count)
