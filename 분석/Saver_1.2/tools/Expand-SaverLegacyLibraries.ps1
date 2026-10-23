param(
    [string]$GameRoot = (Join-Path $PSScriptRoot '..\extracted\Saver_Files\Saver'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\unpacked\legacy_libraries'),
    [string]$ManifestPath = (Join-Path $PSScriptRoot '..\catalogs\legacy_library_payloads.csv')
)

$rows = [Collections.Generic.List[object]]::new()
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$files = Get-ChildItem (Join-Path $GameRoot 'Glib'), (Join-Path $GameRoot 'Wlib') -File |
    Where-Object { $_.Extension -in '.lce', '.lnm', '.ltn' } | Sort-Object FullName

foreach ($file in $files) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $bmpOffset = -1; $bmpSize = 0
    for ($i = 0; $i -le $bytes.Length - 14; $i++) {
        if ($bytes[$i] -ne 0x42 -or $bytes[$i + 1] -ne 0x4D) { continue }
        $declared = [BitConverter]::ToInt32($bytes, $i + 2)
        $pixelOffset = [BitConverter]::ToInt32($bytes, $i + 10)
        if ($declared -ge 54 -and $pixelOffset -ge 54 -and $i + $declared -eq $bytes.Length) {
            $bmpOffset = $i; $bmpSize = $declared; break
        }
    }
    if ($bmpOffset -lt 0) { throw "No terminal BMP payload found in $($file.FullName)" }

    $prefix = [byte[]]::new($bmpOffset)
    [Array]::Copy($bytes, 0, $prefix, 0, $bmpOffset)
    $bmp = [byte[]]::new($bmpSize)
    [Array]::Copy($bytes, $bmpOffset, $bmp, 0, $bmpSize)
    $stem = '{0}_{1}' -f $file.Directory.Name, $file.Name.Replace('.', '_')
    $prefixName = "$stem.header.bin"; $bmpName = "$stem.bmp"
    [IO.File]::WriteAllBytes((Join-Path $OutputDirectory $prefixName), $prefix)
    [IO.File]::WriteAllBytes((Join-Path $OutputDirectory $bmpName), $bmp)

    $dibSize = [BitConverter]::ToInt32($bmp, 14)
    $width = [BitConverter]::ToInt32($bmp, 18)
    $height = [BitConverter]::ToInt32($bmp, 22)
    $bitDepth = [BitConverter]::ToInt16($bmp, 28)
    $rows.Add([pscustomobject]@{
        source = "$($file.Directory.Name)/$($file.Name)"; headerSize = $bmpOffset
        bmpSize = $bmpSize; dibSize = $dibSize; width = $width; height = $height
        bitDepth = $bitDepth; headerOutput = $prefixName; imageOutput = $bmpName
    })
}

$rows | Export-Csv -NoTypeInformation -Encoding utf8 -LiteralPath $ManifestPath
Write-Output ("libraries={0}, images={1}, headers={2}" -f $rows.Count, $rows.Count, $rows.Count)
