param(
    [Parameter(Mandatory = $true)][string]$Root,
    [Parameter(Mandatory = $true)][string]$OutFile
)

$rootPath = (Resolve-Path -LiteralPath $Root).Path
$extensions = @('.grs', '.lce', '.lnm', '.ltn', '.lsc', '.mxf', '.mrtc')
$rows = foreach ($file in Get-ChildItem -LiteralPath $rootPath -Recurse -File) {
    $ext = $file.Extension.ToLowerInvariant()
    if ($ext -notin $extensions) { continue }
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $ints = @()
    for ($offset = 0; $offset + 4 -le [Math]::Min(32, $bytes.Length); $offset += 4) {
        $ints += [BitConverter]::ToInt32($bytes, $offset)
    }
    $kind = switch ($ext) {
        '.grs'  { 'map_graphics_resource' }
        '.lce'  { 'scene_effect_library' }
        '.lnm'  { 'scene_animation_library' }
        '.ltn'  { 'scene_tile_library' }
        '.lsc'  { 'scene_layout' }
        '.mxf'  { 'raw_288x288_mask_candidate' }
        '.mrtc' { 'map_auxiliary_table' }
    }
    [pscustomobject]@{
        path = $file.FullName.Substring($rootPath.Length + 1).Replace('\', '/')
        extension = $ext
        kind = $kind
        size = $file.Length
        int0 = if ($ints.Count -gt 0) { $ints[0] } else { $null }
        int1 = if ($ints.Count -gt 1) { $ints[1] } else { $null }
        int2 = if ($ints.Count -gt 2) { $ints[2] } else { $null }
        int3 = if ($ints.Count -gt 3) { $ints[3] } else { $null }
        int4 = if ($ints.Count -gt 4) { $ints[4] } else { $null }
        int5 = if ($ints.Count -gt 5) { $ints[5] } else { $null }
        int6 = if ($ints.Count -gt 6) { $ints[6] } else { $null }
        int7 = if ($ints.Count -gt 7) { $ints[7] } else { $null }
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash
    }
}

$parent = Split-Path -Parent $OutFile
if ($parent) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
[IO.File]::WriteAllLines(
    $OutFile,
    @($rows | Sort-Object path | ConvertTo-Csv -NoTypeInformation),
    [Text.UTF8Encoding]::new($true)
)
$rows | Group-Object extension | Sort-Object Name | Select-Object Name, Count
