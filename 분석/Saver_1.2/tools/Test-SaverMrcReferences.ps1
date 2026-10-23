param([string]$AnalysisRoot = (Join-Path $PSScriptRoot '..'))

$mapDirectory = Join-Path $AnalysisRoot 'extracted\Saver_Files\Saver\MAP'
$cells = @{}; $images = @{}; $sources = @{}
Import-Csv (Join-Path $AnalysisRoot 'catalogs\grs_cells.csv') | ForEach-Object {
    $source = $_.source.ToLower(); $sources[$source] = $true
    $cells[('{0}|{1}|{2}' -f $source, [int]$_.group, [int]$_.cell)] = $_
}
Import-Csv (Join-Path $AnalysisRoot 'catalogs\grs_images.csv') | ForEach-Object {
    $source = $_.source.ToLower(); $sources[$source] = $true
    $images[('{0}|{1}|{2}|{3}' -f $source, $_.category, [int]$_.group, [int]$_.image)] = $true
}

$mapCount = 0; $tileCount = 0; $spriteCount = 0
$missingBack = 0; $missingResource = 0; $missingCell = 0; $missingImage = 0; $missingSprite = 0
foreach ($file in Get-ChildItem -LiteralPath $mapDirectory -Filter '*.mrc' -File) {
    $mapCount++; $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $resourceCount = [BitConverter]::ToInt32($bytes, 0)
    $width = [BitConverter]::ToInt32($bytes, 4 + 4 * $resourceCount)
    $height = [BitConverter]::ToInt32($bytes, 8 + 4 * $resourceCount)
    $mapSprites = [BitConverter]::ToInt32($bytes, 12 + 4 * $resourceCount)
    $tileOffset = 16 + 4 * $resourceCount + 2 * $width * $height
    $backResourceOffset = 16 + 4 * $resourceCount
    $backTileOffset = $backResourceOffset + $width * $height
    $spriteOffset = $tileOffset + 5 * $width * $height
    for ($index = 0; $index -lt $width * $height; $index++) {
        $tileCount++; $record = $tileOffset + 5 * $index
        $backKey = 'mdsr{0}.grs|primary|{1}|0' -f $bytes[$backResourceOffset + $index], $bytes[$backTileOffset + $index]
        if (!$images.ContainsKey($backKey)) { $missingBack++ }
        $source = ('mdsr{0}.grs' -f $bytes[$record]).ToLower()
        $group = $bytes[$record + 1]; $cell = $bytes[$record + 2]
        if ($group -eq 255) { continue }
        if (!$sources.ContainsKey($source)) { $missingResource++; continue }
        $cellKey = '{0}|{1}|{2}' -f $source, $group, $cell
        if (!$cells.ContainsKey($cellKey)) { $missingCell++; continue }
        $frames = @($cells[$cellKey].imageIndexes -split ';' | Where-Object { $_ -ne '' })
        foreach ($frame in $frames) {
            if (!$images.ContainsKey(('{0}|primary|{1}|{2}' -f $source, $group, [int]$frame))) { $missingImage++ }
        }
    }
    for ($index = 0; $index -lt $mapSprites; $index++) {
        $spriteCount++; $record = $spriteOffset + 16 * $index
        $key = 'mdsr{0}.grs|secondary|{1}|{2}' -f $bytes[$record], $bytes[$record + 12], $bytes[$record + 13]
        if (!$images.ContainsKey($key)) { $missingSprite++ }
    }
}

[pscustomobject]@{
    maps = $mapCount; tiles = $tileCount; sprites = $spriteCount
    missingBack = $missingBack; missingResource = $missingResource; missingCell = $missingCell
    missingImage = $missingImage; missingSprite = $missingSprite
}
