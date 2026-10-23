param(
    [Parameter(Mandatory = $true)]
    [string]$Root,

    [Parameter(Mandatory = $true)]
    [string]$OutFile
)

$rootPath = (Resolve-Path -LiteralPath $Root).Path
$rows = foreach ($file in Get-ChildItem -LiteralPath $rootPath -Recurse -File) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $kind = 'binary'
    $width = $null
    $height = $null
    $detail = ''

    if ($bytes.Length -ge 30 -and $bytes[0] -eq 0x42 -and $bytes[1] -eq 0x4D) {
        $kind = 'bmp'
        $width = [BitConverter]::ToInt32($bytes, 18)
        $height = [BitConverter]::ToInt32($bytes, 22)
        $detail = 'bpp=' + [BitConverter]::ToUInt16($bytes, 28)
    } elseif ($bytes.Length -ge 12 -and [Text.Encoding]::ASCII.GetString($bytes, 0, 4) -eq 'RIFF' -and [Text.Encoding]::ASCII.GetString($bytes, 8, 4) -eq 'WAVE') {
        $kind = 'wav'
        $channels = [BitConverter]::ToUInt16($bytes, 22)
        $sampleRate = [BitConverter]::ToUInt32($bytes, 24)
        $bits = [BitConverter]::ToUInt16($bytes, 34)
        $detail = "channels=$channels;sampleRate=$sampleRate;bits=$bits"
    } elseif ($bytes.Length -ge 14 -and [Text.Encoding]::ASCII.GetString($bytes, 0, 4) -eq 'MThd') {
        $kind = 'midi'
        $tracks = ($bytes[10] -shl 8) -bor $bytes[11]
        $division = ($bytes[12] -shl 8) -bor $bytes[13]
        $detail = "tracks=$tracks;division=$division"
    } elseif ($file.Extension.ToLowerInvariant() -eq '.rbm' -and $bytes.Length -ge 16) {
        $kind = 'effect_mask'
        $width = [BitConverter]::ToInt32($bytes, 4)
        $height = [BitConverter]::ToInt32($bytes, 8)
        $stride = [BitConverter]::ToInt32($bytes, 12)
        $detail = "stride=$stride;structureValid=$($bytes.Length -eq 16 + $height * $stride)"
    } elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0x4D -and $bytes[1] -eq 0x5A) {
        $kind = 'executable'
    } elseif ($file.Extension.ToLowerInvariant() -in @('.ai', '.evt', '.dee', '.deo', '.srf', '.txt', '.atr', '.set', '.pos', '.all', '.bak')) {
        $kind = 'text_or_table'
    }

    [pscustomobject]@{
        path = $file.FullName.Substring($rootPath.Length + 1).Replace('\', '/')
        extension = $file.Extension.ToLowerInvariant()
        kind = $kind
        size = $file.Length
        width = $width
        height = $height
        detail = $detail
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

$rows | Group-Object kind | Sort-Object Count -Descending | Select-Object Name, Count
