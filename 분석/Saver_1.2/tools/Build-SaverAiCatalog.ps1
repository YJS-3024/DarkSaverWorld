param(
    [string]$InputPath = (Join-Path $PSScriptRoot '..\reports\all_map_placements.json'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\catalogs')
)

$data = Get-Content -Raw -LiteralPath $InputPath | ConvertFrom-Json
$modeCodes = @{ ODNORMAL = 2; ODDEFENCE = 4; ODATTACK = 8 }
$ruleRows = [System.Collections.Generic.List[object]]::new()
$actionRows = [System.Collections.Generic.List[object]]::new()

$ruleId = 0
foreach ($rule in $data.aiRules) {
    $ruleId++
    $isBackup = $rule.source -match '(?i)\.bak(?:$|[\\/])'
    $targetX = $targetY = $radius = $targetUnit = $null
    if ($rule.targetType -eq 'AREA') {
        $targetX, $targetY, $radius = $rule.targetValues
    } elseif ($rule.targetType -eq 'SOILDER') {
        $targetUnit = $rule.targetValues[0]
    }

    $actions = [regex]::Matches($rule.behavior, '([A-Z0-9_]+)\((\d+)\)')
    $weightTotal = 0
    foreach ($action in $actions) { $weightTotal += [int]$action.Groups[2].Value }

    $ruleRows.Add([pscustomobject]@{
        ruleId = $ruleId; source = $rule.source; isBackup = $isBackup
        unit = $rule.unit; aiType = $rule.aiType; aiModeCode = $modeCodes[$rule.aiType]
        targetType = $rule.targetType; targetX = $targetX; targetY = $targetY
        radius = $radius; targetUnit = $targetUnit; actionCount = $actions.Count
        weightTotal = $weightTotal
    })

    $cumulative = 0
    $order = 0
    foreach ($action in $actions) {
        $order++
        $token = $action.Groups[1].Value
        $weight = [int]$action.Groups[2].Value
        $kind = if ($token -eq 'ATTACK') { 'ATTACK' } elseif ($token -eq 'REST') { 'REST' } else { 'MAGIC' }
        $magicId = if ($kind -eq 'MAGIC' -and $token -match '_(\d+)$') { [int]$Matches[1] } else { $null }
        $effective = [Math]::Max(0, [Math]::Min(100, $cumulative + $weight) - [Math]::Min(100, $cumulative))
        $actionRows.Add([pscustomobject]@{
            ruleId = $ruleId; source = $rule.source; isBackup = $isBackup; unit = $rule.unit
            actionOrder = $order; actionType = $kind; token = $token; magicId = $magicId
            weight = $weight; cumulativeStart = $cumulative
            cumulativeEnd = $cumulative + $weight; effectiveProbability = $effective
        })
        $cumulative += $weight
    }
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$ruleRows | Export-Csv -NoTypeInformation -Encoding utf8 -LiteralPath (Join-Path $OutputDirectory 'ai_rules.csv')
$actionRows | Export-Csv -NoTypeInformation -Encoding utf8 -LiteralPath (Join-Path $OutputDirectory 'ai_actions.csv')

Write-Output ("rules={0}, actions={1}" -f $ruleRows.Count, $actionRows.Count)
