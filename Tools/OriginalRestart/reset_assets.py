"""Prepare a source-only Unity foundation; --apply backs up before replacing Assets.

No original installer/executable is executed. No previous Unity gameplay is copied.
The current working tree (including uncommitted Assets) is archived outside Assets.
"""
import argparse
import hashlib
import io
import json
import shutil
import sys
import tarfile
import uuid
from datetime import datetime, timezone
from pathlib import Path, PurePosixPath

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'Assets'
SOURCE = ROOT / '분석/Saver_Late'
REPORT = ROOT / 'Tools/OriginalRestart/reset-report.json'
NATIVE_SUFFIXES = {'.arc', '.mrc', '.grs', '.mid', '.wav', '.bmp', '.atr', '.lsc', '.txt',
                   '.lce', '.lnm', '.ltn', '.mxf', '.dat', '.ini'}
TEXT_SUFFIXES = {'.ai', '.deo', '.dee', '.evt', '.set', '.pos', '.srf', '.atr', '.txt'}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def safe_relative(value):
    value = value.replace('\\', '/')
    path = PurePosixPath(value)
    if path.is_absolute() or '..' in path.parts:
        raise ValueError('Invalid source path: ' + value)
    return path


def sources():
    inventory = json.loads((ROOT / '분석/InstallerVersions/installer_inventory.json').read_text())
    late = next(p for p in inventory['payloads'] if 'saver.exe' in p['containers'])
    canonical = {r['path'].lower(): r['sha256'] for r in json.loads((SOURCE / 'files.json').read_text())}
    result = []
    for row in late['files']:
        key = str(safe_relative(row['file'])).lower()
        if Path(key).suffix not in NATIVE_SUFFIXES or key not in canonical:
            continue
        path = Path(row['extractedFile'])
        if row['sha256'] != canonical[key] or not path.is_file() or digest(path.read_bytes()) != canonical[key]:
            raise ValueError('Original source hash mismatch: ' + key)
        result.append((key, path, canonical[key]))
    if len(result) < 1000:
        raise ValueError('Incomplete original data inventory')
    return sorted(result)


def meta(path, base, folder=False):
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'DarkSaverOriginalRestart/' + path.relative_to(base).as_posix()).hex
    if folder:
        body = 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix == '.png':
        body = '''TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  spriteMode: 1
  spritePixelsToUnits: 32
  alphaIsTransparency: 0
  textureType: 8
  maxTextureSize: 16384
  textureCompression: 0
  userData: Original pixels only; no scaling, alpha key, placement or timing rule.
  assetBundleName:
  assetBundleVariant:
'''
    elif path.suffix == '.cs':
        body = 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix == '.asmdef':
        body = 'AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    else:
        importer = 'TextScriptImporter' if path.suffix in {'.txt', '.bytes', '.json', '.csv'} else 'DefaultImporter'
        body = importer + ':\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    Path(str(path) + '.meta').write_text('fileFormatVersion: 2\nguid: ' + guid + '\n' + body, encoding='utf-8')
    return guid


def stage_originals(stage, selected):
    sys.path.insert(0, str(ROOT / '닥세_리소스/분석도구'))
    import analyze_client
    import build_late_saver_catalog

    base = stage / '_Original'
    rows, skipped = [], []

    def store(relative, raw, source, **evidence):
        path = base / relative
        if path.exists():
            raise ValueError('Duplicate output: ' + relative)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(raw)
        rows.append(dict(asset='Assets/_Original/' + relative, sha256=digest(raw), source=source, **evidence))

    def artwork(relative, raw, source, **evidence):
        try:
            with Image.open(io.BytesIO(raw)) as picture:
                image = picture.convert('RGB')
        except (OSError, ValueError) as error:
            skipped.append(dict(source=source, reason=type(error).__name__, rawRetained=True))
            return
        output = io.BytesIO()
        image.save(output, format='PNG')
        # Preserve original dimensions and every RGB pixel; do not recolor or key black.
        check = Image.open(io.BytesIO(output.getvalue())).convert('RGB')
        assert check.size == image.size and check.tobytes() == image.tobytes()
        store('Art/' + relative + '.png', output.getvalue(), source,
              originalBytesSha256=digest(raw), pixelSha256=digest(image.tobytes()),
              width=image.width, height=image.height, **evidence)

    for key, path, source_hash in selected:
        raw = path.read_bytes()
        store('SourceData/Native/' + key + '.bytes', raw, 'Saver_Late/' + key, originalSha256=source_hash)
        if path.suffix.lower() == '.bmp':
            artwork('Native/' + key, raw, 'Saver_Late/' + key, archiveSha256=None)
        if path.suffix.lower() != '.arc':
            continue
        for entry, method, size, packed in build_late_saver_catalog.arc_entries(path):
            entry = str(safe_relative(entry)).lower()
            suffix = Path(entry).suffix
            if suffix not in TEXT_SUFFIXES and suffix != '.bmp':
                continue
            if method not in (0, 1):
                raise ValueError('Unknown ARC compression method: ' + str(method))
            unpacked = (analyze_client.lzh_decode if method == 0 else analyze_client.lzss_decode)(packed, size)
            assert len(unpacked) == size
            origin = 'Saver_Late/' + key + '/' + entry
            if suffix == '.bmp':
                artwork('Archives/' + key + '/' + entry, unpacked, origin, archiveSha256=source_hash)
            else:
                try:
                    text = unpacked.decode('cp949').encode('utf-8')
                except UnicodeDecodeError:
                    skipped.append(dict(source=origin, reason='Not strict CP949 text', rawRetained=True))
                    continue
                store('SourceData/Decoded/' + key + '/' + entry + '.txt', text, origin,
                      originalBytesSha256=digest(unpacked), archiveSha256=source_hash, encoding='CP949 to UTF-8')
    for path in sorted((SOURCE / 'unity_catalogs').glob('*.csv')):
        store('Data/Tables/' + path.name, path.read_bytes(), path.relative_to(ROOT).as_posix(), interpretation='Original table columns; not final server rules')
    for name in ['scenario_monster_placements.json', 'scenario_monster_placements.csv']:
        path = SOURCE / name
        store('Data/Placements/' + name, path.read_bytes(), path.relative_to(ROOT).as_posix(), interpretation='Reference only; no runtime unit spawn')
    for name in ['files.json', 'maps.json', 'summary.json', 'source_quality.json', 'entry/layout.json']:
        path = SOURCE / name
        store('SourceData/Manifests/' + name, path.read_bytes(), path.relative_to(ROOT).as_posix())
    write_json(base / 'SourceIndex.json', dict(version=1, originalVersion='Saver_Late',
               scope='Original bytes, strict decoded text and pixel-preserving images only. No gameplay, server-rule, scene-placement or UI-composition migration.',
               sources=rows, skippedDerivedOutputs=skipped))
    return base, rows, skipped


def backup_current(backup):
    # Capture actual working files, not git HEAD, so uncommitted changes survive.
    paths = sorted(p for folder in ['Assets', 'ProjectSettings', 'Packages'] for p in (ROOT / folder).rglob('*') if p.is_file())
    if any(p.is_symlink() for p in paths):
        raise ValueError('Symlink in backup scope; inspect before reset')
    hashes = {p.relative_to(ROOT).as_posix(): digest(p.read_bytes()) for p in paths}
    write_json(backup / 'files-before.json', hashes)
    archive = backup / 'working-files.tar'
    with tarfile.open(archive, 'w') as target:
        for path in paths:
            target.add(path, arcname=path.relative_to(ROOT).as_posix(), recursive=False)
    with tarfile.open(archive, 'r') as saved:
        for member in saved.getmembers():
            if member.isfile() and digest(saved.extractfile(member).read()) != hashes[member.name]:
                raise ValueError('Backup verification failed: ' + member.name)
    if any(digest((ROOT / path).read_bytes()) != value for path, value in hashes.items()):
        raise ValueError('Working files changed during backup; reset aborted')
    return archive, hashes


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    if not (ASSETS / '_Game').is_dir() or (ASSETS / '_Original').exists():
        raise SystemExit('Expected previous _Game and no _Original. Refusing a repeated or unknown reset.')
    selected = sources()
    old_files = [p for p in ASSETS.rglob('*') if p.is_file()]
    preview = dict(originalNativeFiles=len(selected), previousAssetFiles=len(old_files),
                   previousScripts=sum(p.suffix == '.cs' for p in old_files), previousScenes=sum(p.suffix == '.unity' for p in old_files))
    if not args.apply:
        print(json.dumps(dict(applied=False, **preview)))
        return
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    backup = ROOT / '.local-backups' / ('assets-restart-' + stamp)
    backup.mkdir(parents=True, exist_ok=False)
    # Complete and verify source staging and backup before any Assets deletion.
    stage = backup / 'staged'
    base, rows, skipped = stage_originals(stage, selected)
    archive, hashes = backup_current(backup)
    # Setup is supplied as reviewed templates alongside this script.
    for path in sorted((Path(__file__).parent / 'foundation').rglob('*')):
        if path.is_file():
            destination = base / path.relative_to(Path(__file__).parent / 'foundation')
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, destination)
    for path in sorted(base.rglob('*')):
        if path.is_file() and path.suffix != '.meta':
            meta(path, stage)
    for path in sorted(stage.rglob('*')):
        if path.is_dir():
            meta(path, stage, folder=True)
    start = base / 'Scenes/Start.unity'
    start_guid = meta(start, stage)
    shutil.rmtree(ASSETS)
    ASSETS.mkdir()
    shutil.move(str(base), str(ASSETS / '_Original'))
    shutil.move(str(stage / '_Original.meta'), str(ASSETS / '_Original.meta'))
    (ROOT / 'ProjectSettings/EditorBuildSettings.asset').write_text(
        '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!1045 &1\nEditorBuildSettings:\n'
        '  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Scenes:\n  - enabled: 1\n'
        '    path: Assets/_Original/Scenes/Start.unity\n    guid: ' + start_guid + '\n  m_configObjects: {}\n', encoding='utf-8')
    report = dict(applied=True, **preview, backup=archive.relative_to(ROOT).as_posix(),
                  backupIndex=(backup / 'files-before.json').relative_to(ROOT).as_posix(),
                  backupFiles=len(hashes), backupVerified=True, preservedSourceOutputs=len(rows),
                  nativeFiles=len(selected), imageOutputs=sum(r['asset'].endswith('.png') for r in rows),
                  decodedTextOutputs=sum('/Decoded/' in r['asset'] for r in rows),
                  tableOutputs=sum('/Tables/' in r['asset'] for r in rows),
                  skippedDerivedOutputs=skipped, originalAnalysisModified=False, savesModified=False,
                  buildScenes=['Assets/_Original/Scenes/Start.unity'],
                  unityExecuted=False, newGameImplemented=False)
    write_json(REPORT, report)
    print(json.dumps(report, ensure_ascii=False))


if __name__ == '__main__':
    main()
