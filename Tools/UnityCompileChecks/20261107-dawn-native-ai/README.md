# 2026-11-07 새벽 개발 검증 보존본

`work / 367e2780` 위의 미커밋 작업이다. 사용자 요청의 오전07시(KST)는2026-11-06 22:00UTC다. 커밋·푸시는 이번 작업에서 실행하지 않았다. 이 디렉터리는 결과·검사 코드·원본 정적 증거·수정 전 반례를 함께 동결한다.

## 변경 및 확인한 범위

- 실제 setunit GetMagicTbl 연결로 속성/패턴 혼동을 발견했다. 수정 전880모드의860송신 차이를 보존하고, 생산 속성 필드·440행 카탈로그·Signature와 검사 입력을 수정했다. 파이어볼101은4,141이다. 원문440행의 실제 native/CRT17열 파싱도 공급 레코드와 일치했다. 이전102보고서 점검의 마법 서명 부분을 이번111보고서/실제 테이블 검사가 갱신한다.
- 원본 AI112파일/1,118행의 파서·월드 바인딩·편집툴 읽기 전용 규칙 조회. AREA를 초기 몬스터 배치로 바꾸지 않는다.
- 완전한 이동 ABI, HP0/07통지·자동/수동 제어·지도 점유·clock·50/51 hint reset 차이 수정. 실제 수신 팀이 legacy 적 목록보다 우선한다.
- 실제 수신 팀의64/65·68마법 대상 조건과 미수신 동맹 보류.
- 수신66의 정확한 CString 접미·02/08/12 a/b 대본 선택·성공한 종료 시작의 저널 고정. 로컬 서버의 a/b 선택 규칙은 만들지 않았다.
- 원본 지휘관 전체 행동1,247문맥과 실제 setunit 연결1,247대조·AI Update120문맥·0C/큐/프레임12·후속 용병14·52/AP256·B2실제 인자54흐름은 원본 분석이다. 생산 런타임0C 연결 완료가 아니다.
- 원본 효과78행/1,560변환·실제434투영·현재60패턴과 누락64/65의6행을 구분했다. 별도 실제 Magic/CBD0의표시 준비144문맥/전체736바이트 레코드288개를 대조했으며 이후 그리기/종료/Unity 상태 연결은 포함하지 않는다. B2의 실제 잔존 스택24흐름/사이 호출2흐름도 별도 분석이다. 생산0C/B2·전체 Windows 이력·시청각 검증으로 세지 않는다.
- 현재111보고서,Runtime235/Editor12·Player API 오류/경고0,247 script GUID. 검사는 범위가 겹치므로 assertion·문맥 수를 합산하지 않는다.

## 파일과 재검증

`development-verification.json`은 현재 소스/스냅샷 SHA, 통과 실행 원장, 반례 분류와 보호 범위를 기록한다. `snapshots/`는 검사 결과·소유 검사 코드·현재247개 C# 파일·정적 원본 증거를 보존한다. 외부 바이너리/의존성/전체 PNG를 재복사하지 않는다. 원본 AI81의 미등록80601/90 및 캐시 후속6문맥은 원작의 잘못된 요청 증거이며 임의 마법 치환/서버 승인으로 처리하지 않는다. `reproductions/`의37/10/6/8건은 수정 전 제품 차이다. 별도860건은 실제 마법 조회 테이블까지 연결해 발견한 생산/검사 입력의 공통 오류이며,2건은 fixture의 별도 mainmap 점유 경계여서 제품 수정으로 세지 않는다. `preparation/`의 컴파일/템플릿/입력 준비 실패도 최종 통과와 구분한다.

```sh
PYTHONDONTWRITEBYTECODE=1 python Tools/UnityCompileChecks/20261107-dawn-native-ai/verify_package.py
PYTHONDONTWRITEBYTECODE=1 python Tools/UnityCompileChecks/20261107-dawn-native-ai/verify_package.py --snapshot-only
PYTHONDONTWRITEBYTECODE=1 python Tools/UnityCompileChecks/20261107-dawn-native-ai/verify_source_eol.py --check-only
```

첫 검사는 동결본과 현재 선언 소스를 대조하므로 이후 코드가 바뀌면 실패하는 것이 정상이다. `--snapshot-only`는 과거 보존본의 무결성만 검사한다. EOL 검사는 해시 대상 Markdown5개와 보존본 전체 소스 경로를 포함한 실제 격리 Git/core.autocrlf checkout을 사용하며 Windows 프로세스나 Unity import로 세지 않는다. 새 작업 폴더는 실행기가 직접 참조된 entry/campaign/gateway/scenario-progress의 원래 준비 검사를 자동 실행한다. 기존 준비 폴더의 명시적 `--path-map`도 지원한다. 자동 준비 실패·중단·손상 기록/변경 파일을 완료로 보지 않는다. 격리101제어와 실제 공백 포함 새 작업 폴더를 검사했다. 마법 대상 원본 자료를 변경한 경우에는 `check_native_magic_target_catalog.py`를 실제 실행한 뒤 redirect 소비 검사를 한다. 실제 검사 재실행은 `Tools/run_original_checks.py`와 각 보고서의 producer·원장 인자를 따른다. 해시를 새 값으로 덮어써 통과시키지 않는다.

보존 검사는 최초 기준 커밋에 대한 보호 경로 차이와 이전 두 패키지의목록/기록 파일·기존 pycache6개도 다시 대조한다. 동일한 작업을 커밋한 것만으로 거부하지 않으며, 격리 Git10사례/변조 거부6사례를 `check_package_preservation.py`로 검사했다. 재실행은 `--check-only`로 보존본 파일을 바꾸지 않고 할 수 있다. 검사 본체와 제어 결과의해시도 비교한다.

## 실제 검증 제약

Unity Play/Test Runner·OS 입력·청음·원작 Windows 전체 실행·FPS 실측은 하지 않았다. 기존 Windows 창고 최초 재접속 실패도 미해결이다. 새벽 실제 Play 절차는 `설계/테스트_검증_계획.md`를 따른다. 공통 원시 수신 선언27개이며0C/30/40/5C/B2는 미연결이다. 원본 서버의 배치·피해·비용·분기·보상은 임의로 만들지 않았다. 사용자 입구/몬스터 배치·원본SourceData·이전 실패 저장과 보존 패키지는 변경하지 않았다.
