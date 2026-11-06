# 2026-11-06 저녁 클라이언트·로컬 서버 검증

원시 `5C0D` 수신 → 원본 FIFO/Update → 대화·마법·제거/지도 변경 → 대본의 GETGENERAL/3C 요청 → 기본 로컬 서버의 원자 지급을 연결했다. 지급은 대본이 요청한 시점에 처리하며, 모든 보상을 종료 연출 뒤로 미루지 않는다. 원작 서버의 미확정 이벤트 발동·금액·아이템·클리어 정책은 생성하지 않았다.

추가로 수정한 사항:

- 이동 응답이 최종 위치/플래그를 먼저 저장하더라도 실제 이동 애니메이션이 끝날 때까지 이벤트를 대기한다.
- 원본 EVT 파일 로더와 다른10개 본문을 정정했다. 숫자 접두 텍스트8줄은 원본처럼 무효 명령 한 줄로 소비하고, END 전 빈 줄로 비활성화되는57번85·90은 실행 대상에서 제외한다. 유효 본문은1,247개다.
- 실제 MFC 호출에 맞춰 별칭의3바이트/COM 접미1바이트와 형식을 구분한다. `%S`는 `%s`로 바꾸지 않고 확인한 C로캘의 안전한 범위를 사용한다.
- 월드 UI 사본에만 있던 CP949 해석기를 실제 계정 서비스가 받을 새 정의에 연결했다. 팀 이름이 있는 대화의 해석기 누락을 실제 진입 메서드로 재현하고 수정했다.
- 원본0D mode1 대화 중 맵·전투 입력과 받은 개체의 효과를 유지한다. 도입/종료 연출의 별도 잠금은 유지한다.

## 확인한 범위

| 검사 | 결과 |
| --- | --- |
| 전체 API | Runtime232·Editor11, Runtime/Editor/Player 오류0·경고0 |
| 관리형 시나리오 |2,101 assertion; 이 중 캐릭터 지급335. 실제 전투 완주 수가 아님 |
| 원시0D 서비스 |351 assertion; 실제05/09 이벤트의 지급·중복·파일 재접속·이동 대기 포함 |
| 원본 EVT 로더 |112파일/1,247유효 본문·제어11·추출/파일 실패3경로, 불일치0 |
| 원본/생산 Update |4,994문맥·12,916프레임 비교, 미지원0·불일치0 |
| 대화 |1,474 MESSAGE/561고유 형식; 안전 별칭/형식589·wide 형식27을 구분, 불일치0 |
| 실제 MFC 문자열 경계 |Left/Mid143사례; 할당/복사 경계는 공급 |
| 원본 효과·입력 |MAGIC32·KILL export84·생산 mode1 요청4; 입력 원본32·관리형12, 서로 합산하지 않음 |
| 실제 진입 메서드 |현재420검사; 새 정의의 CP949 전달·코덱 변경·저장된 필드 출입구/파일 재접속 포함. 과거 gate baseline4와 분리 |
| 최종 통합 감사 |89보고서·현재 선언 소스·리소스·243스크립트 GUID 대조.89검사를 모두 새로 실행했다는 뜻이 아님 |
| 근거 감사 |5manifest/48근거/25보고서 참조. 늦은 거부 시 앞선 참조가 부분 수정되지 않는 제어 포함 |

수치는 서로 겹치므로 합산하지 않는다. `development-verification.json`은 실행 원장·현재 소스 SHA·110개 스냅샷의 SHA를 기록한다. `snapshots/`는 원본 보고서 바이트를 보존한다. `runs/`는 완료 실행, 준비 오류, 중단 기록을 구분한다. 원본 x86 실행도 명시한 CString·파일/압축·대상 조회·표면/음향·호스트 호출 경계 안에서의 검사다.

`reproductions/`에는 수정 전 한글 별칭/COM, EVT 로더10차이, 실제 월드 진입 해석기 누락을 보존했다. 검사 준비 오류는 제품 오류로 합산하지 않는다. 본문1,249개라는 이전 추출 수와 원시0D 미연결이라는 이전 패키지 표기는 당시 결과다.

## 실제 Unity 재검증

[테스트·검증 계획](../../../설계/테스트_검증_계획.md)의 저녁 절을 따른다. 별도 프로필에서50/51 개체·현재 팀·60 팀 이름을 준비하고0D 수신, 대화/클릭·행동/효과, F2 획득/기술·저장/재접속·중복 거부, 일반61 결과/복귀를 확인한다. 재접속 세션에도60 이름을 다시 준비한다.

실제 Unity Play·Editor Test Runner·OS 입력·청음은 실행하지 않았다. 기존 Windows 최초 창고 재접속 실패도 해결된 것으로 바꾸지 않았다. 이전 패키지·실패 저장, 사용자 입구/배치·원본 Data/SourceData/Resources/Scenes·ProjectSettings·Ghidra 프로젝트의 변경은0이다.

새 지급 프로필의 레벨1·XP0·클래스명 기본 이름·미출전·로컬 식별자는 로컬 서버 생성 정책이다. 원작 기본 능력치/직업1레벨 습득표와 이 운영 정책을 구분한다. 전체 클라이언트·서버·전체 실전 시나리오 완료를 판정하는 자료가 아니다.

## 새 검증 폴더에서 핵심 검사 재실행

Unity SDK·후기 클라이언트·inventory와 같은 추출본 경로를 준비한 뒤 `Tools/run_original_checks.py`에 다음 `--check`를 순서대로 지정한다. 상세 경로/PowerShell 예시는 [검사 실행 안내](../../CheckSupport/README.md)를 따른다.

1. `Tools/OriginalWorldChecks/check_gateways.py`
2. `Tools/OriginalWorldChecks/check_scenario_progress.py`
3. `Tools/OriginalWorldChecks/check_native_directed_events.py`
4. `Tools/OriginalWorldChecks/check_native_directed_controls.py`
5. `Tools/OriginalWorldChecks/check_native_directed_frames.py`
6. `Tools/OriginalWorldChecks/check_native_campaign_messages.py`
7. `Tools/OriginalWorldChecks/check_native_message_substrings.py`
8. `Tools/OriginalWorldChecks/check_native_directed_input.py`
9. `Tools/OriginalWorldChecks/check_native_event_loader.py`
10. `Tools/OriginalWorldChecks/check_native_event_corpus.py`

직접 `refresh_native_evidence_references.py`를 실행하는 것은 재검사가 아니다. 현재 소스 검사가 끝난 뒤 보고서 포인터만 갱신한다. 그 후 `check_native_evidence_manifests.py`와 `check_evidence_reference_refresh.py`로 근거/갱신 거부를 검사한다. 최종89보고서 통합은 다른 기존 보고서도 현재 소스여야 하며 오래된 보고서는 실제 생성기를 재실행한다. 이 패키지를 덮어써 새 통과처럼 표시하지 않는다.
