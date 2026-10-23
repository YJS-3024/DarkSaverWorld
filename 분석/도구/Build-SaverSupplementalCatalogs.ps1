param([Parameter(Mandatory = $true)][string]$AnalysisRoot)

$root = (Resolve-Path -LiteralPath $AnalysisRoot).Path
$decoded = Join-Path $root 'decoded\all_arc\gameres'
$catalogs = Join-Path $root 'catalogs'
$encoding = [Text.Encoding]::GetEncoding(949)

function Write-Utf8Csv($Rows, [string]$Path) {
    [IO.File]::WriteAllLines($Path, @($Rows | ConvertTo-Csv -NoTypeInformation), [Text.UTF8Encoding]::new($true))
}

$quests = foreach ($file in Get-ChildItem (Join-Path $decoded 'duty') -File -Filter 'duty*.txt') {
    $text = $encoding.GetString([IO.File]::ReadAllBytes($file.FullName)).Trim()
    $lines = @($text -split "`r?`n")
    $match = [regex]::Match($file.BaseName, '^duty(\d+)-(\d+)$', 'IgnoreCase')
    $sections = [ordered]@{}
    $current = 'header'
    foreach ($line in $lines) {
        if ($line -match '^\[(.+)\](.*)$') {
            $current = $matches[1]
            if (-not $sections.Contains($current)) { $sections[$current] = [Collections.Generic.List[string]]::new() }
            if ($matches[2].Trim()) { $sections[$current].Add($matches[2].Trim()) }
        } elseif ($current -ne 'header' -and $line.Trim()) {
            $sections[$current].Add($line.Trim())
        }
    }
    [pscustomobject]@{
        questId = if ($match.Success) { $match.Groups[1].Value } else { '' }
        stage = if ($match.Success) { $match.Groups[2].Value } else { '' }
        npcId = $lines[0].Trim()
        dialogue = if ($sections['대사']) { $sections['대사'] -join ' ' } else { '' }
        prompt = if ($sections['예/아니오']) { $sections['예/아니오'] -join ' ' } else { '' }
        yes = if ($sections['예']) { $sections['예'] -join ' ' } else { '' }
        no = if ($sections['아니오']) { $sections['아니오'] -join ' ' } else { '' }
        result = if ($sections['결과']) { $sections['결과'] -join ' ' } else { '' }
        source = 'gameres/duty/' + $file.Name
    }
}
Write-Utf8Csv ($quests | Sort-Object {[int]$_.questId}, {[int]$_.stage}) (Join-Path $catalogs 'quests.csv')

$musicFile = Join-Path $decoded 'set\midiset.txt'
$musicLines = @($encoding.GetString([IO.File]::ReadAllBytes($musicFile)) -split "`r?`n" | ForEach-Object {$_.Trim()} | Where-Object {$_})
$declaredCount = [int]$musicLines[0]
$music = foreach ($line in $musicLines[1..($musicLines.Count - 1)]) {
    if ($line -match '^(\d+)\s+(.+?)\s*$') {
        [pscustomobject]@{ contextId=$matches[1]; midi=$matches[2]; source='gameres/set/midiset.txt' }
    }
}
Write-Utf8Csv $music (Join-Path $catalogs 'music_map.csv')

[pscustomobject]@{ quests=$quests.Count; musicDeclared=$declaredCount; musicParsed=$music.Count }
