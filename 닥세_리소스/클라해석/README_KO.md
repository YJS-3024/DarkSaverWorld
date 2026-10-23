# 클라해석 — 범용 로컬 구조 분석 도구

이 패키지는 특정 게임을 복원하는 서버팩이 아닙니다. 권한이 있는 파일을 사용자가 자기 PC에서 읽고, 필요한 구조 정보부터 작은 범위로 확인하기 위한 Python 표준 라이브러리 도구입니다. 기존 프로젝트의 코드를 옮긴 것이 아니라 공개용으로 새로 작성했습니다.

원본 클라이언트, 그림·음악·대사, 추출 테이블, 특정 게임의 포맷 정의·주소·패킷, 서버 구현, 패치, 자동전투 AI, 에디터는 포함하지 않습니다. AI_GUIDE_KO.md는 코덱스·클로드에 분석 작업을 설명하는 문서일 뿐 게임 AI 기능이 아닙니다.

## 먼저 읽기

LEGAL_KO.md를 먼저 확인하세요. 이 도구나 출력물에 대해 법적 안전을 보증하지 않습니다. 적법한 보유만으로 모든 분석·복제·공유가 허용되는 것은 아닙니다. 생성한 보고서도 자동으로 공개 가능한 자료가 되지 않습니다.

## 시작

Python 3.10 이상, 외부 패키지 설치 불필요. 명령은 압축을 푼 폴더의 터미널에서 실행합니다. 아래 경로는 사용자가 자기 경로로 바꿀 예시입니다. 입력은 실행 중에 변경되지 않는 권한 있는 사본을 권장합니다. 암호화·보호 조치가 있으면 중단하고 권한을 확인하세요.

```powershell
python -B -m unittest -v test_inspect_client
python -B inspect_client.py scan "D:\MyAuthorizedInput" --out "D:\MyPrivateReport"
python -B inspect_client.py probe "D:\MyAuthorizedInput\sample.dat"
```

`scan`은 파일 크기·상대경로·SHA-256 목록과 짧은 BRIEF.md를 새 폴더에 만듭니다. 원본 내용은 복사하지 않습니다. 입력 폴더나 이 도구 폴더 안에는 보고서를 만들지 않으며 기존 출력 폴더를 덮어쓰지 않습니다. 일부 저장·설정·로그 파일은 건너뛰지만 완전한 개인정보 필터가 아닙니다. 파일명과 해시도 비공개 정보일 수 있습니다.

## 필요한 기능만 사용

```powershell
python -B inspect_client.py pe "D:\MyAuthorizedInput\sample.exe"
python -B inspect_client.py table "D:\MyAuthorizedInput\sample.txt" --encoding cp949 --delimiter tab
python -B inspect_client.py records "D:\MyAuthorizedInput\sample.dat" --offset 0 --format "<2H" --count 4
python -B inspect_client.py grid "D:\MyAuthorizedInput\sample.dat" --width 8 --height 8 --stride 1 --blocked 1
python -B inspect_client.py compare "D:\ReportBefore\inventory.json" "D:\ReportAfter\inventory.json"
```

- `probe`: 최대 앞 64 KiB 표본의 엔트로피·바이트 비율과 크기 단서. 압축 여부나 레코드 길이를 확정하지 않습니다.
- `pe`: 공개 PE 규격에 따른 헤더·섹션의 수치 정보만 출력. 실행·디스어셈블·리소스 추출을 하지 않습니다.
- `table`: 문자 셀을 출력하지 않고 행·열 수와 정수 통계만 확인. 일반 CSV의 따옴표/이스케이프 처리는 지원하지 않습니다.
- `records`: 사용자가 지정한 가설대로 최대 64개 숫자 레코드만 읽습니다. `<2H`는 리틀엔디언 16비트 부호 없는 정수 2개이며 실제 파일 규격을 뜻하지 않습니다. 출력 숫자도 원본에서 유래하므로 공개하지 마세요.
- `grid`: 사용자가 폭·높이·간격·막힘 값을 이미 확인한 바이트 격자에 대해 4방향 연결성 통계만 계산합니다. 게임 맵을 자동 해석하거나 그림으로 추출하지 않습니다.
- `compare`: 두 로컬 목록의 추가·삭제·내용 변경 경로를 비교합니다.

단일 파일 구조 분석은 64 MiB 이하로 제한됩니다. 목록 해시는 스트리밍으로 계산하며 파일 수는 20,000개로 제한됩니다. 심볼릭 링크와 Windows reparse point는 거부/제외합니다. 동시 파일 교체 공격에 대한 보안 샌드박스는 아니므로 신뢰할 수 있는 로컬 폴더에서만 사용하세요. 실행파일을 실행하거나 원본을 수정하지 않습니다.

## 배포

공개하려면 생성된 `클라해석.zip`만 선택하세요. 클라이언트, 보고서, 기존 작업 폴더를 함께 압축하지 마세요. 재패키징은 `python -B build_release.py --out "D:\PublicOutput\클라해석.zip"`처럼 새 경로를 사용합니다. 코드나 문서를 바꿨다면 별도 검토가 필요하며 검사 통과는 법적 승인과 다릅니다.

게시글 예시: “권한 있는 로컬 파일의 구조 확인을 돕는 범용 Python 도구입니다. 게임 데이터·실행파일·서버·자동전투 AI·에디터는 포함하지 않습니다. 대상별 분석 권한과 결과 공유 가능 여부는 별도 확인이 필요합니다.”
