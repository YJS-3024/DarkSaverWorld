# 전투·이벤트 회귀 검증

프로젝트 루트에서 .NET 10 SDK로 실행한다.

```powershell
dotnet restore Tools/CombatRulesChecks/CombatRulesChecks.csproj --configfile Tools/CombatRulesChecks/NuGet.Config
dotnet run --project Tools/CombatRulesChecks/CombatRulesChecks.csproj --no-restore
```

NuGet 패키지는 사용하지 않는다. 실제 `Assets/_Game/Runtime`의 전투 모델·승리 판정·로컬 판정기·전투 흐름·이벤트 파서를 링크하여 컴파일한다. 코드 복사본이나 원본 게임 파일은 실행에 사용하지 않는다.

현재 기본 24개 검증: 지정 대상/전멸 구분, 누락·아군 목표 거부, 복수 목표, 빈 전투, HP·MP 절대값 적용, 시체 상태, 시작/결과 순서, CHARDEAD 접두값·행 번호, 미검증 숫자 토큰 보존, 명령별 좌표 순서, 장면 배치의 정의 참조/인스턴스 분리·COM0 보존·다른 형식 거부.

분석용으로 DEO 파일 경로 하나를 추가 인수로 전달하면 CP949 원문에서 CHARPOS 형식과 키스라 배치를 2개 추가 검증한다. 이는 Tools의 오프라인 분석 기능이며 게임 런타임은 원본 파일을 읽지 않는다.

이 검증은 Unity 컴포넌트·씬·ScriptableObject 임포트·플레이 검증을 대신하지 않는다. 해당 검증은 Unity EditMode 테스트와 플레이 확인으로 수행한다.
