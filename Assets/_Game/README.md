# DarkSaver 재구축 기반

기존 프로토타입을 제거하고 정적 분석 결과를 기준으로 새로 구성한 첫 실행 단위다.

- `Runtime/Maps`: 원본 MRC 크기 공식과 타일 속성·가림 비트 파서
- `Runtime/Events`: 명령별 좌표 순서를 보존하는 EVT 파서
- `Runtime/Combat`: 원작의 요청/선행 연출/서버 결과 적용 경계를 싱글게임용으로 분리
- `Runtime/Rendering`: 캐릭터·전경 스프라이트·상태 UI 패스 순서와 물 오버레이 규칙
- `StreamingAssets/OriginalData`: 검증용 원본 `01.mrc`

현재 전투 공식은 서버 실행 파일 부재로 임시 로컬 판정기를 사용한다. 데이터·상태 전이 경계는 원작 분석 결과를 따른다.
