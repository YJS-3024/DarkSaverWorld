# 후기 Saver 설치본 카탈로그

원본은 `saver/data1.cab`이다. 설치 프로그램과 게임을 실행하지 않고 추출·정적 분석했다. 제품의 정확한 후기 버전명은 확정하지 않았다.

분석 결론과 적용 우선순위는 [후기 버전 분석 기준](../../설계/11_후기버전_분석기준.md)을 참고한다.

| 자료 | 내용 |
|---|---|
| `unity_catalogs/` | Unity 변환기 입력용 원문 호환 CSV. 조회용 최상위 CSV와 스키마가 다름 |
| `decoded_art/`, `native_music/` | 후기 변환에 사용한 원본 BMP·MIDI. 원문 바이너리이며 런타임에서 읽지 않음 |
| `native_overrides/MAP/` | 연결 검사 후 적용한 필드3개의 원본 MRC와 해당 버전 GRS |
| `summary.json` | 설치본 해시, 집계, 실패 범위 |
| `files.json`, `executables.json` | 파일 해시·이전 버전 비교·PE 타임스탬프·주소 재사용 가능 여부 |
| `decoded_manifest.json`, `decoded/` | 원문 텍스트. CP949에서 UTF-8로 변환하며 원본 바이트 해시를 매니페스트에 보존 |
| `tables.json` | ATR 수치·텍스트 토큰과 원문 행 번호. 중복 키를 지우지 않음 |
| `table_differences.json` | 이전 설치본과 비교. 성장표는 직업+레벨 복합 키 사용 |
| `jobs.csv`, `units.csv`, `items.csv`, `magic.csv` | 후기 직업·전체 유닛·아이템·마법/기술 조회용 카탈로그 |
| `maps.json` | MRC 크기·리소스 번호·장식 수·원본 해시 |
| `scenarios.json`, `reward_commands.json` | 시나리오별 원문 경로와 지급·클리어 관련 명령. 일반 상자/이벤트도 포함하므로 최초 클리어 보상표와 동일하지 않음 |
| `clear_rewards.json` | 후기 112묶음의 클리어 보상 분석. 동일 아카이브만 이전 해석을 승계하고 시나리오 10의 후기 실행 동작은 미확정으로 분리 |
| `reward_keyword_strings.json` | 후기 실행 파일의 GETGENERAL / GET_GENERAL 문자열 대조. 실행 검증과 구분 |
| `placement_commands.json` | 원문 CHARPOS·LOADSCENE·SETPOSITION 출현 위치. 동시 초기 배치 확정표가 아님 |
| `script_commands.json` | 스크립트 명령 출현 집계 |
| `new_job_artwork.json` | 169~172의 원본 시트 위치·크기·해시 |
| `source_quality.json` | 성장표의 비정형 행·중복·참조 미해결 항목 |
| `decode_errors.json`, `map_errors.json` | 복원 실패. 빈 목록도 결과로 보존 |
| `verification.json` | 별도 원본 대조와 카탈로그 검증 결과 |

`tables.json`의 `values`는 헤더 의미를 임의 보정하지 않은 원문 공백 토큰이다. 원래 행과 탭은 `decoded/`에서 확인한다. 성장표 일부는 열이 적으므로 모든 행을 동일한 18열 레코드로 해석하면 안 된다. 같은 키의 행도 보존한다. CSV의 `fields`는 원문 토큰의 JSON 배열이다.

재현은 저장소 루트에서 실행한다.

```sh
unshield -d /tmp/saver-late-extracted x saver/data1.cab
python 닥세_리소스/분석도구/build_late_saver_catalog.py /tmp/saver-late-extracted
```

Python 3과 Pillow가 필요하다. 기존 `analyze_client.py`의 ARC 디코더 및 `decode_visuals.py`의 MRC 파서를 재사용한다. Windows 실행 파일 실행·원작 서버 판정 복원·Unity 임포트나 Play 검증은 포함하지 않는다.
