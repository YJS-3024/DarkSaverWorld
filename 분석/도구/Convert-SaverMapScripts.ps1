param(
    [Parameter(Mandatory = $true)]
    [string]$InputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutFile
)

$encoding = [Text.Encoding]::GetEncoding(949)
$placements = [Collections.Generic.List[object]]::new()
$aiRules = [Collections.Generic.List[object]]::new()

$inputRoot = (Resolve-Path -LiteralPath $InputDirectory).Path
Get-ChildItem -LiteralPath $inputRoot -Recurse -File | ForEach-Object {
    $sourceName = $_.FullName.Substring($inputRoot.Length + 1).Replace('\', '/')
    $lines = $encoding.GetString([IO.File]::ReadAllBytes($_.FullName)) -split "`r?`n"

    foreach ($line in $lines) {
        $trimmed = $line.Trim()

        if ($trimmed -match '^CHARPOS\s+(\S+)\s+(\S+)\s+(-?\d+)\s+(-?\d+)\s*$') {
            $placements.Add([pscustomobject]@{
                source = $sourceName
                owner = $Matches[1]
                unit = $Matches[2]
                x = [int]$Matches[3]
                y = [int]$Matches[4]
            })
            continue
        }

        if ($trimmed -match '^(\d+)\s+(\S+)\s+(AREA|SOILDER)\(([^)]*)\)\s+(.+)$') {
            $targetValues = @($Matches[4] -split ',' | ForEach-Object { $_.Trim() })
            $aiRules.Add([pscustomobject]@{
                source = $sourceName
                unit = [int]$Matches[1]
                aiType = $Matches[2]
                targetType = $Matches[3]
                targetValues = $targetValues
                behavior = $Matches[5].Trim()
            })
        }
    }
}

$result = [pscustomobject]@{
    placementCount = $placements.Count
    aiRuleCount = $aiRules.Count
    placements = $placements
    aiRules = $aiRules
}

$parent = Split-Path -Parent $OutFile
if ($parent) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}

[IO.File]::WriteAllText(
    $OutFile,
    ($result | ConvertTo-Json -Depth 5),
    [Text.UTF8Encoding]::new($false)
)

$result
