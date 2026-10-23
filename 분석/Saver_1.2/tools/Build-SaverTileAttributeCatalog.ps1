param(
    [string]$AnalysisRoot = (Join-Path $PSScriptRoot '..'),
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\catalogs\mrc_tile_attribute_usage.csv')
)

$mapDirectory = Join-Path $AnalysisRoot 'extracted\Saver_Files\Saver\MAP'
$tileAttrPath = Join-Path $AnalysisRoot 'converted\TileAttr.csv'
$definitions = @{}
foreach ($row in Import-Csv $tileAttrPath) {
    $id = if ($row.num -match '^\d$') { [int]$row.num } else { 10 + ([int][char]$row.num - [int][char]'A') }
    $definitions[$id] = $row.num
}

$counts = @{}
$maps = @{}
foreach ($mapFile in Get-ChildItem -LiteralPath $mapDirectory -Filter '*.mrc' -File) {
    $bytes = [IO.File]::ReadAllBytes($mapFile.FullName)
    $resourceCount = [BitConverter]::ToInt32($bytes, 0)
    $width = [BitConverter]::ToInt32($bytes, 4 + 4 * $resourceCount)
    $height = [BitConverter]::ToInt32($bytes, 8 + 4 * $resourceCount)
    $tileOffset = 16 + 4 * $resourceCount + 2 * $width * $height
    for ($index = 0; $index -lt $width * $height; $index++) {
        $raw = [int]$bytes[$tileOffset + 5 * $index + 3]
        $counts[$raw] = 1 + $counts[$raw]
        if (!$maps.ContainsKey($raw)) { $maps[$raw] = [Collections.Generic.HashSet[string]]::new() }
        $null = $maps[$raw].Add($mapFile.BaseName)
    }
}

$rows = foreach ($entry in $counts.GetEnumerator() | Sort-Object Name) {
    $raw = [int]$entry.Name
    $masked = $raw -band 0x7F
    $resolved = if ($masked -le 0x1B) { $masked } else { 0 }
    $effect = if ($masked -eq 6) { 'water_submerge_overlay' } elseif ($masked -eq 20) { 'water_submerge_overlay_code_only' } else { '' }
    [pscustomobject]@{
        rawDecimal = $raw
        rawHex = '0x{0:X2}' -f $raw
        maskedDecimal = $masked
        maskedHex = '0x{0:X2}' -f $masked
        runtimeResolvedDecimal = $resolved
        runtimeResolvedHex = '0x{0:X2}' -f $resolved
        outOfRangeFallback = ($masked -gt 0x1B)
        highBit = (($raw -band 0x80) -ne 0)
        highBitSemantic = if (($raw -band 0x80) -ne 0) { 'sprite_footprint_occlusion' } else { '' }
        tileAttrRow = if ($definitions.ContainsKey($resolved)) { $definitions[$resolved] } else { '' }
        tileCount = $entry.Value
        mapCount = $maps[$raw].Count
        confirmedVisualEffect = $effect
    }
}

$rows | Export-Csv -LiteralPath $OutputPath -NoTypeInformation -Encoding utf8BOM
Write-Output ("attributes={0}, tiles={1}, output={2}" -f $rows.Count, (($rows | Measure-Object tileCount -Sum).Sum), $OutputPath)
