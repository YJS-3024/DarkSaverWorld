param(
    [Parameter(Mandatory = $true)][string]$ModulePath,
    [Parameter(Mandatory = $true)][UInt64]$Start,
    [int]$Size = 0x180,
    [string]$PythonExecutable
)

$analysisRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$analysisDependencies = Join-Path $analysisRoot 'Tools/StaticAnalysisDeps'
$analysisScript = Join-Path $analysisRoot '분석/Saver_1.2/tools/Inspect-SaverFunction.py'
if ([string]::IsNullOrWhiteSpace($PythonExecutable)) {
    $PythonExecutable = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
}
if (!(Test-Path -LiteralPath $PythonExecutable -PathType Leaf)) { throw 'Provide an installed Python executable with -PythonExecutable.' }
if (!(Test-Path -LiteralPath $analysisScript -PathType Leaf)) { throw 'Original static analysis script is missing.' }
if ($Size -le 0 -or $Size -gt 0x10000) { throw 'Use a bounded code range of 1..65536 bytes.' }
$analysisModule = (Resolve-Path -LiteralPath $ModulePath -ErrorAction Stop).Path
# Only pefile/capstone read the module bytes; the target EXE/DLL is never loaded or executed.
$analysisBootstrap = "import sys,runpy;sys.path.insert(0,sys.argv.pop(1));runpy.run_path(sys.argv.pop(1),run_name='__main__')"
& $PythonExecutable -c $analysisBootstrap $analysisDependencies $analysisScript $analysisModule --start ('0x{0:x}' -f $Start) --size $Size
if ($LASTEXITCODE -ne 0) { throw 'Static analysis failed. Install the pinned requirements into Tools/StaticAnalysisDeps.' }
