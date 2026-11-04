"""Exercise opt-in path translation, child inheritance and unmodified failures."""
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile

rules = json.loads(os.environ['DARKSAVER_CHECK_PATHS'])
work = Path(next(new for old,new in rules if old == '/tmp'))
checks = 0
def check(value):
    global checks
    assert value
    checks += 1
check(Path('/tmp/example') == work/'example')
check(Path('/tmp')/'example' == work/'example')
check(Path('/tmp').joinpath('example') == work/'example')
check(Path(work/'example') == work/'example')
check(Path('/unmapped-client-evidence') == Path('/unmapped-client-evidence'))
child = subprocess.run([sys.executable, '-c', "import os;from pathlib import Path;assert str(Path('/tmp/example')).replace('\\\\','/').endswith('/example');assert 'DARKSAVER_CHECK_PATHS' in os.environ"], capture_output=True, text=True)
check(child.returncode == 0)
child = subprocess.run([sys.executable, '-c', "assert False, 'negative assertion control'"], capture_output=True, text=True)
check(child.returncode != 0 and 'negative assertion control' in child.stderr)
with tempfile.TemporaryDirectory(dir=work) as temp:
    response = Path(temp)/'argument file.rsp'
    response.write_text('-r:C:/Program Files/Unity/test.dll\n"already quoted"\n-r:"C:/Already Quoted/Code.dll"\n')
    child = subprocess.run([sys.executable, '-c', 'pass', 'csc.dll', '@'+str(response)])
    check(child.returncode == 0 and response.read_text().splitlines() == ['"-r:C:/Program Files/Unity/test.dll"','"already quoted"','-r:"C:/Already Quoted/Code.dll"'])
print(json.dumps(dict(checks=checks, unityPlay=False, scope='Opt-in Python path/child/compiler-argument adapter. Windows execution not implied.')))
