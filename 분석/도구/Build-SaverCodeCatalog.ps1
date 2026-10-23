param(
    [Parameter(Mandatory = $true)]
    [string]$BinaryDirectory,

    [Parameter(Mandatory = $true)]
    [string]$LlvmReadObj,

    [Parameter(Mandatory = $true)]
    [string]$OutFile
)

$rows = [Collections.Generic.List[object]]::new()

foreach ($file in Get-ChildItem -LiteralPath $BinaryDirectory -File -Include '*.exe', '*.dll') {
    $lines = & $LlvmReadObj --coff-exports --coff-imports $file.FullName
    $block = $null
    $library = $null
    $ordinal = $null
    $rva = $null
    $exportName = $null

    foreach ($line in $lines) {
        $trimmed = $line.Trim()
        if ($trimmed -eq 'Import {') {
            $block = 'import'
            $library = $null
            continue
        }
        if ($trimmed -eq 'Export {') {
            $block = 'export'
            $ordinal = $null
            $rva = $null
            $exportName = $null
            continue
        }
        if ($trimmed -eq '}') {
            if ($block -eq 'export' -and $exportName) {
                $rows.Add([pscustomobject]@{
                    binary = $file.Name
                    kind = 'export'
                    library = ''
                    symbol = $exportName
                    ordinal = $ordinal
                    rva = $rva
                })
            }
            $block = $null
            continue
        }

        if ($block -eq 'import') {
            if ($trimmed -match '^Name:\s+(.+)$') {
                $library = $Matches[1]
            } elseif ($trimmed -match '^Symbol:\s*(.*?)\s+\((\d+)\)$') {
                $rows.Add([pscustomobject]@{
                    binary = $file.Name
                    kind = 'import'
                    library = $library
                    symbol = $Matches[1]
                    ordinal = $Matches[2]
                    rva = ''
                })
            }
        } elseif ($block -eq 'export') {
            if ($trimmed -match '^Ordinal:\s+(\d+)$') { $ordinal = $Matches[1] }
            elseif ($trimmed -match '^Name:\s+(.+)$') { $exportName = $Matches[1] }
            elseif ($trimmed -match '^RVA:\s+(.+)$') { $rva = $Matches[1] }
        }
    }
}

$parent = Split-Path -Parent $OutFile
if ($parent) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
$csv = $rows | ConvertTo-Csv -NoTypeInformation
[IO.File]::WriteAllLines($OutFile, $csv, [Text.UTF8Encoding]::new($true))

[pscustomobject]@{
    binaries = @($rows.binary | Sort-Object -Unique).Count
    imports = @($rows | Where-Object kind -eq 'import').Count
    exports = @($rows | Where-Object kind -eq 'export').Count
}
