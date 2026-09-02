# 123일차 : 5개 층 회차 루프 완성 및 로비 씬 신설

## 목표
- 5개 층 회차 루프 완성: 1층/5층 테마 고정, 2~4층 테마 셔플, 층 이동 시 회복, 이전 층 복귀 방지, 보스 처치 후에만 계단 개방 확인
- (추가 요청) 던전을 시작하는 버튼과 던전 클리어 후 돌아오는 버튼 신설 — 로비 씬 도입

## 구현 내용

### 1. 층 테마 스케줄
- `FloorTheme` (Domain): 동굴/폐허/숲/무덤/마왕성 5종 테마 열거형과 한글 표시명
- `FloorThemeSchedule` (Domain): 1층은 항상 동굴, 5층은 항상 마왕성, 2~4층은 폐허·숲·무덤을 `baseSeed` 기반 결정적 셔플로 배치. 저장 데이터 없이도 같은 시드면 항상 같은 순서로 재현됨

### 2. 층 이동 시 회복 및 마지막 층 계단 차단
- `DungeonFloorController.RecoverPlayerOnFloorChange()`: 층 이동(신규 생성/기존 경로 모두) 시 HP/마나/스태미나를 최대치로 회복 — 기획서 3.6.2 "다음 회복 시점은 층 이동뿐이다" 반영
- `TryDescend`: 5층(마지막 층)에서는 계단 사용 자체를 차단
- `CurrentFloorNumber`/`CurrentFloorTheme` 프로퍼티로 다른 컨트롤러가 층/테마 정보를 조회 가능

### 3. "이전 층으로 돌아가지 않는다" 확인
- `StairsInteractionController`: 계단 앞에서 상호작용 키를 한 번 누르면 확인 문구를 표시하고, 다시 누르면 실제로 하강. Esc나 계단 이탈 시 확인 취소

### 4. 층/테마 HUD 표시
- `RoomStatusHudController`: 기존 방 종류 표시 앞에 "N층 (테마명) /" 형태로 현재 층과 테마를 함께 표시

### 5. 로비 씬 신설
- `LobbyScene.unity` 신규 제작 (TitleScene 구조를 그대로 복제), `EditorBuildSettings.asset`에 등록
- `LobbySceneController`: "던전 입장"(새 런 시작) / "타이틀로" 버튼을 가진 임시 로비 화면
- `TitleSceneController`의 "새 게임" 버튼은 이제 던전이 아닌 로비로 이동
- `DungeonLobbyReturnHudController` + `DungeonLobbyReturnHudInstaller`: 던전 진입 중 화면 우상단에 항상 떠 있는 "로비로" 버튼. 아직 5층 마왕(124일차)이 없어 지금은 언제든 나갈 수 있는 일반적인 나가기 버튼으로 동작하며, 124일차에서 마왕 처치 시 자동 호출로 연결할 예정
- `ApplicationFlow`: `EnterLobby()`/`ReturnToLobby()` 추가 — `ReturnToLobby()`는 기존 `ReturnToTitle()`과 동일한 런 종료/저장 삭제/체크포인트 정리 로직을 그대로 사용하고 목적지만 로비로 변경

## 확인한 기존 규칙
- 보스방 계단은 몬스터 처치(`MonsterDefeated`) 시에만 개방되는 기존 규칙(121~122일차)이 그대로 유지됨을 재확인
- 층별 던전 배치(방/몬스터 배치)는 매 층 진입 시 새로 생성되고, 프로필(영구 성장)은 런과 무관하게 유지되는 기존 데이터 계층 구조를 그대로 따름 — 이번 일차에서 별도 변경 없음
