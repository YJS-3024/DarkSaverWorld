# 시나리오 01~17 보완 배치 정돈

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
