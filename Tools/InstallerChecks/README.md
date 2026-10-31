# 설치본 비교 도구

`닥세_리소스/설치파일`의 원본을 실행하지 않고 추출·해시·데이터를 비교한다. Python 3와 InstallShield CAB를 읽는 `unshield`가 필요하다. 기존 `닥세_리소스/분석도구`의 ARC/LZH/LZSS 해석기를 재사용한다.

프로젝트 루트에서 순서대로 실행한다.

```bash
python Tools/InstallerChecks/audit_installers.py --work /tmp/ds-installers --unshield /tmp/ds-unshield
python Tools/InstallerChecks/compare_payload_data.py
python Tools/InstallerChecks/summarize_versions.py
```

`--unshield`에는 실제 실행 파일 경로를 지정한다. 결과는 `분석/InstallerVersions/`에 기록한다. `installer_inventory.json`의 `extractedFile`은 실행 당시 임시 경로이므로 재현할 때 첫 단계부터 실행한다. 해시와 설치 파일명은 원본 식별 기준이다. 원본이 있는 상태에서 다시 추출할 수 있으며 설치 EXE나 게임 실행은 하지 않는다.

- `installer_inventory.json`: 설치 컨테이너16개, 중복, CAB10종과 EGG 파일 묶음1종, 추출 파일별 해시/PE 타임스탬프/후기본 대조.
- `payload_data_comparison.json`: 버전별 ARC 내부 스크립트·표·SET/POS·튜토리얼 해시와 늑대 계열 그림4종. 패치에 없는 파일은 삭제로 판단하지 않는다.
- `version_comparison_summary.json`: 표의 숫자 행·키 차이, 변경 스크립트, 클라이언트 해시·문자열 존재 비교. 문자열 존재만으로 명령의 동작을 확정하지 않는다.
- `decoded/`: 같은 원문은 SHA256 앞16자리 디렉터리를 공유한다. UTF-8로 전사한 자료이며, JSON의 SHA256은 변환 전 CP949 원본 바이트 기준이다. SET/POS·튜토리얼·01/302 스크립트·기본 표를 보존한다.

`read_split_egg.py`는 제공된 비암호화 EGG3권의 헤더를 제거하고 파일/블록을 이어 읽는다. store/deflate/bzip2 블록을 지원하며 블록 CRC32·압축 전후 길이·완성 파일 길이를 검사한다. 이번 파일에서 사용한 코덱은 store와 deflate다. 임의의 모든 EGG 변형 지원을 뜻하지 않는다. 검사를 생략하는 `python -O`로 실행하지 않는다.

확인된 복호화 실패는 후기본과 동일 EGG에 들어 있는 백업 `ability_bak.at`, `levelabl_bak.a`의 EOF다. 현용 표·튜토리얼·SET/POS와 분리해 기록한다. 정적 파일 비교 결과이며 설치 성공·원작 실제 재생·Unity Play 검증 결과가 아니다.
