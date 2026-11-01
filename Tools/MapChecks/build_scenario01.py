"""Original script/resource helpers. The authored initial-roster baker was removed."""
from pathlib import Path
import uuid


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / '분석/Saver_1.2/decoded/all_arc'
OUT = ROOT / 'Assets/_Game/Resources/Scenarios/01'
NAMESPACE = uuid.UUID('23b3f21a-85ac-47cb-a63f-c304722b82c8')


def guid(path):
    return uuid.uuid5(NAMESPACE, str(path.relative_to(ROOT))).hex


def directory(path):
    path.mkdir(parents=True, exist_ok=True)
    if not path.with_name(path.name + '.meta').exists():
        path.with_name(path.name + '.meta').write_text('fileFormatVersion: 2\nguid: ' + guid(path) + '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')


def commands(file):
    rows = []
    for line, raw in enumerate(file.read_bytes().decode('cp949').splitlines(), 1):
        text = raw.split(';', 1)[0].strip()
        if not text:
            continue
        tokens = text.split()
        group = 0
        if tokens[0].isdigit() and len(tokens) > 1:
            group = int(tokens.pop(0))
        name = tokens.pop(0).upper()
        body = text.split(None, 5)[5] if name == 'MESSAGE' else ''
        args = tokens[:4] if name == 'MESSAGE' else tokens
        rows.append(dict(name=name, args=args, line=line, text=body, group=group))
    return rows


def main():
    raise SystemExit("싱글판 보완 편성 생성기는 삭제됐습니다. Tools/ScenarioPlacementChecks/apply_original_deployments.py를 사용하세요.")


if __name__ == "__main__":
    main()
