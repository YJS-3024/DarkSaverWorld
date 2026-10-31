# 시나리오 배치 도구·검증

## 원본 SET/POS를 편집툴에 연결

```bash
python Tools/ScenarioPlacementChecks/build_original_editor_layouts.py
python Tools/ScenarioPlacementChecks/check_editor_troops.py
```

첫 명령은 원문 SET/POS16맵(26,51~55,314~323)의610유닛·195아군 슬롯을 `Assets/_Game/Editor/BattleLayouts/Original{key}.json`으로 옮긴다. 원문 직업·수량·정수 좌표를 보존하며 새 대형/인원/출현 순서를 만들지 않는다. 기존 Runtime Content.json 및 씬을 바꾸지 않는다. 설치본 인벤토리의 추출 파일과 기존 ARC/LZH·Pillow·PyYAML을 사용한다.61직업/36직그림의 하향 첫 프레임을 Editor 전용 Art 폴더에 변환하고 단일 Sprite 설정을 기록한다.

이미 편집한 배치는 기본 재생성으로 덮어쓰지 않는다. 모든 대상의 편집 표시·유닛 내용을 쓰기 전에 검사하며 `--reset`을 명시해야 원문 배치로 되돌린다. `.meta` GUID는 경로 기준으로 안정적으로 생성한다. 결과 `original-editor-layouts.json`에 그림 원문/출력 해시와 범위를 기록한다.

Unity 편집툴의 **원본 배치 보기**, **[원본 SET/POS]** 선택 항목으로 연다. **[기존 콘텐츠]**는 원본과 구분해 남겨둔 기존 편성이다. 편집툴은 전체 배치를 표시하고 지휘관 ID로만 묶으며 웨이브 필터·수치 편집·부대 분리를 사용하지 않는다. Runtime 호환 데이터는 임의로 삭제하지 않는다. 원본16개를 원작 전체 시나리오의 초기 편성으로 확대 해석하지 않는다.

## 과거 시나리오 01~17 보완 배치 정돈 (현재 원본 배치 생성에 사용하지 않음)

이 자료는 사용자 요청에 따른 편집 배치다. 원작 서버의 확정 배치로 취급하지 않는다. 원본 SET/POS 분석 자료와 33 이후 시나리오는 수정하지 않는다.

적용 규칙:

- 기존 웨이브와 ID 십단위로 구성된 3마리 무리를 유지한다.
- 삼각형 또는 가로/세로 일렬로 정렬하며 구성원 간격은2칸이다. 서로 다른 무리도 같은 웨이브에서 한 칸 이상 떨어뜨린다.
- 기존 선두 유닛 위치에서 맨해튼8칸 이내의 통행 가능 연결 영역을 사용한다. 이전에 멀리 분산된 동료는 선두 근처로 모은다.
- traversalCells 밖, 이벤트의 CHARPOS 조건/CHANGESPRITE 대상 칸, 진입점 주변5×5 영역에는 놓지 않는다. 실제 씬의 모든 동적 장애물/출구 콜라이더를 실행 검증한 것은 아니다.
- 수량·종류·ID·웨이브·전투 수치·대사·연출·이벤트·진입 좌표는 보존한다. 좌표와 콘텐츠/배치 해시, 좌표 변경 이력만 변경한다. 연출 Visuals와 리소스 명세의 콘텐츠 해시는 동기화한다.

결과:17개 시나리오,336무리,1008마리 중870마리의 좌표 변경. 전 무리를2칸 간격 대형으로 배치했다. 변경 전후 좌표는 `regular-formation-report.json`에 기록한다. 같은 입력에 다시 실행하면 이동0개로 유지된다.

검증:

```
python Tools/ScenarioPlacementChecks/regularize_authored_placements.py
python Tools/ScenarioPlacementChecks/validate_regular_placements.py --baseline 90f84ce9
```

첫 명령은 미리보기만 수행한다. `--apply`를 명시해야 실제 콘텐츠를 변경한다. 콘텐츠를 원래 생성기로 재생성하면 이 정돈 배치가 덮어써질 수 있으므로 변경 이력을 확인하고 명시적으로 재적용한다. 기존 세이브는 콘텐츠 해시 불일치로 거부될 수 있으므로 별도 테스트 저장으로 확인한다.

검증 결과:무리 형태·간격·통행 칸·웨이브 내 중복 좌표·원본 편성/전투 수치/이벤트 보존·Content/Visuals 해시 일치·재실행 이동0개 통과. 실제 Unity Play는 미실행이다. 기존 check_scenario01_resources는 초상 픽셀 대조에서, check_campaign_resources는 입구 개수17 기대값에서 중단했다. 두 중단은 이번에 변경하지 않은 자산/입구 검사이며 해당 전체 검사를 통과로 처리하지 않는다.

## 원본 부대 구조 재해석 (2026-10-31)

```bash
python Tools/ScenarioPlacementChecks/analyze_troop_structure.py
```

설치본 추출 인벤토리와 추출된 후기 userunit.dll, 원본 SET/POS/AI, 현재01~17 Content.json을 읽는다. Python3·objdump·matplotlib가 필요하다. 게임/편집툴 소스·Content.json·저장본을 쓰지 않는다.

- `분석/Saver_Late/troop_structure_review.json`: 후기 지휘관 ID 계산 코드 근거, 원본16맵/610마리/122부대, 슬롯별 상대 모양, SET 없는 POS205행, AREA 불일치6건·근접 가설 오분류24마리, SOILDER22개 참조, 현재 보완 배치의 별도 대조.
- `evidence/late-troop-grouping.asm.txt`: 해시를 확인한 후기 코드 발췌. 원본 분기·opcode를 검사하며 실제 게임이나 명령 에뮬레이터를 실행한 결과는 아니다.
- `troop-structure-view.html`: 로컬 서버 없이 여는 읽기 전용 대조 화면. 원본/현재 보완 배치 선택, ID/근접 묶음 비교, 웨이브 선택, AREA/명시적 SOILDER 참조 표시. 원작 타일 이미지가 아닌 좌표 격자다. 전투/연출을 합친 화면이 아니다.
- `troop-structure-comparison.png`: 원본26번의 근접 묶음 오류와 관측 대형4종 비교 그림.

기존 비교기의 `AREA (` 공백 표기를 누락하던 정규식을 수정했다. 최신 부대 소속 판정은 근접 거리 대신 후기 코드의 `unitId/10*10`을 따른다. AREA와 SOILDER는 이동 기준으로 별도 해석하며, SET/POS의 런타임 자동 로드·미확보01~17 초기 편성을 증명하지 않는다. 이전3마리 정돈 배치를 새 분석으로 자동 덮어쓰지 않는다.

## Unity 편집툴 반영 검증

```bash
python Tools/ScenarioPlacementChecks/check_editor_troops.py
```

Unity 설치가 다르면 `--unity-data <Editor/Data>`로 경로를 지정한다. Unity가 제공한 .NET SDK/Roslyn·API DLL을 사용해 전체 Runtime과 변경된 편집툴/부대 해석기를 컴파일한다. `EditorTroopChecks.cs`는 실제 런타임 유닛 타입과 실제 `ScenarioPlacementTroops.cs`를 사용하며, 복제한 부대 구현을 검사하지 않는다. Unity Editor 실행·라이선스는 이 보조 검사에서 사용하지 않는다.

검사 범위는 지휘관/부하·웨이브 분리·지휘관 누락·슬롯 포화와 재사용·원본 대형4종·중복 좌표 거부·AREA 공백/잘못된 인수/SOILDER 파싱, 원본610마리/122부대 및 AI1099+22행이다. `editor-troop-check.json`에 소스 해시·건수·실행 범위를 기록하고 상세 로그는 `--work` 디렉터리에 쓴다. 모든 Content.json의 실행 전후 해시를 대조한다. 실제 GUI의 가독성·휠/클릭·드래그·파일 저장 성공을 통과 처리하지 않는다.

최신 검사는 기존 등장 단계 값이 달라도 ID 기준 지휘관/부하 관계가 유지되는지 확인한다. 편집툴에는 웨이브 필터 자체가 없다. 원본 편집본16개/610유닛의 직업·좌표·새 출현 단계 필드 부재를 원문과 대조한다. 실제 Unity에서 원본 항목 선택·61그림 임포트·부대/연출 토글·드래그·별도 편집본 저장/재열기를 확인해야 한다.
