# 후기 데이터 적용·검증

`설계/11_후기버전_분석기준.md`의 확정 데이터를 실행 에셋에 반영한다. Python 3, UnityPy, Pillow, PyYAML이 필요하다. 음악 변환에는 libfluidsynth, TimGM6mb.sf2, ffmpeg가 필요하다. 원본 실행 파일과 저장본은 실행·수정하지 않는다.

저장소 루트에서 새 CAB를 unshield로 추출한 후 실행한다. 첫 도구는 기존 분석 매니페스트와 설치본의 모든 파일 해시를 대조한 뒤 에셋을 변경한다.

```sh
python Tools/LateVersionChecks/apply_late_data.py <추출루트>
python Tools/LateVersionChecks/apply_bound_art.py <추출루트>
python Tools/LateVersionChecks/apply_effect_art.py <추출루트>
python Tools/LateVersionChecks/apply_safe_maps.py <추출루트>
python Tools/LateVersionChecks/apply_late_audio.py <추출루트>
python Tools/LateVersionChecks/apply_entry_art.py <추출루트>
python Tools/LateVersionChecks/migrate_world.py <추출루트>
python Tools/LateVersionChecks/build_new_scenarios.py <추출루트>
python Tools/LateVersionChecks/check_world_campaign.py
python Tools/LateVersionChecks/verify_assets.py
python Tools/MapChecks/check_world_fields.py
python Tools/MapChecks/check_world_audio.py
python Tools/MapChecks/build_unit_status_pictures.py --check
```

기존 시나리오 베이커를 재실행했다면 이후에 후기 그림 갱신 도구를 다시 실행한다. 베이커의 기존 원본 스크립트/지형과 후기 그림을 혼동하지 않는다. 변경 횟수는 해당 도구의 마지막 실행 기준이므로 여러 실행 결과를 합산하지 않는다. 기본 카탈로그 GUID·기존 에셋 참조·사용자 확정 싱글판 규칙은 유지한다.

| 결과 파일 | 검증 내용 |
|---|---|
| `applied_data_check.json` | 카탈로그/개별 아이템·원본 색상 변환·미확보 그림 |
| `asset_readback_check.json` | 별도 Unity 바이너리 재읽기, 개별 정의 일치, Sprite 외부 GUID·UV·사각형 |
| `bound_art_check.json` | 사용 중인 프레임/초상/상태 그림의 후기 원본 픽셀 |
| `effect_art_check.json` | 후기 Fx 시트의 확인된 원본 사각형·프레임, 누락 패턴 유지 |
| `growth_check.json` | 실제 프로덕션 전직/성장/프로필 소스의 관리형 호스트 검사; Unity와 저장 IO는 대체 모델 |
| `map_migration_check.json` | 이전 안전 이관 기록;1000/1002/1010은 후속 world_migration_check를 따름 |
| `world_migration_check.json` | 세 맵 후기 지형, 시설 문, 보존한 왕복 연결 |
| `new_scenarios_check.json` | 신규8개 콘텐츠·근거·싱글판 편성/선행조건 |
| `world_campaign_check.json` | 세 맵/신규8개 원본 픽셀·문·복귀·대사·미디어·씬GUID·안전 복구 셀 |
| `audio_update_check.json` | 명시적 후기 BGM 변경과 이전 연결 유지 예외 |

성장/전투의 실제 프로덕션 C# 오프라인 검사는 `Tools/AbilityChecks/run_checks.py`를 사용한다. 먼저 Unity API를 참조해 최신 런타임 DLL을 컴파일해야 한다. 전체 카탈로그440개 중 큐리즘30901은 미확정으로 차단됐으며, 이를 시전 성공 건수에 넣지 않는다.

월드1000/1002/1010과 추가8개 시나리오의 실행 경로를 이관했다. 원작 서버의 출입구/편성/해금 복원, 후기 DLL 전체 ABI, 새 UI 배치까지 완료한 것은 아니다. 그랜드소드 동맹색과 신규 세 직업의 얼굴은 원본 누락으로 만들지 않았다. 실제 Unity 임포트·EditMode/Play·청음 검증은 별도이며 이 도구들의 통과가 대신하지 않는다.

전직/레벨300의 관리형 실행 검사는 `python Tools/LateVersionChecks/run_growth_checks.py <dotnet 실행파일>`로 재현한다. Unity 속성·ScriptableObject와 사용하지 않는 저장 선택 API만 호스트 대체물을 사용하며, 실제 프로필·마법·성장 코드는 변경 없이 컴파일한다. 이 결과는 Unity JsonUtility나 실제 저장 검증이 아니다.

기존01~17 베이커와 필드/마을/입구 생성기를 재실행했다면 **마지막에 migrate_world → build_new_scenarios** 순서로 후기 문·포털·복구 셀을 재적용한다. 기존 베이커가 검토된 전체 ScenarioCatalog/추가8개 입구를 삭제하지 않도록 했다. 신규 분기는17 이후33,330→331→332→333,404→405→406이며 원작 순서가 아닌 싱글판 연결이다.33의55명과 나머지42명 편성은 provenance를 따른다.

관리형 저장/규칙 회귀: `python Tools/AnalysisIntegrationChecks/run_checks.py <dotnet>` (설치 SDK 참조 팩 사용, NuGet 불필요). 신규 분기 실제 Unity 검증은 별도 저장본에서 선행 완료→물리 포털→연출→전투→종료→복귀→재접속 순서로 한다.35,737 호스트 검사와58,018 에셋 검사는 실제 전투/Unity Play 통과 수가 아니다.

시작·로그인 그림 추출은 `apply_entry_art.py`를 사용한다. `entry_art_check.json`에 원본 SHA256/잘라낸 사각형/출력 픽셀 해시와 실제 Unity 검증 여부를 남긴다. 로그인 창 원점226,279는 후기 실행 파일의 두 렌더링 경로에서 확인했고 Unity는197×87 BltFast 크기를 사용한다. `PYTHONPATH=Tools/StaticAnalysisDependencies python Tools/LateVersionChecks/analyze_entry_layout.py <추출루트>`로12개 명령 바이트 근거를 대조한다. 시작 버튼 위치는 로컬 구성이다. login2의 과금/서버오류 표시나 Logo1/2 전환을 임의로 재생하지 않는다.
