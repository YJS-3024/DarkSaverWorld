param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [string]$OutFile
)

$resolved = (Resolve-Path -LiteralPath $Path).Path
$bytes = [IO.File]::ReadAllBytes($resolved)

if ($bytes.Length -lt 5) {
    throw 'ARC file is too small.'
}

$versionLength = $bytes[0]
if (1 + $versionLength -gt $bytes.Length) {
    throw 'Invalid ARC version field.'
}

$version = [Text.Encoding]::ASCII.GetString($bytes, 1, $versionLength)
$recordOffset = 1 + $versionLength
$records = [Collections.Generic.List[object]]::new()

while ($recordOffset + 36 -le $bytes.Length) {
    $nameLength = $bytes[$recordOffset]
    if ($nameLength -gt 16) {
        throw "Invalid ARC name length at offset $recordOffset."
    }

    $name = [Text.Encoding]::ASCII.GetString($bytes, $recordOffset + 1, $nameLength)
    if ($name -eq 'HEADEND') {
        $dataOffset = $recordOffset + 36
        break
    }

    $checksum = [BitConverter]::ToUInt16($bytes, $recordOffset + 18)
    $unpackedSize = [BitConverter]::ToUInt32($bytes, $recordOffset + 20)
    $packedSize = [BitConverter]::ToUInt32($bytes, $recordOffset + 24)
    $start = [BitConverter]::ToUInt32($bytes, $recordOffset + 28)
    $end = [BitConverter]::ToUInt32($bytes, $recordOffset + 32)

    $rangeValid = $start -le $end -and $end -lt $bytes.Length
    $actualChecksum = $null
    if ($rangeValid) {
        $sum = 0
        for ($index = $start; $index -le $end; $index++) {
            $sum = ($sum + $bytes[$index]) -band 0xffff
        }
        $actualChecksum = $sum
    }

    $records.Add([pscustomobject]@{
        name = $name
        unpackedSize = $unpackedSize
        packedSize = $packedSize
        startOffset = $start
        endOffset = $end
        storedChecksum = ('{0:X4}' -f $checksum)
        actualChecksum = if ($null -eq $actualChecksum) { $null } else { '{0:X4}' -f $actualChecksum }
        checksumValid = $rangeValid -and $actualChecksum -eq $checksum
    })

    $recordOffset += 36
}

if ($null -eq $dataOffset) {
    throw 'ARC HEADEND record was not found.'
}

$result = [pscustomobject]@{
    source = $resolved
    formatVersion = $version
    recordSize = 36
    dataOffset = $dataOffset
    entryCount = $records.Count
    entries = $records
}

$json = $result | ConvertTo-Json -Depth 4
if ($OutFile) {
    $parent = Split-Path -Parent $OutFile
    if ($parent) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    [IO.File]::WriteAllText($OutFile, $json, [Text.UTF8Encoding]::new($false))
}

$result
