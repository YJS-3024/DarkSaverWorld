"""Package only reviewed text files. This is not a legal-rights checker."""
import argparse
import ast
import hashlib
import json
from pathlib import Path
import zipfile

FILES = ('inspect_client.py', 'test_inspect_client.py', 'build_release.py',
         'README_KO.md', 'AI_GUIDE_KO.md', 'LEGAL_KO.md', 'PROVENANCE_AND_LICENSE.md')
IMPORTS = {'argparse', 'collections', 'hashlib', 'json', 'math', 'os', 'pathlib',
           're', 'stat', 'struct', 'contextlib', 'io', 'tempfile', 'unittest',
           'inspect_client', 'build_release', 'ast', 'zipfile'}


def build(output):
    root = Path(__file__).resolve().parent
    output = Path(output).absolute()
    if output.exists() or output.resolve() == root or root in output.resolve().parents:
        raise ValueError('Use a new ZIP path outside the toolkit')
    unexpected = {p.name for p in root.iterdir()} - set(FILES) - {'__pycache__'}
    if unexpected:
        raise ValueError('Unexpected files in toolkit; review them before packaging')
    payloads = {}
    for name in FILES:
        path = root / name
        if path.is_symlink() or not path.is_file() or getattr(path.stat(), 'st_file_attributes', 0) & 0x400:
            raise ValueError('Only regular allowlisted files are accepted')
        data = path.read_bytes()
        if len(data) > 100000 or b'\0' in data:
            raise ValueError('Unexpected size or binary content')
        text = data.decode('utf-8')
        if name.endswith('.py'):
            tree = ast.parse(text, filename=name)
            for node in ast.walk(tree):
                if isinstance(node, ast.Constant) and isinstance(node.value, (str, bytes)) and len(node.value) > 2000:
                    raise ValueError('Large literal requires review')
                if isinstance(node, ast.Import):
                    modules = [a.name for a in node.names]
                elif isinstance(node, ast.ImportFrom):
                    modules = [node.module or '']
                else:
                    modules = []
                if any(m.split('.')[0] not in IMPORTS for m in modules):
                    raise ValueError('Unexpected import requires review')
        payloads[name] = data
    manifest = dict(schema=1, scope='Generic structural tools only; no gameplay AI or editor.',
                    caveat='Mechanical checks do not establish legal clearance.',
                    files=[dict(path=n, size=len(d), sha256=hashlib.sha256(d).hexdigest())
                           for n, d in payloads.items()])
    payloads['release_manifest.json'] = json.dumps(manifest, ensure_ascii=False, indent=2).encode('utf-8')
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, 'x', compression=zipfile.ZIP_DEFLATED) as archive:
        for name, data in payloads.items():
            archive.writestr(name, data)
    with zipfile.ZipFile(output) as archive:
        if archive.testzip() is not None or set(archive.namelist()) != set(payloads):
            raise ValueError('ZIP integrity check failed')
        for name, data in payloads.items():
            if archive.read(name) != data:
                raise ValueError('ZIP content mismatch')
    print(json.dumps(dict(files=len(payloads), bytes=output.stat().st_size,
                         sha256=hashlib.sha256(output.read_bytes()).hexdigest()), indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', required=True)
    build(parser.parse_args().out)
