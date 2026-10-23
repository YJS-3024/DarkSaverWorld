param(
    [string[]]$MapName = @('01'),
    [string]$AnalysisRoot = (Join-Path $PSScriptRoot '..'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\unpacked\map_previews'),
    [string]$SpriteOutputDirectory,
    [switch]$SkipPreview
)

Add-Type -AssemblyName System.Drawing
$mapDirectory = Join-Path $AnalysisRoot 'extracted\Saver_Files\Saver\MAP'
$cells = @{}
Import-Csv (Join-Path $AnalysisRoot 'catalogs\grs_cells.csv') | ForEach-Object {
    $cells[('{0}|{1}|{2}' -f $_.source.ToLower(), [int]$_.group, [int]$_.cell)] = $_
}
$images = @{}
Import-Csv (Join-Path $AnalysisRoot 'catalogs\grs_images.csv') | ForEach-Object {
    $images[('{0}|{1}|{2}|{3}' -f $_.source.ToLower(), $_.category, [int]$_.group, [int]$_.image)] = $_
}
$targets = if ($MapName -contains '*') {
    Get-ChildItem -LiteralPath $mapDirectory -Filter '*.mrc' -File
} else {
    foreach ($name in $MapName) { Get-Item -LiteralPath (Join-Path $mapDirectory "$name.mrc") }
}
if (-not $SkipPreview) { New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null }
if ($SpriteOutputDirectory) { New-Item -ItemType Directory -Force -Path $SpriteOutputDirectory | Out-Null }

foreach ($mapFile in $targets) {
    $bytes = [IO.File]::ReadAllBytes($mapFile.FullName)
    $resourceCount = [BitConverter]::ToInt32($bytes, 0)
    $width = [BitConverter]::ToInt32($bytes, 4 + 4 * $resourceCount)
    $height = [BitConverter]::ToInt32($bytes, 8 + 4 * $resourceCount)
    $spriteCount = [BitConverter]::ToInt32($bytes, 12 + 4 * $resourceCount)
    $backResourceOffset = 16 + 4 * $resourceCount
    $backTileOffset = $backResourceOffset + $width * $height
    $tileOffset = $backTileOffset + $width * $height
    $spriteOffset = $tileOffset + 5 * $width * $height
    $canvas = [Drawing.Bitmap]::new($width * 32, $height * 32, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($canvas)
    $graphics.Clear([Drawing.Color]::Transparent)
    $cache = @{}; $missing = 0
    function Get-Image([object]$entry) {
        $key = $entry.output
        if (!$cache.ContainsKey($key)) {
            $path = Join-Path (Join-Path $AnalysisRoot 'unpacked\grs') $entry.output
            $bitmap = [Drawing.Bitmap]::FromFile($path)
            # Original MakeSurfaceFromCDIBitmap calls SetColorKey(0): RGB black is transparent.
            $bitmap.MakeTransparent([Drawing.Color]::Black)
            if ($SpriteOutputDirectory) {
                $assetName = ('{0}|{1}|{2}|{3}' -f $entry.source, $entry.category, [int]$entry.group, [int]$entry.image).Replace('.grs|', '_').Replace('|', '_')
                $assetPath = Join-Path $SpriteOutputDirectory ($assetName + '.png')
                if (-not (Test-Path -LiteralPath $assetPath)) {
                    $bitmap.Save($assetPath, [Drawing.Imaging.ImageFormat]::Png)
                }
            }
            $cache[$key] = $bitmap
        }
        $cache[$key]
    }
    try {
        for ($index = 0; $index -lt $width * $height; $index++) {
            $x = ($index % $width) * 32; $y = [Math]::Floor($index / $width) * 32
            $backSource = 'mdsr{0}.grs' -f $bytes[$backResourceOffset + $index]
            $backGroup = $bytes[$backTileOffset + $index]
            $backKey = '{0}|primary|{1}|0' -f $backSource, $backGroup
            if ($images.ContainsKey($backKey)) {
                $backBitmap = Get-Image $images[$backKey]
                $graphics.DrawImageUnscaled($backBitmap, $x, $y)
            } else { $missing++ }
            $record = $tileOffset + 5 * $index
            $resource = $bytes[$record]; $group = $bytes[$record + 1]; $cell = $bytes[$record + 2]
            if ($group -eq 255) { continue }
            $source = "mdsr$resource.grs"
            $cellKey = '{0}|{1}|{2}' -f $source, $group, $cell
            if (!$cells.ContainsKey($cellKey)) { $missing++; continue }
            $frame = @($cells[$cellKey].imageIndexes -split ';' | Where-Object { $_ -ne '' })[0]
            $imageKey = '{0}|primary|{1}|{2}' -f $source, $group, [int]$frame
            if (!$images.ContainsKey($imageKey)) { $missing++; continue }
            $bitmap = Get-Image $images[$imageKey]
            $graphics.DrawImageUnscaled($bitmap, $x, $y)
        }
        for ($index = 0; $index -lt $spriteCount; $index++) {
            $record = $spriteOffset + 16 * $index
            $resource = $bytes[$record]
            $x = [BitConverter]::ToInt32($bytes, $record + 4) * 32
            $y = [BitConverter]::ToInt32($bytes, $record + 8) * 32
            $group = $bytes[$record + 12]; $frame = $bytes[$record + 13]
            $imageKey = 'mdsr{0}.grs|secondary|{1}|{2}' -f $resource, $group, $frame
            if (!$images.ContainsKey($imageKey)) { $missing++; continue }
            $bitmap = Get-Image $images[$imageKey]
            $graphics.DrawImageUnscaled($bitmap, $x, $y)
        }
        $output = Join-Path $OutputDirectory ($mapFile.BaseName + '.png')
        if (-not $SkipPreview) { $canvas.Save($output, [Drawing.Imaging.ImageFormat]::Png) }
        $previewOutput = if ($SkipPreview) { '(skipped)' } else { $output }
        Write-Output ("map={0}, size={1}x{2}, sprites={3}, missing={4}, output={5}" -f $mapFile.BaseName, $width, $height, $spriteCount, $missing, $previewOutput)
    } finally {
        $graphics.Dispose(); $canvas.Dispose()
        foreach ($bitmap in $cache.Values) { $bitmap.Dispose() }
    }
}
