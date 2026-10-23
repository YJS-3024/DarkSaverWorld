# Unity API 컴파일 확인

01 데모 생성 메뉴까지 컴파일하려면 빌드 명령에 `-p:CheckBrugosEditor=true`를 추가한다. 이 옵션도 실제 씬 생성이나 메뉴 실행은 하지 않는다.

프로젝트 루트에서 .NET 10 SDK로 실행한다. UnityManagedPath에는 해당 프로젝트 버전 Editor/Data/Managed 경로를 전달한다.

```powershell
dotnet restore Tools/UnityCompileChecks/UnityCompileChecks.csproj --configfile Tools/UnityCompileChecks/NuGet.Config
dotnet build Tools/UnityCompileChecks/UnityCompileChecks.csproj --no-restore "-p:UnityManagedPath=C:/Program Files/Unity/Hub/Editor/6000.5.7f1/Editor/Data/Managed"
```

현재 런타임 소스와 설치된 Unity API 어셈블리를 참조하는 보조 컴파일 검증이다. 실행 중인 Unity를 종료하거나 씬을 변경하지 않는다. Unity 자체 컴파일의 플랫폼 정의·런타임 환경 및 에셋 임포트·EditMode/PlayMode 테스트를 완전히 대체하지 않는다. 직렬화 필드의 CS0649 경고는 외부 컴파일러가 Unity 직렬화를 수행하지 않아 나타날 수 있다.
