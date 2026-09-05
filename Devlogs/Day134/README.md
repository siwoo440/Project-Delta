# 134일차 : 도전과제 시스템 및 Steam 연동 지점 구현

## 목표
- 기획서 7.5절 "Steam 도전과제"·7.6절 "엔딩 보상과 콘텐츠 해금" 구현
- 도전과제가 True로 바뀌는 순간 Steam API로 넘길 수 있도록 CG 갤러리처럼 별도 화면과
  연동 지점을 분리해 구성

## 구현 내용

### 1. 도전과제 카탈로그 (Domain)
- `AchievementDefinition` - 도전과제 하나(ID/이름/카테고리/판정 방식/대상/목표값/숨김 여부)
- `AchievementCatalog` - 기획서 100개 구성(엔딩 45 + 패배 기록 31 + 탐험·전투·성장 12 +
  행동 숙련도 12)을 그대로 반영한 로컬 원본 목록
  - 엔딩(45)·패배 기록(31)은 이미 있는 영구 기록(`UnlockedMainEndingIds` 등)의
    고유 개수만 세면 되어 새 게임플레이 훅이 필요 없음
  - 탐험·전투·성장 12개·행동 숙련도 12개는 기획서에 개별 임계값이 없어 임시값으로
    채움(콘텐츠 확정 시 숫자만 교체)

### 2. 판정/기록 (Application)
- `AchievementProgressService.EvaluateAndRecord()` - 프로필의 기존 영구 기록만 읽어
  100개를 판정하고, 처음 True가 된 ID를 `PermanentRecord.UnlockedAchievementIds`에 기록
- `AchievementProgressSnapshot` - 전체/달성/신규 달성 ID 목록을 담아 로비·Steam 동기화가
  공통으로 사용

### 3. 자유 탐험 모드 해금 (7.6절)
- `FreeExplorationUnlockRule` - 주요 엔딩 1개 이상 달성 시 해금
- `LobbySceneController`에서 로비 진입마다 판정하고 상태 표시

### 4. Steam 연동 지점
- `ISteamAchievementBridge`(Application) - 서비스 인터페이스 계층(`ILogService` 등과 같은
  위치)에 계약만 정의 - Infrastructure가 Application을 참조하는 방향이라 반대로 두면
  컴파일 에러가 남(실제로 한 번 겪음: `CS0246 Infrastructure`)
- `NullSteamAchievementBridge`(Infrastructure) - 실제 Steamworks 연동 전까지 로그만 남기는
  기본 구현, `AppRoot`에서 등록
- `ApplicationFlow.SyncSteamAchievements()` - 이번에 새로 True가 된 항목만 브릿지로 전달,
  `LobbySceneController`가 판정 직후 자동 호출

### 5. 도전과제 갤러리 화면 (Canvas/UGUI)
- 전용 씬 `AchievementGalleryScene` + 타이틀 화면 "도전과제" 버튼으로 진입
- 카테고리 탭(전체/엔딩/패배 기록/탐험·전투·성장/행동 숙련도)으로 필터링
- 세로 스크롤 목록 - 숨김 도전과제는 미달성일 때 이름 대신 "???"로 표시, 달성/미달성 상태 표시
- CG 갤러리(133일차)와 동일하게 필요한 UI 오브젝트를 전부 런타임 코드로 생성

### 6. 버그 수정 (사용자 피드백)
- 카테고리 탭 목록과 도전과제 행 목록이 겹쳐 보이던 문제 - 목록 패널을
  `sizeDelta` 계산 대신 `offsetMin`/`offsetMax`로 상하 여백을 명시하고, 헤더
  요소들(요약/탭) 간격도 넓혀서 해결

## 남은 사항
- 100개 중 24개(탐험·전투·성장 12 + 행동 숙련도 12)의 실제 임계값은 임시값 -
  밸런싱 확정 시 `AchievementCatalog`의 숫자만 교체하면 됨
- 실제 Steamworks.NET 패키지 연동 자체는 아직 없음 - `ISteamAchievementBridge` 구현체만
  교체하면 되는 구조로 자리만 만들어 둠
