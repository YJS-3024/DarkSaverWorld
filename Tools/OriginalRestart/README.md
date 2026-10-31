# Assets 원작 재현 기반 초기화

현재 기준은 `설계/원작_재구축_시작기준.md`, 작업 위치는 `Assets/_Original`이다. 이전 게임 로직은 삭제했고 원본 설치본의 바이트·그림 픽셀·엄격한 디코딩 텍스트·원문 호환 표만 남겼다. 기존 Assets 작업본은 `.local-backups`에 해시 검증 후 보관했다.

```bash
python Tools/OriginalRestart/check_foundation.py
```

이 검사는 원본 해시/픽셀/테이블, 메타 참조, 새 시작 씬/빌드 목록, 백업을 확인한다. 현재 PC에 로컬 백업이 없으면 백업 검증도 통과하지 않는다. Unity에서 임포트·Play를 실행하는 검사가 아니다.

`independentSourceAudit`은 별도 후기 파일 인벤토리와 실제 보존 ARC를 기준으로 다시 대조한다. PNG/텍스트1266개를 원문 바이트에서 확인하고 CSV8개의 수치·행을 ATR와, 배치610유닛을 SET/POS의 원문 행과 직접 대조한다. CSV의 category/itemCategory·영문 필드 별칭은 분석용 표기다. 카메라7.5·Sprite PPU32는 새 Unity 작업용 기본값이며 원작 게임 규칙으로 확정하지 않는다. Assets 밖의 이전 Tools/검증 자료/로컬 백업은 이 감사의 삭제 대상이 아니며 Unity 실행 코드로 포함되지 않는다.

`reset_assets.py`의 기본 실행은 미리보기이고 `--apply`가 삭제 작업이다. 이전 `_Game`이 있고 `_Original`이 없을 때만 실행한다. 원본 전체 스테이징·실제 작업 파일 백업·SHA256 대조가 끝난 뒤에만 Assets를 교체한다. 임의의 프로젝트나 이미 초기화한 작업에는 재실행하지 않는다.

`foundation/`은 새 기반의 작은 템플릿이다. 이전 게임을 재생성하지 않는다. 원본 설치파일·분석·계정/실패 저장은 초기화 범위 밖이다. 실제 실행 기록은 reset-report/foundation-check/api-compile/unity-attempt JSON을 구분해 읽는다.

현재는 초기화 뒤 새 진입 기능을 추가한 단계다. `Tools/OriginalEntryChecks/check_entry.py`를 먼저 실행하면 새 Runtime/Editor 코드 해시를 기록하고, 기반 검사는 그 허용 목록과 EntryEvidence 파생 그림을 원본 SourceIndex와 구분한다. 새 게임 전체 완성을 의미하지 않으며 구현 범위는 시작/로그인·서버 선택·최초 지휘관 생성이다.
