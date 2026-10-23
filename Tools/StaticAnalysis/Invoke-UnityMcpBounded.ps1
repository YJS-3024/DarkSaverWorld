param([Parameter(Mandatory=$true)][string]$Code, [ValidateRange(1,180)][int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
$clock=[Diagnostics.Stopwatch]::StartNew()
function Assert-SafeToContinue {
    if($clock.Elapsed.TotalSeconds -ge $TimeoutSeconds){throw 'Total MCP wait deadline reached; remote Unity execution may still be running. Do not retry automatically.'}
}
Assert-SafeToContinue
# Unity Hub also runs unity.exe; only the installed Editor can serve project commands.
$editors=@(Get-Process -Name Unity -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -match '[\\/]Editor[\\/]Unity\.exe$'
})
if($editors.Count -eq 0){throw 'Unity Editor is not running; Hub helper processes cannot execute project checks.'}
$info=New-Object System.Diagnostics.ProcessStartInfo
$info.FileName=Join-Path $env:USERPROFILE '.unity/relay/relay_win.exe'
$info.Arguments='--mcp';$info.UseShellExecute=$false;$info.CreateNoWindow=$true
$info.RedirectStandardInput=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
$client=New-Object System.Diagnostics.Process;$client.StartInfo=$info
function Send-Request($Message){$client.StandardInput.WriteLine(($Message|ConvertTo-Json -Depth 10 -Compress))}
function Read-Response([int]$Id){
    while($clock.Elapsed.TotalSeconds -lt $TimeoutSeconds){
        $read=$client.StandardOutput.ReadLineAsync()
        while(-not $read.Wait(500)){Assert-SafeToContinue}
        Assert-SafeToContinue
        if($null -eq $read.Result){throw 'MCP stdout closed'}
        $reply=$read.Result|ConvertFrom-Json
        if($reply.id -eq $Id){return $reply}
    }
    throw "MCP response $Id timed out; remote Unity execution may still be running."
}
try{
    [void]$client.Start();$stderr=$client.StandardError.ReadToEndAsync()
    Send-Request @{jsonrpc='2.0';id=1;method='initialize';params=@{protocolVersion='2024-11-05';capabilities=@{};clientInfo=@{name='bounded-project-verification';version='1'}}}
    [void](Read-Response 1)
    Assert-SafeToContinue
    Send-Request @{jsonrpc='2.0';method='notifications/initialized'}
    Send-Request @{jsonrpc='2.0';id=2;method='tools/list';params=@{}}
    $list=Read-Response 2
    if(-not ($list.result.tools|Where-Object name -eq 'Unity_RunCommand')){throw 'Unity_RunCommand unavailable'}
    Assert-SafeToContinue
    Send-Request @{jsonrpc='2.0';id=3;method='tools/call';params=@{name='Unity_RunCommand';arguments=@{Code=$Code;Title='Bounded project verification'}}}
    $response=Read-Response 3
    if($response.error){throw ($response.error|ConvertTo-Json -Compress)}
    foreach($part in $response.result.content){
        if($part.type -ne 'text'){continue}
        try{$result=$part.text|ConvertFrom-Json}catch{$part.text;continue}
        [pscustomobject]@{Success=$result.success;Logs=$result.data.executionLogs;
            Compilation=$result.data.compilationLogs;Error=$result.error;Message=$result.message}|ConvertTo-Json -Depth 5 -Compress
        if($result.success -eq $false){throw 'Unity command reported failure; see diagnostic output above.'}
    }
}finally{
    if($client.Id -and -not $client.HasExited){$client.Kill();[void]$client.WaitForExit(5000)}
    $client.Dispose()

}
