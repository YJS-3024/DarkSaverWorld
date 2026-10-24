"""Compile linked production C# without NuGet or Unity; host stubs are explicit.
Usage: python Tools/AnalysisIntegrationChecks/run_checks.py /path/to/dotnet
"""
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

folder = Path(__file__).resolve().parent
root = folder.parent.parent
dotnet = Path(sys.argv[1]).resolve()
home = dotnet.parent
version_key = lambda p: tuple(int(n) for n in p.name.split('.') if n.isdigit())
compiler = max((home / 'sdk').iterdir(), key=version_key) / 'Roslyn/bincore/csc.dll'
reference = max((home / 'packs/Microsoft.NETCore.App.Ref').iterdir(), key=version_key) / 'ref/net10.0'
sources = [folder / 'HostStubs.cs', folder / 'Program.cs', folder / 'Scenario01Checks.cs', folder / 'TownQuestChecks.cs', folder / 'CampaignChecks.cs', folder / 'LocalServerChecks.cs', folder / 'LoginFlowChecks.cs', folder / 'AccountChecks.cs', folder / 'ShopInteriorChecks.cs', folder / 'WorldAudioChecks.cs']
for node in ET.parse(folder / 'AnalysisIntegrationChecks.csproj').findall('.//Compile'):
    sources.append((folder / node.attrib['Include']).resolve())
env = dict(os.environ, DOTNET_CLI_HOME=str(Path(tempfile.gettempdir()) / 'ds-dotnet-cli'))
with tempfile.TemporaryDirectory(prefix='ds-csharp-checks-') as tmp:
    tmp = Path(tmp)
    (tmp / 'Usings.cs').write_text('global using System;\nglobal using System.IO;\nglobal using System.Linq;\nglobal using System.Collections.Generic;\n')
    output = tmp / 'AnalysisIntegrationChecks.dll'
    args = [str(dotnet), str(compiler), '-nologo', '-target:exe', '-langversion:latest', '-out:' + str(output)]
    args += ['-r:' + str(p) for p in sorted(reference.glob('*.dll'))]
    args += list(map(str, sources)) + [str(tmp / 'Usings.cs')]
    subprocess.run(args, cwd=root, env=env, check=True)
    (tmp / 'AnalysisIntegrationChecks.runtimeconfig.json').write_text(json.dumps({
        'runtimeOptions': {'tfm': 'net10.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '10.0.0'}}
    }))
    subprocess.run([str(dotnet), str(output)], cwd=root, env=env, check=True)
