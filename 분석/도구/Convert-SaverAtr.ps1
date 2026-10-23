param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$OutFile
)

$encoding = [Text.Encoding]::GetEncoding(949)
$lines = $encoding.GetString([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)) -split "`r?`n" |
    Where-Object { $_.Trim() }

if ($lines.Count -lt 2) {
    throw 'ATR table has no data rows.'
}

$name = [IO.Path]::GetFileName($Path).ToLowerInvariant()
if ($name -eq 'levelptl.atr') {
    $headers = @('직업', '체력', '마법력', '공격력', '방어력', '마공력', '마방력', '다운시한계', '수평시한계', '체인지')
} else {
    $headers = @($lines[0].TrimStart(';').Trim() -split '\s+')
}

$rows = [Collections.Generic.List[object]]::new()
$skippedLines = [Collections.Generic.List[int]]::new()
for ($lineIndex = 1; $lineIndex -lt $lines.Count; $lineIndex++) {
    if ($lines[$lineIndex].TrimStart().StartsWith(';')) {
        $skippedLines.Add($lineIndex + 1)
        continue
    }
    $values = @($lines[$lineIndex].Trim() -split '\s+')
    if ($values.Count -ne $headers.Count) {
        if ($values.Count -eq 1) {
            $skippedLines.Add($lineIndex + 1)
            continue
        }
        if ($values.Count -lt $headers.Count) {
            $values = @($values) + @('') * ($headers.Count - $values.Count)
        } else {
            $values = @($values[0..($headers.Count - 2)]) + (($values[($headers.Count - 1)..($values.Count - 1)]) -join ' ')
        }
    }

    $row = [ordered]@{}
    for ($column = 0; $column -lt $headers.Count; $column++) {
        $row[$headers[$column]] = $values[$column]
    }
    $rows.Add([pscustomobject]$row)
}

$parent = Split-Path -Parent $OutFile
if ($parent) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}

$csv = $rows | ConvertTo-Csv -NoTypeInformation
[IO.File]::WriteAllLines($OutFile, $csv, [Text.UTF8Encoding]::new($true))

[pscustomobject]@{
    source = (Resolve-Path -LiteralPath $Path).Path
    output = [IO.Path]::GetFullPath($OutFile)
    columns = $headers.Count
    rows = $rows.Count
    skippedLines = @($skippedLines)
}
