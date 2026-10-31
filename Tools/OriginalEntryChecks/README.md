# 새 원작 진입 경로 검사

```sh
python Tools/OriginalEntryChecks/build_entry_assets.py
python Tools/OriginalEntryChecks/check_entry.py
python Tools/OriginalRestart/check_foundation.py
```

첫 명령은 새 `Assets/_Original`의 검증된 원문에서 시작/로그인·서버/생성창 자료만 구성한다. 이전 `_Game` 생성기는 실행하지 않는다. UI/씬을 사용자가 다르게 편집한 상태에서는 변경을 먼저 확인한다.

두 번째 명령은 Unity6000.5.7f1 API의 Runtime/Editor/Player 조건 컴파일과 생산 계정/파일 저장/클라이언트 코드를 사용하는 .NET 관리형 검사를 수행한다. 실제 임시 디스크 파일에 세 직업 생성/재연결을 검사한다. 원작 PE 명령·새 씬 스크립트/아트 참조도 대조한다. 실제 Unity Play/렌더링/물리 입력 검사는 아니다.

마지막 명령은 원본 SourceIndex 자료와 새 기능의 경계를 확인한다. 계정·사용자 실패 저장은 검사 입력으로 사용하지 않으며 .NET 계정 fixture는 임시 디렉터리를 삭제한다.

실제 사용·로컬 호스팅 정책·원본 근거·미실행 항목은 `설계/원작_진입_UI_재구현.md`를 따른다. 현재 Unity 실행 시도는 활성 라이선스 없음으로 종료했으며 `unity-attempt.json`에 별도 기록한다.
