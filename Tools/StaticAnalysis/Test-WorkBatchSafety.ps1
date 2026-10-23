$ErrorActionPreference='Stop'
foreach($name in @('Invoke-UnityMcpBounded.ps1','Invoke-CombatBatchChecks.ps1','Watch-WorkBatchMemory.ps1')){
    $tokens=$null;$parseErrors=$null
    [void][Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $name),[ref]$tokens,[ref]$parseErrors)
    if($parseErrors.Count){throw ($parseErrors.Message -join '; ')}
}
# Exercise the actual response loop with a reader that never completes. No Unity request is sent.
class WorkBatchNeverReplyReader {
    [Threading.Tasks.Task[string]] ReadLineAsync(){
        $pending=[Threading.Tasks.TaskCompletionSource[string]]::new()
        return $pending.Task
    }
}
$tokens=$null;$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Invoke-UnityMcpBounded.ps1'),[ref]$tokens,[ref]$parseErrors)
$function=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Read-Response'},$true)
Invoke-Expression $function.Extent.Text
function Assert-SafeToContinue {
    if($clock.Elapsed.TotalSeconds -ge $TimeoutSeconds){throw 'EXPECTED_LOCAL_TIMEOUT'}
}
$client=[pscustomobject]@{StandardOutput=[WorkBatchNeverReplyReader]::new()}
$TimeoutSeconds=1;$clock=[Diagnostics.Stopwatch]::StartNew();$timedOut=$false
try{[void](Read-Response 1)}catch{if($_.Exception.Message -eq 'EXPECTED_LOCAL_TIMEOUT'){$timedOut=$true}else{throw}}
$elapsed=$clock.Elapsed.TotalSeconds
if(-not $timedOut -or $elapsed -gt 3){throw 'Response deadline self-test failed'}
[pscustomobject]@{Passed=1;TimeoutSeconds=[math]::Round($elapsed,2)}|ConvertTo-Json -Compress
