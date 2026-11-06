# 다른 환경에서 원본 클라이언트 검사 실행

`Tools/run_original_checks.py`는 원래 검사 파일과 하위 Python 호출을 그대로 실행한다. assertion/생산 코드를 제거하지 않는다. Linux에 고정된 Unity/임시/추출본 경로만 지정한 위치로 대응하고 C# 응답 파일의 인수를 따옴표로 감싼다. Python 3.10 이상, 해당 Unity SDK, 검사별 Python 의존성이 필요하다. 시나리오 음원 길이 검사는 ffprobe(FFmpeg 배포물)가 현재 프로세스 PATH에 있어야 한다. 실행 전 `ffprobe -version`을 확인하고, 없다면 검증 전용 폴더에 준비한 실행 파일의 배포처·SHA256을 기록한 뒤 해당 터미널의 PATH에만 추가한다. 누락 시 음원 검사 미실행/실패를 보존하며 청음 통과로 대체하지 않는다. 네이티브 검사는 정확한 버전의 원본 PE 파일도 필요하며 기존 SHA 검사는 유지된다.

Windows PowerShell 예시(실제 경로로 변경):

```powershell
python -m pip install -r Tools/OriginalWorldChecks/requirements-native-emulation.txt
python -m pip install pefile capstone
python Tools/run_original_checks.py `
  --unity-data 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Data' `
  --work-dir 'C:\DarkSaverChecks' `
  --installer-root 'D:\Extracted\ds-installers' `
  --late-client 'D:\Extracted\late-saver' `
  --normalize-evidence-eol `
  --check Tools/CheckSupport/check_environment.py `
  --check Tools/OriginalWorldChecks/check_training.py `
  --check Tools/OriginalWorldChecks/check_map_animation.py `
  --check Tools/OriginalWorldChecks/check_pixel_importers.py `
  --refresh-integration
```

- `--unity-data`: DotNetSdk가 들어 있는 Unity 데이터 폴더. Editor 라이선스/Play 실행을 대신하지 않는다.
- `--work-dir`: 새 검증 전용 임시 폴더. 하위 검사와 컴파일 템플릿이 같은 폴더를 공유하므로 동시에 실행하지 않는다. 실제 계정/저장 폴더를 지정하지 않는다.
- `--installer-root`: installer_inventory.json의 `/tmp/ds-installers`에 대응하는 실제 추출 폴더. 원래 payloads 구조를 유지해야 한다.
- `--late-client`: 후기 Saver.exe·GameCtl.dll·UserUnit.dll·setunit.dll 등 해당 검사에 맞는 파일 폴더. 구형1.2 파일로 대신 채우지 않는다. 누락/버전 차이는 원래 검사에서 실패한다. 실행기는 다른 버전을 자동 선택하지 않는다.
- 다른 고정 경로는 `--path-map '기존접두사=실제폴더'`로 추가한다. 예: 구형 UserUnit의 별도 위치. 실제 원본 확보가 필요한 미실행을 통과로 바꾸는 옵션은 없다.
- `--normalize-evidence-eol`: 생성 분석 JSON인 waiting_tutorial_resources.json과 ClientWindows/native-boundaries.asm.txt 두 파일만 대상으로 한다. CRLF→LF 결과가 각 manifest의 기존 선언 SHA256과 정확히 일치할 때만 쓴다. 두 파일을 모두 사전 검증하므로 하나라도 내용이 다르면 아무 파일도 변경하지 않는다. 내용이 다르면 파일을 보존하고 실패한다. 기존 Windows 사본은 새 .gitattributes만 풀받아도 이미 있는 CRLF가 바뀌지 않을 수 있어 이 옵션을 제공한다. 원본 아카이브/SourceData/CSV는 변경하지 않는다. 튜토리얼 추출기는 이제 UTF-8 바이트로 LF를 직접 생성하므로 하위 추출 실행 후 다시 CRLF로 바뀌지 않는다. 별도 usercustomize 패치는 필요하지 않다.
- `--check`는 여러 번 지정할 수 있고 순서대로 실행한다. 수련장 검사는 기존 성장→월드/캠페인 의존 검사도 실행해 C# 템플릿을 준비한다. 임시 산출물을 직접 읽는 다른 검사는 해당 선행 검사부터 실행해야 한다.
- `--refresh-integration`: 명시한 `--check`들이 성공한 뒤, 새 work-dir에 `contracts.rsp`/`EntryChecks.runtimeconfig.json`이 없으면 자동 감사의 소비 검사에 앞서 실제 `check_entry.py`를 실행한다. 이미 준비됐으면 이 초기 준비를 반복하지 않으며 실패하면 소비 검사를 시작하지 않는다. 이후 현재 선언 소스가 바뀐 보고서의 생성 검사를 순차 실행하고 다시 의존성을 계산한다. 픽셀 재검사 후 이를 참조하는 네이티브 보고서도 실제 재검사한다. 새 자산/메타가 추가되어 리소스 GUID 목록이 달라진 경우에도 바인딩 검사를 재실행한다. 해시만 덮어쓰지 않는다. 필요한 원본/선행 산출물이 없으면 실패 로그를 보존하며 최종 통과를 만들지 않는다.
  보고서 이름을 읽기만 하는 소비 검사와 실제 `write_text/write_bytes/open(w)` 생성 검사는 AST로 구분한다. 실제 생성기가 여러 개거나 확인되지 않으면 임의 선택하지 않고 실패한다.
- 각 실행 로그와 `run-results.json`은 work-dir에 남긴다. 시작할 때 기존 원장을 `running/completed=false`로 바꾸고 각 검사 뒤에 갱신한다. 최종 `passed/completed=true`만 성공이다. 생성기 모호성/의존 순환은 실패 원인을 기록하며, 중단·준비 오류의 미완료 원장을 예전 통과로 해석하지 않는다. 보고서는 기존 도구대로 저장소의 JSON에 갱신된다. 기존 검증 증거 폴더는 덮어쓰지 않는다.

일부 구형 `_Game` 검사에는 현재 삭제된 자산이 필요하다. 이 실행기는 삭제 자산을 복원하거나 검사를 생략하지 않는다. 현재 `_Original` 검사를 선택하고, 구형 검사 미실행은 별도 기록한다. 실제 Editor Search/AI 패키지 오류, NUnit 어셈블리 부재, 물리 입력·청음 검증은 이 실행기로 해결/대체하지 않는다.


원본 후기15/61/68 및 종료 호스트 근거의 보고서 포인터는 원본 파일 해시와 구분한다. 실제 생성 검사가 현재 소스로 성공한 뒤 `python Tools/GhidraChecks/refresh_native_evidence_references.py`를 실행하면선언된 manifest의 보고서 참조만 갱신한다. 네이티브 근거 파일의 기존 SHA가 바뀌었거나 참조 보고서의 선언 소스가 낡았으면 첫 쓰기 전에 거부한다. 테스트를 재실행하거나 입력/근거 해시를 다시 쓰는 도구가 아니다. 이후 `python Tools/OriginalWorldChecks/check_native_evidence_manifests.py`로 근거 파일·현재 참조·명시된0D/15/61/68 연결과B2 보류를 감사한다. 이 감사는 실제 Unity/Windows 실행을 대신하지 않는다.

`check_native_event_corpus.py`는 기존 `NativeImmediateEventsDifferential`의 생산 Events 파서를 재사용하고, 추가 C# 진입점으로 실제 요청 생성기 결과를 수집한다.112 EVT 파일의1,247개 유효 본문(이전 로더 정정 전1,249개)을 원본10F20에서 세 가지 팀/난수 문맥으로 실행하고 각 송신을 현재 요청 바이트와 대조한다. 새 work-dir에서는 먼저 같은 실행기로 `check_gateways.py`를 실행해 관리형 컴파일 템플릿을 준비한다. 기존 정상 템플릿을 다른 위치에서 재사용하려면 `--path-map /tmp/ds-npc-gateway-checks=<해당 절대 경로>`를 명시한다. 원본 파일 로더·0D 런타임·메시지 렌더/물리 입력·마법 명중·실제 음향 재생 검사는 포함하지 않는다.

기존 계약만 빠르게 다시 실행할 때 `check_native_magic_contracts.py`와 `check_native_battle_packet_contracts.py`는 기존 전체 계약 harness의 `run_contract`를 호출한다. 원래 넓은 회귀 진입점은 유지하며 필요한 원본/컴파일 선행 산출물은 먼저 준비한다. 서로 다른 실행기를 같은 work-dir에서 동시에 실행하지 않는다. 최종 통합은 완료 원장과 현재 소스 해시를 함께 확인한다.


원시0D 검사 순서는 `check_gateways.py`(기존 API/컴파일 템플릿) → `check_scenario_progress.py` → `check_native_directed_events.py` → `check_native_directed_controls.py`다. 이어 `check_native_directed_frames.py`, `check_native_campaign_messages.py`, `check_native_directed_input.py`를 실행한다. 마지막의 입력 검사는 원본 함수와 추출한 생산 처리 메서드를 비교하며 Unity Play나 OS 클릭을 실행하지 않는다. 원본 `%S` 검사는 설치 CAB의 `shared_dlls/mfc42.dll`·`msvcrt.dll`이 inventory의 payload 경로에 있어야 한다. 정확한 PE SHA는 보고서의 additionalNativeLibraries에 남으며 최신 Windows DLL로 대체하지 않는다. C로캘/할당/윈도우 경계도 보고서 scope를 따른다.

`check_native_battle_response_contracts.py`, `check_native_local_order_contracts.py`, `check_native_unit_snapshot_contracts.py`도 기존 `run_contract` 전체를 호출하는 집중 진입점이다. 선행 템플릿이 필요하며 assertion을 줄인 대체 검사가 아니다. 원본 scenario-end 보고서를 다시 생성했다면 이를 소비하는 `check_native_end_stage.py`도 실행한 뒤 통합한다.0D 근거 manifest는 기존 EventQueue와 별도 DirectedEvents에 있으며, 참조 갱신기는 모든 원본 해시/현재 보고서를 먼저 검증한다. 과거 패키지의 원장/소스 해시는 최신 값으로 덮어쓰지 않는다.

근거 참조 갱신을 수정한 경우 `python Tools/GhidraChecks/check_evidence_reference_refresh.py`도 실행한다. 이 검사는 manifest 사본에서 실제 갱신기를 호출하고, 마지막 manifest의 해시/참조 경로를 각각 손상시켜 앞선 manifest까지 부분 저장되지 않는지 확인한다. 저장소의 원본 근거·보고서·게임 파일은 읽기만 하며, 원본/관리형 검사의 재실행 건수로 합산하지 않는다.


`check_native_event_loader.py`는 `check_gateways.py`가 준비한 템플릿으로 생산 Events 파서를 컴파일하고, 실제 GameCtl13D60/F110/F310/F040에112개 EVT의 ReadString 행을 공급한다. 이전 관리형 추출1,249개에서 원본의 미완성 이벤트2개를 제외한1,247개가 현재 유효 본문이다. 실제 압축 DLL·Windows 파일 시스템은 실행하지 않으며 파일 미확보/열기 실패 시 이전 활성 플래그가 지워지는 경로를 따로 확인한다. `check_native_message_substrings.py`는 배포 MFC의 실제 Left/Mid 분기와 전달되는 복사 길이/오프셋을 검사하여 CP9493바이트·COM접미1바이트 해석을 보강한다. 실제 할당/문자열 복사는 명시한 경계이며 메시지 출력/Windows 로캘 재생의 대체 검사가 아니다.

`check_native_victory_notice.py`는 원본 수신20/OnLButtonDown/08CA0/08AD0/10F20을 생산 공유 대화 상태와 비교한다. 생성자·메모리 할당·그리기 경계는 공급하며 실제 창/OS 입력 검증으로 세지 않는다. 같은 실행기의 선행 `check_gateways.py` 컴파일 템플릿을 재사용한다. `check_native_directed_events.py`에는20 수신의 저장 비의존·수치 보존·거부/중복·재접속·0D 교체 검사가 포함된다.

`check_native_ai_dispatch.py`는023C0 큐 소비·02420 역할/cursor/clock 분기를 실제 x86으로 실행하고 하위 지휘관/용병 AI는 캡처한다. 기존 `check_native_soldier_ai.py`는 별도로041F0과 이동·행동 도우미를 실행해 이동 bit1/AP 최대/강제 휴식 경계를 포함한 현재 계획기를 비교한다. 둘의 건수와 구현 범위를 합산하지 않는다.

2026-11-07 새벽 보완: `--refresh-integration`은 명시한 검사보다 먼저 API 소스 범위·SHA와 컴파일 템플릿을 확인한다. 코드가 바뀌거나 추가·삭제되었으면 실제 `check_entry.py`를 먼저 실행하며, 실패하면 종속 검사를 시작하지 않는다. 기존 보고서의 SHA를 현재 파일로 바꾸어 통과시키지 않는다. `check_runner.py`의 격리 61검사는 이 순서와 실패 시 기존 보고서 보존을 포함한다.

`native_retreat_confirmation_emulator.py`는 원본 도망 확인창28의 송신/창 요청만 캡처한다. 확인 시 `5C61` 송신 후28을 닫고, 현재 시나리오의 원본 숫자 변환값<999 및 수신5C 값==1이면 결과창8을(110,10)에 여는6,152문맥을 `check_native_auxiliary_packets.py`에서 대조한다. 서버의 도망 승인·실제 결과표 갱신·raw5C 런타임 연결과 구분한다.

실행기의 `PYTHONPYCACHEPREFIX`는 작업 디렉터리별로 격리하고 바이트코드 쓰기는 끈다. 같은 수정 시각·파일 크기로 남은 이전 Python 캐시를 읽어 현재 소스 SHA와 다른 검사 코드를 실행하는 상황을 막는다. 기존 추적된 원본 도구의 `.pyc`는 지우거나 덮어쓰지 않는다.

## 새 작업 폴더의 컴파일 호스트 준비

실행기는 검사 코드가 `Path`로 직접 참조하는 기존 entry/campaign/gateway/scenario-progress 템플릿을 확인한다. 필요한 rsp·runtimeconfig·후속 콜백 파일이 없으면 해당 원래 `check_*.py` producer를 먼저 실행한다. 임시 파일만 만들어 통과시키지 않는다. 실제 준비 단계는 `run-results.json`의 `preparedFor`에 기록한다. 명시적 `--path-map`의 기존 준비 폴더도 지원한다.

자동 준비에는 상태와 생성 파일 SHA를 기록한다. 실패·중단·손상된 기록이나 변경된 생성 파일은 다시 준비하며, producer 실패·필수 파일 누락·순환 의존에서는 종속 검사를 실행하지 않는다. 파이썬 문법 오류는 준비 분석에서 숨기지 않고 실제 실패한 자식 검사로 기록한다. 이 기능은 위 네 컴파일 호스트에 한정하며 원본 DLL·설치본/Unity SDK 경로와 버전 SHA 검사는 그대로 필요하다.

최종 격리 제어101개를 확인했고 실제 빈 폴더·공백 포함 폴더의 원래 gateway/progress 준비→마법표/이벤트 검사를 실행했다. Windows 프로세스·Unity Play 통과로 해석하지 않는다.
