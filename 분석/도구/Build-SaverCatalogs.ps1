param(
    [Parameter(Mandatory = $true)]
    [string]$AnalysisRoot
)

$root = (Resolve-Path -LiteralPath $AnalysisRoot).Path
$converted = Join-Path $root 'converted'
$decodedMap = Join-Path $root 'decoded\all_arc\MAP'
$reports = Join-Path $root 'reports'
$catalogs = Join-Path $root 'catalogs'
New-Item -ItemType Directory -Path $catalogs -Force | Out-Null

function Write-Utf8Csv($Rows, [string]$Path) {
    $csv = @($Rows | ConvertTo-Csv -NoTypeInformation)
    [IO.File]::WriteAllLines($Path, $csv, [Text.UTF8Encoding]::new($true))
}

$units = Import-Csv -LiteralPath (Join-Path $converted 'ability.csv')
$classified = foreach ($unit in $units) {
    $id = [int]$unit.직업
    $category = if ($id -lt 200) {
        'player_job'
    } elseif ($id -lt 300) {
        'mercenary'
    } elseif ($id -lt 800) {
        'monster'
    } else {
        'npc_or_special'
    }

    $unit | Add-Member -NotePropertyName category -NotePropertyValue $category -PassThru
}

Write-Utf8Csv $classified (Join-Path $catalogs 'units_all.csv')
Write-Utf8Csv ($classified | Where-Object category -eq 'player_job') (Join-Path $catalogs 'jobs.csv')
Write-Utf8Csv ($classified | Where-Object category -eq 'mercenary') (Join-Path $catalogs 'mercenaries.csv')
Write-Utf8Csv ($classified | Where-Object category -eq 'monster') (Join-Path $catalogs 'monsters.csv')
Write-Utf8Csv ($classified | Where-Object category -eq 'npc_or_special') (Join-Path $catalogs 'npcs_and_special.csv')
$unitById = @{}
foreach ($unitRow in $classified) { $unitById[[string]$unitRow.직업] = $unitRow }

$itemSlots = @{
    '0'='special'; '1'='ranged_or_staff'; '2'='weapon'; '3'='armor'; '4'='boots'
    '5'='headgear'; '6'='accessory_or_manual'; '7'='consumable'; '8'='shield'
    '9'='quest_or_misc'; '10'='travel_or_special_consumable'
}
$items = Import-Csv -LiteralPath (Join-Path $converted 'itemtbl.csv')
$itemCatalog = foreach ($item in $items) {
    $item | Add-Member -NotePropertyName itemCategory -NotePropertyValue $itemSlots[[string]$item.장착위] -PassThru
}
Write-Utf8Csv $itemCatalog (Join-Path $catalogs 'items.csv')

$levels = Import-Csv -LiteralPath (Join-Path $converted 'levelabl.csv')
$levelCatalog = foreach ($level in $levels) {
    $unitInfo = $unitById[[string]$level.직업]
    [pscustomobject]@{
        jobId = $level.직업; jobName = if ($unitInfo) { $unitInfo.이름 } else { '' }
        level = $level.레벨; requiredXp = $level.경험치
        nextJobId = $level.담직업; rewardGold = $level.섭돈수; rewardXp = $level.섭경수
        learnedMagic = $level.섭마법; usableSkills = $level.사용기술
    }
}
Write-Utf8Csv $levelCatalog (Join-Path $catalogs 'experience_progression.csv')

$placementData = Get-Content -LiteralPath (Join-Path $reports 'all_map_placements.json') -Raw | ConvertFrom-Json
$joinedPlacements = foreach ($entry in $placementData.placements) {
    $ownerInfo = $unitById[[string]$entry.owner]
    $unitInfo = $unitById[[string]$entry.unit]
    [pscustomobject]@{
        mapKey = (($entry.source -split '/')[0] -replace 'set$', '')
        source = $entry.source
        ownerId = $entry.owner
        ownerName = if ($ownerInfo) { $ownerInfo.이름 } else { '' }
        unitId = $entry.unit
        unitName = if ($unitInfo) { $unitInfo.이름 } else { '' }
        category = if ($unitInfo) { $unitInfo.category } else { 'unresolved' }
        x = $entry.x
        y = $entry.y
    }
}
Write-Utf8Csv $joinedPlacements (Join-Path $catalogs 'map_unit_placements.csv')
$placementCounts = @{}
$aiCounts = @{}
foreach ($entry in $placementData.placements) {
    $key = ($entry.source -split '/')[0]
    $placementCounts[$key] = 1 + [int]$placementCounts[$key]
}
foreach ($entry in $placementData.aiRules) {
    $key = ($entry.source -split '/')[0]
    $aiCounts[$key] = 1 + [int]$aiCounts[$key]
}

$encoding = [Text.Encoding]::GetEncoding(949)
$maps = foreach ($file in Get-ChildItem -LiteralPath $decodedMap -Recurse -Filter '*.srf' -File) {
    $directoryKey = $file.Directory.Name
    $lines = $encoding.GetString([IO.File]::ReadAllBytes($file.FullName)) -split "`r?`n" |
        ForEach-Object { $_.Trim() } | Where-Object { $_ }
    [pscustomobject]@{
        mapKey = $directoryKey -replace 'set$', ''
        title = if ($lines.Count) { $lines[0] } else { '' }
        source = $file.FullName.Substring($decodedMap.Length + 1).Replace('\', '/')
        placementCount = [int]$placementCounts[$directoryKey]
        aiRuleCount = [int]$aiCounts[$directoryKey]
        summary = if ($lines.Count -gt 2) { ($lines[2..($lines.Count - 1)] -join ' ') } else { '' }
    }
}
Write-Utf8Csv ($maps | Sort-Object {[int]($_.mapKey -replace '\D','')}) (Join-Path $catalogs 'maps.csv')

[pscustomobject]@{
    units = $classified.Count
    jobs = @($classified | Where-Object category -eq 'player_job').Count
    mercenaries = @($classified | Where-Object category -eq 'mercenary').Count
    monsters = @($classified | Where-Object category -eq 'monster').Count
    npcAndSpecial = @($classified | Where-Object category -eq 'npc_or_special').Count
    maps = @($maps).Count
    placements = $placementData.placementCount
    aiRules = $placementData.aiRuleCount
}
