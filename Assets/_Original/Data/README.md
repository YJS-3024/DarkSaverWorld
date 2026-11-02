# 원문 호환 데이터

`WorldChatInput.json/png`는 F5 기록창19와 별개인 원본 입력창17/scene1이다. 두 토글의1/2/3종류와 승인된 전처리 목록·원본7A/후속7D 요청을 사용한다. 개인 귓속말 수신 창이나 서버 반향을 생성하지 않는다.

자기정보21의 `growth.stats.nativeValues`16개와 `growth.nativeLevels`2개는 명시된 원본 표시값이다.732바이트 메모리 사본 변환은 wire 디코더와 구분한다. `nextExperience`도 명시 목표만 보존하며 임계값을 계산하지 않는다. `identity`의 나이/성별/배우자는 공용 헤더여서 보유 캐릭터를 교환해도 이동하지 않는다. 승인된 부분 응답만 적용하고 미제공 필드는 보존한다. 레거시 저장의 값은—이며 실제 나이·성별·결혼 상태를 만들지 않는다.

보유 캐릭터의 조정 대상은 ID/목록 리비전 문맥을 사용한다. 전직·능력·마법 편성 승인은 해당 개인 기록에만 저장하며 공용 돈·맵·다른 캐릭터의 부상과 장비를 보존한다. 개인 성장/가방이 미확보이면 조정하지 않는다.


`Resources/OriginalBitmapFont`는후기MXF 두종류의원본14px 마스크와Unicode/CP949 glyph 매핑을사용한다. 기본창줄바꿈/정렬은uGUI 어댑터이며메모는원본단락진행모드를사용한다. `OriginalCodePage949.json`은서버메모해제 payload를위한표준CP949 변환표이며MXF글자그림과다르다. `WorldUtilities.json`은원본scene15/19/48의설정/도움말/메모창이다. 종료는기존scene27을재사용한다. 메모는승인해제 payload만추가하며실제서버메일함/7E압축전송을추정하지않는다. F12는7A0109원문요청만보내고워프값을만들지않는다. NativeTextSurface는논리화면경계에서글자전체를제외하며부모필드마스크/GPU출력은별도어댑터다.

`WorldRecord.roster`는 승인 소유 캐릭터의 순서·이름/직업·nativeRecord·nativeStatus·출전1/2·개인 상태를 보관한다. 조회 선택과0번 교환은 구분한다. 주 캐릭터 상태는 기존WorldRecord가 원본이며 공용 소지금/창고·맵은 교환하지 않는다. `CommanderRoster3.json`과CommanderRoster3/m 그림은 원본scene42/m의 대체F2이며 승인 gate와3개체 조건에서만 사용한다. 소유권·개인 능력치의 기본값을 생성하지 않는다.

음악의 현재 기준은 후기 `gameres/set.arc/midiset.txt`의 선언47항목이다.50개 원문 행 중 후행1503~1505는 로드하지 않는다. `OriginalMapMusic`/`NativeMapMusicState`가 번호 조회·성공 캐시·표 미등록 유지·실제 열기 실패·직접 대본/설정 분기를 구분한다. Unity는 현재 논리 맵 번호를 연결하고 별도 수련장 RoomNumber 별칭을 만들지 않는다. 원문 음악표와 대본을 합친32곡을 GM 합성/스트리밍으로 연결했으며 원본 장치 음색·실제 재생 속도는 미검증이다. 비동기 시작 대기 중에는 반복 카운터를 소모하지 않는다. 최신 검증은 테스트 계획 「맵·방별 BGM 선택·전환」과 `map-music-integration-check.json`을 따른다. 아래14곡/164검사 등의 이전 범위는 당시 기록이다.

시나리오 진행은 원문97개의 도입/종료와 명시 승인 전투·5C15 이벤트·결과/원점 복귀를 연결한다. `campaignProgress`는 방문의 분기/지도 변경/이벤트 재개, `clearedScenarios`는 로컬 최초 클리어 기록, `campaignDelivery`는 복귀/재접속 후에도 남는 미확인 원본 요청을 보관한다. 봉투 id/visit/session은 로컬 계약이며 원작 패킷에 추가하지 않는다. GETGENERAL/GOLD/USEITEM 요청 자체로 소유/돈을 만들지 않는다. COMBINE_CLASS의 화면 픽셀 좌표와 원본 RBM8장, 명시 MIDI14곡은 NativeCampaignArt에 연결했다. MIDI는 GM 합성이며 원본 장치 음색/실제 속도 검증이 아니다. nco02.mid는 원본에 없어 보류한다. 초기 편성16확보/3빈 편성/78미확보 및 운영값 보류는 유지한다.97개 승인 fixture 경로 통과를 실제97개 전투/Unity Play 통과로 해석하지 않는다. 자세한 근거·검증은 테스트 계획의 「시나리오 전체 진행 연결」을 따른다.

`NativeViewport`는 원본 F900의 논리 타일 범위(좌/상 포함·우/하 제외)를 표시용으로 적용한다. 현재 픽셀 스크롤과 부분 타일은 어댑터로 연결하며 카메라 계산 자체는 바꾸지 않는다. 상대9행의 큰 이동 몸체58·큰 물결26을 사용하고 대기 마른 몸체64·물 몸체48·일반32/25는 유지한다. 순회/몸체/공격/숫자/사망에 같은 범위를 쓰며 자원/타이머/저장 정책은 바꾸지 않는다. 실제 Unity/장치 픽셀 자르기·원작 카메라 위치는 미검증이다.

`NativeUnitFrames`는 원본1000FCD0의 몸체→공격/숫자 또는 사망 표시를 한 단위로 만든다. `NativeBodyOrder.Plan(includePostPasses:true)`와 WorldSceneRoot는1차 개체→장식→2차 개체→마법→최종 숫자를 합성한다. 공격 표식은 보행 오프셋, 타격/숫자는 현재 논리 타일을 사용한다. 위치 필드는 이 표시 입력에서는 픽셀 원점이다.4/40은 현재 생애의 명시 표시 슬롯에서 복사본으로만 해결하며 nativeState/저장/자원을 바꾸지 않는다. 이전 생애 효과와 없는 시체 그림을 표시하지 않는다. 실제 Unity 화면/전체 속도·가장자리 자르기는 미검증이다.

`NativeMapDecoder.RenderAnimatedLayers`는 원본 MRC 마지막 타일 바이트가2 미만이면 로딩 때 고른 무작위 고정 프레임,2 이상이면 맵 공통 BYTE 위상의 GRS 프레임을 사용한다. `NativeMapAnimationClock`은 clock 차이가300 초과일 때 한 번만 진행한다. 마을11개 원본 MRC/GRS 참조와 캠페인126개에 연결했으며, 표시 풀은 화면 안의 타일만 사용한다. 원본 자료/사용자 입구·NPC/통행 판정/서버 운영값은 변경하지 않았다. 편집툴의 기존 첫 프레임 출력은 유지한다. 실제 Unity 화면·당시 로딩 seed/전체 속도는 미검증이다.

맵의 `renderAttributes`는 MRC 타일5바이트 중 네 번째 원본 바이트다. 통행용 `attributes`와 분리해 표시 비트128을 유지한다. `NativeBodyOrder`는 원본 점유/외형/이동 종류에 따라 1차 몸체→맵 장식→2차 대형5조각을 선택하며 `WorldSceneRoot.BodyLayers`가 표시한다. 미리 변환한11개+캠페인126개 맵에 적용했다. 큰 연출 몸체를32픽셀로 축소하지 않는다. 원본 슬롯이 없는 NPC 표시 키는 로컬 순서이며 wire ID가 아니다. 실제 Unity 화면/가장자리 자르기·속도는 미검증이다.

최신 공통 수신은 이동5C01/0E(각8바이트)·마법 표시5C89(11+2N)도 포함한다. 송신 일반/특수기5C89/92는16+2N, 도구5C91은19+2N이다. `NativePacket`은 확인된 수동 모드1 마법/특수기를, `NativeMagicPacket(mode)`는 명시1/2를 제공한다. `NativeItemPacket(token)`의 원본 식별 토큰을 로컬 소지품 slot/GUID로 대신하지 않는다. 계산 DWORD는 ID×(pattern+unit+team+range+distance)이며 비용/피해 공식이 아니다.

`NativeBattleNotice.localTeam/sourceTeam/targetLives`는 로컬 문맥이다. raw proof를 재해석해 같은 팀 반향/0대상/음수 마법 무시·환경 source(-1,-1)·생애를 검사한다. 표시 패킷으로 자원/소비를 만들지 않는다. 실제 매니저의 `ExecuteNativeBattleCast`는 원본처럼 결과 전 표시만 시작하고 승인 때 중복 생성하지 않는다. 직접 형별 서비스 API와 시점이 다르므로 테스트 계획 최신 절을 따른다.

전체 보행의 축1001616C는 송신5좌표 축10016170과 별개다. 이동 중 통지는 한 칸을 보존하고 경계에서 최신 목적지를 계산한다. 보정은 해당 보행·명시 위치/이동 메뉴 비트만 갱신한다. 저장에는 승인 최종 상태를 보존하고 경로/축/프레임은 복원하지 않는다. 지휘3종은 원본 클라이언트 setsolai이므로 매니저 UI에서 외부 권위자를 요구하지 않는다.

`Art/Combat/Palettes`의219그림은 원본73개8비트 BMP의 A0~AF 인덱스만16×팔레트1/2/3만큼 옮겼다. `build_body_palettes.py`와 feedback/world 재생성에 연결한다. 기본 PNG와 원본 픽셀을 대조하며 나머지 색/검정 투명키/크기를 보존한다. `NativeBodyPalette.Resolve`는 원본 선호0/3/2와 명시 관계를 사용하고 unknown은 null이다. 선택적 `TrainingUnit.nativePalette`0~3과 `NativeWorldArt.ownPalette`는 클라이언트 표시 값이며 새 서버 필드가 아니다.

수면6/20(병종1/899 예외)에서25/48픽셀 크롭·원본 물결, 그림자·20/10 표식·980~998 이동주기·999 비표시를 사용한다. 대형5분할/전체 맵 합성·다른 플레이어 관계·실제 Unity 색공간/속도/청음은 변환만으로 통과 처리하지 않는다. 근거/검증은 테스트 계획 최신 절과 `native-movement-path-check.json`, `native-magic-packet-check.json`, `native-body-scenery-check.json`, `native-body-palette-check.json`을 따른다.

원본 전투 응용 패킷 공통 입력은 `Notify_BattlePacket/Notify_TrainingPacket(NativeBattleNotice)`다. 수신5C03·0F(4)/04(2)/05·06·07(6)/52·82(8바이트)를 처리한다. local 세션/리비전/생애/ID는 wire 추가 필드가 아니다. 송신 요청의 `NativePacket`은 공격5C82(10바이트, source/target/지형2WORD)·휴식5C0B(4바이트), 계산된 이동5C43(24바이트)을 제공하며 수신 공격(target/source/raw flag)과 혼용하지 않는다. 다른 종류·프레이밍/소켓 처리는 미확인 형식으로 대체하지 않는다.

`NativeBattleResponse`는 명시 기본 공격/휴식의 응답 패킷 배열과 로컬 승인ID/대기ID를 묶는다. 원작 TCP 봉투/서버 묶음 정책이 아니다. `NativeBattleAuthority/NativeTrainingAuthority`는 기존 형별 권위자가 없을 때만 선택하며 `Notify_BattleResponse/Notify_TrainingResponse`로도 받을 수 있다. source5C52, source/공격target5C05/06/0F, 일치하는5C82, 공격target5C07을 재해석·검증해 한 번 저장한 뒤 효과를 반영한다. 각 필드의 중복과 관계없는 통지는 거부한다. HP/MP 둘 다 반영하되 공유 숫자 슬롯은 마지막 통지만 남긴다.32패킷/각8바이트 제한은 로컬 봉투의 방어 조건이다. 임의 피해/비용/XP나 미수신 효과를 생성하지 않는다. 저장 실패는 동일 응답 재시도와 기존 상태를 보존한다. 기본 권위자는 설정하지 않는다.

`TrainingActionRequest.nativeMovePoints`는 원본 MoveCharPath→DD30이 만든 목적지부터 최대4칸 역추적의5좌표이며 전체 보행 경로/개수 필드가 아니다. 송신5C43은 source WORD와 signed 좌표10WORD, 남는 쌍은(-1,-1)로 총24바이트다. 이동 예산+7과 원본 동률 축10016170을 적용한다. 축 전후 값은 임시 클라이언트 문맥이며 프로필/서버 seed에 저장하지 않는다. 기존 보행 어댑터와5C01/0E 이동 수신 전체를 이 계산으로 대체하지 않는다.5C89는 마법이다. `check_move_hint_integration.py`는 기존 회귀/응답/요청 통합을, `check_native_move_hints.py`는 원본 x86의5좌표·축·바이트를 대조한다.

`TrainingUnitsNotice.feedbackEvents/lifecycleEvents`는 명시 표시/생애 통지다. 전체 unit 스냅샷만으로 HP/MP 숫자·MISS/공격/사망을 만들지 않는다. 원본 숫자 단일 슬롯·양쪽 공격 슬롯·40 숫자 플래그에 따른 사망 보류·퇴장/점유 해제는 런타임에서 처리한다. received HP/MP와 선택적 `nativeAppearanceJob`은 승인 시나리오 상태로 저장하고, 숫자/공격/사망/SFX의 중간 카운터는 재접속에 재생하지 않는다. 외형 키는 해석된 runtime+68이며 직업/병종/성장값과 분리한다. 누락null은 이전 클래스 그림 표시를 유지하며 미수신 외형 상태를 확정하지 않는다.

`Art/Combat/AttackMarker.png`는 원본 mcursor.bmp 픽셀의 RGB 검정 투명키 변환이다. `build_feedback_assets.py`가 바인딩/59프레임 근거를 재생성하며 world 재생성도 이 단계를 호출한다. 효과/몸체 원본 사각형이 실제 변환 시트를 벗어나면 다른 그림이나 끝 행 늘이기로 대체하지 않는다. 32/64픽셀 몸체·대기333/이동125clock 간격은 원작 코드에서 대조하며,40ms 효과 환산과 실제 원작 전체 재생 속도의 실측을 구분한다.

이번 피드백/사망/몸체/송신 builder의 실제 x86 비교 도구는 `check_native_feedback.py`, `check_native_death.py`, `check_native_feedback_frames.py`, `check_native_body.py`, `check_native_outbound_packets.py`, `check_native_battle_decoder.py`다. 원본 PE를 읽기 전용으로 실행하고 CString·할당·DirectDraw/음향/송신 경계만 격리한다. 합성 테스트 입력을 운영 데이터로 복사하지 않는다. 자세한 한계/재검증은 테스트 계획 최신 절을 따른다.



용병 직접/자동 입력은 기존 `TrainingUnit`에 명시 `attackTargets`(로컬 권위자가 정한 공격 허용ID), 선택적 `nativeActive/nativeEnabled/nativeMenuMask`를 보존한다. 원본 ally3의 미확보 관계를 ID만 보고 적대로 만들지 않으며 +0/+4 플래그를 HP와 동일시하지 않는다. 소유/능력 권한과 메뉴 허용은 별개다. 클라이언트 AI 준비 플래그/1FC 휴식 힌트의 패킷 없는 변화는 런타임에만 반영하고 HP/AP/좌표/저장을 예측 변경하지 않는다. 실제 승인 결과만 scenarioBattle에 저장한다.

`BattleTickNotice.packet`은 원본5C04(2바이트)와5C52(8바이트)의 완전한 응용 패킷만 받는다. signed WORD AP는 절대값으로0~수신 maxAP에 제한하고 메뉴 unsigned WORD는0/null을 구별한다. 전송 프레이밍/접속 인증은 이 파서의 범위가 아니다. notice 세션/생애/리비전/ID는 로컬 어댑터 문맥으로 wire 필드를 추가한 것이 아니다. 활성/유효 플래그가 미확인인 전역 증가는 보류하며 원본3000슬롯과 기존 프로필0~999 한도를 구분한다. AP 주기/초기값/최댓값을 생성하지 않는다.

`check_native_soldier_ai.py`/`check_native_battle_ticks.py`는 원본 DLL을 수정하지 않고 격리 x86 실행으로 판단/카운터를 대조한다. 비교용 인물/능력은 테스트 입력이며 운영 편성/배치 데이터가 아니다. `requirements-native-emulation.txt`의 Unicorn은 도구 의존성이며 게임 Assets에 포함하지 않는다. 상세 구현 경계와 실제 Play 순서는 테스트 계획의2026-11-02 최신 절을 따른다.



`Combat/OriginalCombat.json` version3의 `rawMovementType`은 원표542직업의 병종0~9 원값이다. 선택적 수신 `TrainingUnit.movementType`이 우선이며 구형 카탈로그에서 누락된 값을 보병으로 추정하지 않는다. 지휘범위 밖 순수 복귀는 원본 SetArea2/4의 남은 이동 점수·병종 통행과 ID/50 그룹 통과를 사용한다. 최근접 빈 칸 동률은 남은 점수 최소(<99)·X/Y 순서이며 원본 도착지 선택을 임의 균등 배치로 바꾸지 않는다. 프로필 scenarioBattle에는 승인된 용병 좌표/명시 AP만 보존하고, 요청의 source/movePath와 중간 motions는 재접속에 재실행하지 않는다. 로컬 매니저의 자동 펌프도 기존 명시 권위자 승인 없이 이동/자원 변경을 만들지 않는다. 적 탐색/공격과 안쪽 방어 타이머는 별도 분기이며 `regroup-native-evidence.json`/`check_regroup.py`로 이번 순수 복귀 경계를 검증한다.



승인 전투 유닛의 `commandRange`는 원본 snapshot+44→runtime+4C의 **최종 수신 지휘범위**다. 미수신null과 명시0/음수를 구분하며 시나리오 저장에 그대로 보존한다. `TrainingView.CommandRanges`와 요청의 `commandRanges`는 부대ID/10×10·생애·맨해튼 거리·안팎·밖 복귀 조건 문맥의 파생 사본이다. 범위 밖 모드2/4/8의 탐색 반경은 최소2지만 공격 전체를 차단하거나 자동 이동/AI를 실행하지 않는다. `Combat/OriginalCombat.json` version3의 rawCommandRange는 원표542직업의 지범, commandRangeItems는 지휘범위 비영9품목의 원값이다. 두 자료를 미확정 최종 장비 합산식으로 사용하지 않고 직업 원값-1도 임의 상속하지 않는다. 원본 호버 외곽선은 기존 Board.png의 경계2조각을 재사용한다. `command-range-native-evidence.json`/`check_command_range.py`로 근거와 계약을 검증한다.


승인 전투 유닛의 `canCommand/troopOrder`는 명시 지휘 권한/선택적 원본 내부 모드다. 메뉴 선택1/2/3은 직접 명령/자동 공격/자동 방어이고 저장값은1/2/8이다. 내부4는 의미를 임의 명명하지 않고 수신 보존만 하며 기본값을 생성하지 않는다. `TroopOrder` 요청의 commandMembers는 알려진 commander+1~+9의 ID/생애 문맥이며 원본 추종/AI 상태의 유효성을 추정한 실행 목록이 아니다. 기존 Unit8 마법의0~8과 구분한다. 시나리오에서는 기존 scenarioBattle 저장에 보존하고 수련장은 종전 중단 복귀 정책을 따른다. wire opcode/AI 비용/탐색 규칙은 생성하지 않았다. 원본 메뉴/모드·실제 좌표·문자열60개 근거는 `Tools/OriginalWorldChecks/troop-order-native-evidence.json`, 실행 검사는 `check_troop_orders.py`다.


`Combat/BattleItems.json`은 원본 장착위7·양수 마법번33개와 원표 내구를 보존한다. `Tools/OriginalWorldChecks/build_battle_items.py`로 재생성한다. 내구는 메타데이터이며 실제 사용 가능 횟수/소모량을 생성하지 않는다. 원본 item+30→공통 대상 선택 경로는 `battle-item-native-evidence.json`의 초기/후기20개 fixture로 대조한다.0x5FF 표시를 원작 wire opcode로 해석하지 않는다.

전투 도구는 `UseItem`·소지품 개체ID를 사용하고 `itemRules`의 명시 MP/AP·권한/대상·선택적 menuSlot으로 연결한다. 소유 마법/특수기 및 F3 준비와 별개다. 응답의 magicTargets/itemConsumed·승인 유닛 상태와 선택적 itemRemainingUses를 함께 저장한다. `WorldOwnedItem.remainingUses`는 미확인null/명시0을 구분하는 로컬 승인 상태이며 원표 내구나 소지 수량과 같은 wire 필드라는 뜻이 아니다. 새 개체/기존 저장에 소급 초기화하지 않고 자동1차감/회복/소모 정책을 만들지 않는다. 기존 비전투 귀환석 계약은 유지한다.


`Combat/MagicProjectiles.json`의 `magic`은 일반289개, `skills`는 원본 특수 메뉴86개다. 높은ID151개 중 고용50·감정15는 전투 시전에 포함하지 않는다. 원본 GameCtl의 메뉴 구간 `[10000,20000)`/`[30000,32000)`을 따른다. 원문 CSV440행은 삭제/재분류해서 덮어쓰지 않는다. 전투 특수기는 `growth.skills`로 소유를 검사하고 F3의 전투 마법 준비와 별개로 `UseSkill`을 요청한다. 비용/권한은 기존 유닛 `magicRules`, 결과는 명시 `magicTargets`와 승인 유닛 스냅샷을 사용한다. 로컬 매니저의 `Request_BattleSkill/Request_TrainingSkill` 및 좌표형을 추가했으며 기존 Notify_BattleUnits/Notify_TrainingUnits로 승인한다. 실제 wire opcode나 서버 산식을 확정한 것이 아니다.

변환은 `build_magic_projectiles.py`가 일반 영역 생성 뒤 `build_special_skills.py`를 호출해375개 지원 정의와 각 패턴을 합친다. 특수기 자산 감사는 `Tools/OriginalWorldChecks/special-skill-assets.json`(241개 영역), 원본 분기/메뉴 바이트는 `special-skill-native-evidence.json`이다. 별도 원본 이미지 복사 없이 기존 Fx/Fx2와20개 음향을 재사용한다. 일반/특수 감사에는 공통 패턴 참조가 겹치므로 수를 합쳐 고유 그림 수로 세지 않는다. 임의 싱글판 광선/피해/강화 공식은 복구하지 않았다. 상세 계약과 실제 Play 확인 항목은 테스트 계획의 최신 특수기 절을 따른다.


Tables는 후기 분석의 unity_catalogs CSV8개를 바이트 단위로 보존했다. 열 이름·중복·미확정 값의 설명은 분석 문서를 따른다. Placements는 원문 SET/POS610유닛과 행 근거의 참조이며 스폰 실행기나 최종 서버 능력치가 아니다. 임의 편성·보상·해금·초기 자금·판매가·NPC 위치는 포함하지 않는다.

CSV의 category/itemCategory 분류열과 experience_progression의 영문 필드명은 분석 편의를 위한 표기다. 원작이 이 이름의 필드를 전송/저장했다는 뜻이 아니다. 값은 원본 ATR 열과 대조했으며 누락된 선택 토큰의 빈 문자열을 기본 기술/보상으로 해석하지 않는다.

승인 전투 유닛의 `magicRules`는 마법별 enabled·명시 MP/AP비용·허용 대상ID·선택적 사망 대상 허용값이다. 일반 마법 영역(ID10000 미만)289개 전체를 지원하며 실제 전투 특수기는86개이며 위의 별도 소유/행동 경로를 따른다. 최종 비용/피해·회복량/강화 배율·상태 지속시간은 생성하지 않는다. 누락과 명시0을 구분한다. 아군/자신도 명시 대상에 포함할 수 있고 Unit8은 양수 range보다 우선한다. `magicRestoreMp`는 비용 차감 뒤의 명시 MP복원, `revivedTargets`는 실제 부활ID, `magicEvents`는 몬스터/아이템/이벤트의 source/life·좌표·대상 표시 통지다. `nativeState/effectiveStats`와 행동 허용값은 권위자의 최종 스냅샷이다. 이 로컬 계약을 원작 wire opcode/서버 산식으로 해석하지 않는다.

`Combat/MagicProjectiles.json`은 후기 magicptn.atr의 원본 Fx/Fx2 표면·사각형·프레임/원본 토큰·음향ID와289개의 pattern/team/unit/distance/range를 보존한다. `Tools/OriginalWorldChecks/build_magic_projectiles.py`로 재생성한다.409개 패턴 프레임과 레벨업 마법진12조각을 픽셀 감사한다. type0의6개 턴언데드 및 후기 패턴64/65 미존재6개는 원본 기본 정리 경로로 그림을 만들지 않는다. 미존재 패턴에 과거 그림을 대입하지 않는다. 원본 카운터·절삭·크기 전환을 기존40ms 로컬 표시 단계에 매핑했으며 실제 원작 속도는 실측하지 않았다. 표시 RNG는 원작의 전체 공유 난수 상태 복원이 아니다.

`Art/Combat/MagicSounds`의20개 PCM16 WAV는 원본 sound.arc의 모든 샘플·주파수·채널·길이를 보존한다. `Tools/OriginalWorldChecks/build_magic_audio.py`로 재생성하며 `NativeWorldArt`에 ID/AudioClip을 연결한다. sound1/sound2의 양수ID만 재생하고0/음수에 대체음을 넣지 않는다. 같은 ID의 비반복 재시작·비행음 중단/명중·지연 타격·좌표 최종0 재시작·맵/수명 정리를 연결했다. 음향 샘플 감사는 청음/실제 Unity 재생 통과를 뜻하지 않는다.

광역 시전의 `TrainingActionRequest.magicTargets`는 맵 내부 양수 마름모와 명시 허용 대상 및 생존 조건(명시 부활 허용 시 사망 포함)의 교집합에서 X외측/Y내측으로 캡처한ID·생애·좌표·사망 여부다. `TrainingUnitsNotice.magicTargets`는 명시 실제 적용ID 부분집합이며 누락과 빈 배열을 구분한다. 후보/범위 가중치로 피해를 추정하지 않는다. 후보/적용 목록은 임시이며 확정 전체 유닛/자원만 기존scenarioBattle에 함께 저장한다. `Request_BattleMagicAt/Request_TrainingMagicAt`와 새 필드는 로컬 계약이며 원작 wire 형식이 아니다.

`BattleMagicEffectRole`은 저장하지 않는 순수 표시 역할이다. 썬더스톰은 대상 전용 효과와 시전자 충전1개를 분리하며, 명시0명 결과도 원본 Magic의 대상 수 검사 전 설정 순서에 따라 충전만 표시한다. 승인 저장 성공 뒤 최신 지휘관 충전과 실제 적용 대상ID의 이전 효과를 교체하고 관계없는 대상은 유지한다. 저장 실패는 기존 그림도 보존한다. 대상/충전 시계와 표시용 targetUnit은 프로필/원작 wire 필드가 아니다.

윈드커터 Unit8은 `groupMagic`의10개 원본ID만 지원한다. 클릭 유닛ID/10*10 기준0~8 슬롯 중 알려진 생존/명시 허용 적을 ID 순서로 캡처하고 요청XY는(-1,-1), `magicUnit`은8로 기록한다. 부대원별 사거리/광역 마스크를 다시 적용하지 않는다. 후보가0명인 원본 성공 경로도 실제 명시 빈 승인/자원이 있어야 반영한다. 실제 적용 목록·개별 좌표는 임시이며 원작 +0/+4 유효 플래그/전체 팀 관계를 HP로 추측한 데이터가 아니다. 다른 Unit8 기술/아군 마법에 이 규칙을 자동 적용하지 않는다.

토네이도는 패턴8/type3의10종만 지원하며 기존 광역 후보/명시 적용 결과를 사용한다. 타격은 개별 고정 좌표, 좌표 효과는 요청 클릭 중심이다. `BattleMagicPresentation`은 현재 대상ID 순서에서 같은 중심을 중복 등록하지 않고 원본modulo15 커서/포함 렌더 범위를 보존한다. 갱신 후1~9 좌표 표시·10 공백·대상 정리로0을 읽는 마지막 좌표 패스·다음 패스 제거를 구분한다. 이 목록/좌표/RNG/시계는 임시이고 저장하지 않는다. 실제 원작 재생 속도/음향/공유 난수의 측정값이 아니다.

프로필의 `magicLoadout`은 승인된 전투 사용 순서(최대8개)·요청/통지/조건ID·revision·당시 job/growthRevision이다. 습득 소유는 기존 growth.magic에 남으며 이 목록 편집으로 지급/삭제하지 않는다. 당시 기록과 현재 직업/소유가 맞을 때만 활성 battleMagic으로 노출한다. 비용/임시16칸 습득 표시·전투 선택·조회 조건은 재접속에 추정 복원하지 않는다. 능력치도 편집한 확인은 statAllocation과 함께 한 번에 저장한다. `Combat/MagicMenu.json`은 Magictbl.csv의440행 이름/원본 아이콘 픽셀 좌표·패턴/유닛·표시 정보이며 `Tools/OriginalWorldChecks/build_magic_menu.py`로 재생성한다. 초기 선택·시전 판정/비용·미확정84 WORD·새 원작 wire는 만들지 않는다.

프로필의 `statAllocation`은 최종 승인된6개 원시 능력치3수치(장비 전/후/한도)·남은 배분 포인트·요청/통지/조건ID·revision·당시 job/growthRevision 기록이다. 임시 목록/항목별 비용은 저장하지 않으며 전직 뒤 현재 능력치로 자동 환산하지 않는다. generic growth.points와 별개이며 전투 수치는 승인 통지의 명시 effectiveStats/health가 있을 때만 함께 반영한다. 기본 비용/포인트·능력 증가 산식은 생성하지 않는다. native 포인트/수치WORD의 signed16 범위를 보존하되 감김은 거부한다. 이는 원작 wire/실제 서버 배분 정책 전체의 복원이 아니다.

프로필의 `classChange`는 승인된 전직의 요청/통지·후보 세트ID·성장 revision·이전/결과 직업·선택 slot/index 영수증이다. 전체 성장 결과와 함께 저장하고 후보/조회/변경 대기는 복원하지 않는다. 후보·전직 포인트는 별도 권위자의 명시 값이며 levelptl의 nextJob이나 growth.points와 자동으로 동일시하지 않는다. `World/ClassAdjustment.json`과 Art/World의 ClassAdjustment 그림2개는 원본 Wlib/scene23/m의 틀/21버튼·최소화 그림이다. `Tools/OriginalWorldChecks/build_class_assets.py`로 재생성한다. 로컬 계약은 원작 wire/전직 자격·비용 공식이 아니다. 능력치 배분과 전투 마법 목록 편집은 별도 statAllocation/magicLoadout 계약을 따른다.

프로필의 `itemUse`는 원본324/361 귀환석 사용의 요청/통지·revision·소유 ID·출발/도착 맵·좌표/방향·명시 소비 여부 영수증이다. 위치/물품과 함께 저장하고 대기는 복원하지 않는다. 기본 목적지·소비 정책을 생성하지 않으며 일반 월드에서 승인된 배치만 반영한다. 장착위10 전체를 귀환/회복 효과로 취급하지 않고 회복약(장착위7)·공간이동석·가구 등의 운영값/효과는 포함하지 않는다. 이 로컬 계약은 원작 wire opcode나 전체 귀환 허용 규칙의 복원이 아니다.

프로필의 `shop`은 승인된 구매/판매의 요청·통지·가격 확인ID·revision·소유 개체/품목/슬롯·명시 가격 영수증이다. 물품/소지금과 함께 저장하고 조회 가격/대기는 복원하지 않는다. 구매는 기존 원본 재고/가격, 미확정 판매는 서버가 명시한 확인 가격을 사용하며 전역 sellKnown/sellPrice를 생성하거나 수정하지 않는다. 무료 거래에도 소유 변경 버전을 보존한다. 이 로컬 계약은 원작 wire opcode/거래 순서나 판매 공식의 복원이 아니다.

프로필의 `mercenary`는 승인된 고용/해고의 요청·통지·revision·병종·결과 인원·명시 차감/환급액 영수증이다. 기존 hiredJob/hiredCount·소지금과 함께 저장하고 미승인 대기는 저장하지 않는다. 고용은 기존 원본 변환 목록/가격/한도를 사용하며 해고 환급은 명시 값이 있어야 한다. 로컬 승인 계약을 원작 wire나 서버 운영 정책으로 취급하지 않으며, 실제 개별 용병 전투 배치/능력치 데이터를 생성한 것이 아니다.

프로필의 `warehouse`는 창고 최종 확인의 요청/통지·revision·임시 목록 버전·승인 한도·명시 요금/대상 영수증이다. 확정 소지품/보관품과 함께 저장하며 임시 이동·조회 조건은 재접속에 복원하지 않는다. 원본 창고의 보이는 12칸을 전체 한도로 해석하지 않으며 기본 보관 한도/요금은 설정하지 않는다. 이 로컬 계약을 원작 wire opcode나 계정 공유 규칙으로 취급하지 않는다.

프로필의 `lastBankTransfer`는 승인된 은행 거래의요청/통지·방향·금액·명시 수수료/대상과공통 금전 버전을 보존한다. 데이터는 원본 가격표/은행 한도나 wire opcode가 아니다. 대기 거래는 저장하지 않으며 기본 은행 정책/수수료를 생성하지 않는다. 원작 Wlib/scene18을 재사용하는 기존 입출금 UI에서 승인 결과만 반영한다.

프로필의 `currency`는 금전 통지/잔액 변경의로컬 영수증·revision이다. 금액은 기존gold/bank와각known 플래그에 보존한다. 수신null은 기존 상태 유지, 명시0은 알려진0이며 초기 자금/보상으로 더하지 않는다. 모든 실제 금전 변경이 같은 버전을 갱신하되 원작 wire 폭/상한/opcode로 해석하지 않는다. 가격·운영 수수료·초기 자금 데이터는 생성하지 않았다.

신전의 승인된 복구는 프로필의 `revival`에 영수증 식별자·revision·명시 비용을 저장한다. 조회 가격/승인 전 요청은 저장하지 않는다. 비용·HP/MP·잔액은 로컬 권위자가 명시한 값만 검증해 반영하며 기본 무료 부활/가격/회복량은 설정하지 않는다. 기존 공통 금액 창을 재사용하는 로컬 연결이며 원작 신전 전용 UI나 wire opcode 복원으로 취급하지 않는다.

프로필의 `lastScenarioExit`는 승인된 비승리 종료의식별 기록이다. 결과outcome=defeat/retreat는 표시 구분이며 페널티/보상 명령이 아니다. 알려진HP0인 일반 시나리오 복귀에서만 사용자 확정 유령 규칙을 적용한다. 원작 서버 판정으로 재분류하거나 기존HP0 저장을 소급 변환하지 않는다. 신전 비용/회복량은 기존 운영 설정에서 계속 보류한다.

시나리오의 실제 승인 전투 상태는 원본 배치 파일과 분리해 프로필의 `scenarioBattle`에 저장한다. 원본 SET/POS와 `WorldView.units`는 참고 목록을 유지하고, `WorldView.battle`이 실제 HP/AP/좌표를 담는다. 원본26의65개 중 보병 이동 불가25칸의 적도 수신 좌표를 그대로 보존한다. 전투 상태/피해/AP를 원본 배치 JSON에 덧쓰거나 HP/AP 검사 fixture를 운영 데이터로 쓰지 않는다.

`World/ScenarioResult.json`과 Art/World의ScenarioResult 그림은 원본Glib/gscene8·gamelib.lce/ltn을 재조립한434×344 결과 틀/확인3상태다. 실제 원작 사진의 경험치·처치 요약을 참고하며, 일반 보고를 gscene17 클래스업 창으로 바꾸지 않는다. `Tools/OriginalWorldChecks/build_result_assets.py`로 재생성한다. 실제 보고 값은 승인 통지에서 받고 표시만 하며 보상/XP 계산표를 생성하지 않는다. 원래 진입 위치와 확인 전 보고는 프로필의scenarioVisit에, 확인 후 최근 보고는lastScenarioResult에 저장한다.

`Combat/OriginalCombat.json`은 원본 542직업의 그림·이름·기본 획득경·공거리/행동틱·지휘범위 원값과 지휘범위 비영 장비9종이다. `Tools/OriginalWorldChecks/build_combat_assets.py`로 재생성한다. 수련장 초기 편성/스폰 좌표·최종 피해/AP 비용·XP 분배·AI/리스폰 공식은 포함하지 않는다. 로컬 전투 요청/통지는 원작 wire opcode를 확정한 자료가 아니다. 기본 권위자는 설정하지 않았으며 수련장 검사의 합성 fixture를 게임 데이터로 사용하지 않는다.

`Growth/CharacterGrowth.json`은 현행levelptl의73직업과levelabl의2781원문 행(동일 중복을 합친2780키)을 참조한다. 요구 경험치·다음 직업·표 포인트·마법/기술·원문 행을 보존하며 경험치 소모/누적, 능력치 증가, 자동 전직 또는 보상 지급 공식으로 사용하지 않는다. 직업166의 원문 끝 쉼표도rawSkills에 보존한다. `Tools/OriginalWorldChecks/build_growth_assets.py`로 재생성한다. World 아이템의jobKinds는 원본10개 병종 허용 열이며, 전직 후 장비 자격을 초기3직업 목록으로만 판정하지 않도록 연결했다.

GetFace용초상은face.arc→face1.arc→face2.arc→face3.arc의최초항목을사용한다. 확보된140개는기존64×64 PNG/GUID를그대로연결하고무압축으로가져온다. 모든아카이브에항목이없을때만etc.arc/noface.bmp를사용하며명시된그림의디코드실패는대체하지않는다. `build_native_face_assets.py`는초상참조만갱신하며맵·입구·배치를재생성하지않는다.

`WorldPrivateChat.json/png`는원본scene47/창80이다. 현재접속의승인된해석메시지와명시nativeSuppression/RoomNumber로만이름별창을연다. 수신12바이트이름과공백/대소문자를보존하고,원문7A09답장·7D선행요청·모드2전처리·300줄/6행·원본스크롤/닫기를연결한다. MRC→RoomNumber추정·자동반향·우편함/플레이어저장은없다. 원작422500에서표식이미확인일때남은스택값에의존하는색은어댑터가청록으로고정한다. 실제IME/8비트팔레트/전체화면·배달동일성은별도검증대상이다.


대기실 `Tutorial/Waiting1500.json`의 전체/최소화 제목줄 영역·크기는 원본8개 LSC를 직접 읽은 값이다. `items`의1/762/777은 CSV 이름 표시용이며 아이템 지급·사용횟수·소지금 데이터를 새로 만들지 않는다. 부분 갱신 도구는 `Tools/OriginalTutorialChecks/update_waiting_window_assets.py`이며 기존 맵·안내문·버튼·최초 표시 좌표를 보존한다.
