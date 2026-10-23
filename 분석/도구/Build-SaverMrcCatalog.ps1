param(
    [Parameter(Mandatory = $true)]
    [string]$MapDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$headers = [Collections.Generic.List[object]]::new()
$sprites = [Collections.Generic.List[object]]::new()

foreach ($file in Get-ChildItem -LiteralPath $MapDirectory -Filter '*.mrc' -File) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    if ($bytes.Length -lt 16) { continue }

    $resourceCount = [BitConverter]::ToInt32($bytes, 0)
    if ($resourceCount -lt 0 -or $resourceCount -gt 1024) { continue }

    $offset = 4
    $resourceIds = [Collections.Generic.List[int]]::new()
    for ($index = 0; $index -lt $resourceCount; $index++) {
        $resourceIds.Add([BitConverter]::ToInt32($bytes, $offset))
        $offset += 4
    }

    $width = [BitConverter]::ToInt32($bytes, $offset)
    $height = [BitConverter]::ToInt32($bytes, $offset + 4)
    $spriteCount = [BitConverter]::ToInt32($bytes, $offset + 8)
    $offset += 12

    $cellCount = [int64]$width * $height
    $layer1Offset = $offset
    $layer2Offset = $layer1Offset + $cellCount
    $tileOffset = $layer2Offset + $cellCount
    $spriteOffset = $tileOffset + 5 * $cellCount
    $expectedSize = $spriteOffset + 16 * [int64]$spriteCount
    $valid = $width -gt 0 -and $height -gt 0 -and $spriteCount -ge 0 -and $expectedSize -eq $bytes.Length

    $headers.Add([pscustomobject]@{
        map = $file.BaseName
        resourceCount = $resourceCount
        resourceIds = $resourceIds -join ','
        width = $width
        height = $height
        spriteCount = $spriteCount
        layer1Offset = $layer1Offset
        layer2Offset = $layer2Offset
        tileOffset = $tileOffset
        spriteOffset = $spriteOffset
        fileSize = $bytes.Length
        structureValid = $valid
    })

    if (!$valid) { continue }
    for ($index = 0; $index -lt $spriteCount; $index++) {
        $at = [int]($spriteOffset + 16 * $index)
        $sprites.Add([pscustomobject]@{
            map = $file.BaseName
            index = $index
            field0 = [BitConverter]::ToUInt32($bytes, $at)
            x = [BitConverter]::ToInt32($bytes, $at + 4)
            y = [BitConverter]::ToInt32($bytes, $at + 8)
            resource = $bytes[$at + 12]
            frame = $bytes[$at + 13]
            field14 = $bytes[$at + 14]
            field15 = $bytes[$at + 15]
        })
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$utf8Bom = [Text.UTF8Encoding]::new($true)
[IO.File]::WriteAllLines(
    (Join-Path $OutputDirectory 'mrc_headers.csv'),
    @($headers | Sort-Object map | ConvertTo-Csv -NoTypeInformation),
    $utf8Bom
)
[IO.File]::WriteAllLines(
    (Join-Path $OutputDirectory 'mrc_sprites.csv'),
    @($sprites | Sort-Object map, index | ConvertTo-Csv -NoTypeInformation),
    $utf8Bom
)

[pscustomobject]@{
    maps = $headers.Count
    validMaps = @($headers | Where-Object structureValid).Count
    sprites = $sprites.Count
}
