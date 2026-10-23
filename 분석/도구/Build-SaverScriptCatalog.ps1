param(
    [Parameter(Mandatory = $true)]
    [string]$MapScriptRoot,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$root = (Resolve-Path -LiteralPath $MapScriptRoot).Path
$encoding = [Text.Encoding]::GetEncoding(949)
$commands = @{}
$dialogues = [Collections.Generic.List[object]]::new()
$extensions = @('.ai', '.evt', '.dee', '.deo')

foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File) {
    $extension = $file.Extension.ToLowerInvariant()
    if ($extension -notin $extensions) { continue }

    $relative = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
    $lines = $encoding.GetString([IO.File]::ReadAllBytes($file.FullName)) -split "`r?`n"
    for ($lineNumber = 0; $lineNumber -lt $lines.Count; $lineNumber++) {
        $line = $lines[$lineNumber].Trim()
        if (!$line -or $line.StartsWith(';') -or $line -in @('(', ')')) { continue }

        $parts = @($line -split '\s+')
        $command = if ($parts[0] -match '^\d+$' -and $parts.Count -gt 1) { $parts[1] } else { $parts[0] }
        $command = $command.ToUpperInvariant()
        $key = "$extension|$command"
        $commands[$key] = 1 + [int]$commands[$key]

        if ($command -eq 'MESSAGE') {
            $dialogues.Add([pscustomobject]@{
                source = $relative
                line = $lineNumber + 1
                raw = $line
            })
        }
    }
}

$commandRows = foreach ($key in $commands.Keys) {
    $parts = $key -split '\|', 2
    [pscustomobject]@{
        extension = $parts[0]
        command = $parts[1]
        count = $commands[$key]
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$utf8Bom = [Text.UTF8Encoding]::new($true)
[IO.File]::WriteAllLines(
    (Join-Path $OutputDirectory 'script_commands.csv'),
    @($commandRows | Sort-Object extension, @{Expression='count'; Descending=$true} | ConvertTo-Csv -NoTypeInformation),
    $utf8Bom
)
[IO.File]::WriteAllLines(
    (Join-Path $OutputDirectory 'dialogues.csv'),
    @($dialogues | ConvertTo-Csv -NoTypeInformation),
    $utf8Bom
)

[pscustomobject]@{
    commandKinds = $commandRows.Count
    dialogueLines = $dialogues.Count
}
