# 다른 환경에서 원본 클라이언트 검사 실행

`Tools/run_original_checks.py`는 원래 검사 파일과 하위 Python 호출을 그대로 실행한다. assertion/생산 코드를 제거하지 않는다. Linux에 고정된 Unity/임시/추출본 경로만 지정한 위치로 대응하고 C# 응답 파일의 인수를 따옴표로 감싼다. Python 3.10 이상, 해당 Unity SDK, 검사별 Python 의존성이 필요하다. 네이티브 검사는 정확한 버전의 원본 PE 파일도 필요하며 기존 SHA 검사는 유지된다.

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
- `--normalize-evidence-eol`: 생성 분석 JSON인 waiting_tutorial_resources.json 한 파일만 대상으로 한다. CRLF→LF 결과가 기존 선언 SHA256과 정확히 일치할 때만 쓴다. 내용이 다르면 파일을 보존하고 실패한다. 기존 Windows 사본은 새 .gitattributes만 풀받아도 이미 있는 CRLF가 바뀌지 않을 수 있어 이 옵션을 제공한다. 원본 아카이브/SourceData/CSV는 변경하지 않는다.
- `--check`는 여러 번 지정할 수 있고 순서대로 실행한다. 수련장 검사는 기존 성장→월드/캠페인 의존 검사도 실행해 C# 템플릿을 준비한다. 임시 산출물을 직접 읽는 다른 검사는 해당 선행 검사부터 실행해야 한다.
- `--refresh-integration`: 현재 선언 소스가 바뀐 보고서의 생성 검사를 순차 실행하고 다시 의존성을 계산한다. 픽셀 재검사 후 이를 참조하는 네이티브 보고서도 실제 재검사한다. 새 자산/메타가 추가되어 리소스 GUID 목록이 달라진 경우에도 바인딩 검사를 재실행한다. 해시만 덮어쓰지 않는다. 필요한 원본/선행 산출물이 없으면 실패 로그를 보존하며 최종 통과를 만들지 않는다.
- 각 실행 로그와 `run-results.json`은 work-dir에 남긴다. 보고서는 기존 도구대로 저장소의 JSON에 갱신된다. 기존 검증 증거 폴더는 덮어쓰지 않는다.

일부 구형 `_Game` 검사에는 현재 삭제된 자산이 필요하다. 이 실행기는 삭제 자산을 복원하거나 검사를 생략하지 않는다. 현재 `_Original` 검사를 선택하고, 구형 검사 미실행은 별도 기록한다. 실제 Editor Search/AI 패키지 오류, NUnit 어셈블리 부재, 물리 입력·청음 검증은 이 실행기로 해결/대체하지 않는다.
