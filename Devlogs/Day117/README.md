# Project Delta - 117일차 개발일지

## 작업 개요

116일차에서 "다음 과제"로 남겨뒀던 부분 - 유혇 성공 시 곧바로 평화롭게 끝내는 대신, 기획서가 요구한 **별도 이벤트 전투**로 넘어가게 만드는 날이다. 일반 전투(`BattleContext`)와 상호 배타적인 전용 Context를 새로 만들고, 하나의 진입구(Entry API)로 여러 경로가 들어올 수 있게 설계했다.

핵심은 세 가지다.

1. 일반 전투와 완전히 분리된 이벤트 전투 구조(Context·Session·Command·Entry API)를 만든다.
2. 실제로 쓰는 콘텐츠(구애·달래기 2개 행동, 호감도 게이지)를 붙인다.
3. 116일차의 유혇 성공 분기를 이 새 시스템으로 연결한다.

---

## Part 1. 별도 이벤트 전투 구조

`Assets/ProjectDelta/Scripts/Application/EventBattleContext.cs`, `EventBattleSession.cs`, `EventBattleState.cs`, `EventBattleOutcome.cs`, `EventBattleResult.cs`, `EventBattleEntrySource.cs`, `EventBattleEntryService.cs`

- `EventBattleContext` - 새 참가자 타입을 또 만들지 않고 기존 `BattleParticipant`(47일차)를 그대로 재사용했다. 이미 정력·마나·매력·저항이 다 있어서, 이 전투에서만 의미 있는 값(호감도 0~100, 시도 횟수)만 추가로 담았다.
- `EventBattleSession` - 기존 `BattleSession`과 같은 역할이지만 라운드·행동자 순서 개념이 없는 1:1 진행이라 Idle/Active/Finished 셋만으로 충분했다.
- `EventBattleEntrySource` - 유혇 성공(Seduction) 외에 스킬/몬스터 행동 전환·일반 이벤트·보스전 자리도 enum에 미리 만들어뒀다. 아직 그런 콘텐츠가 없어 실제로 연결한 건 유혇 하나뿐이다 - 나머지 셋은 "이 값으로 들어오면 된다"는 자리만 잡아뒀다.
- `EventBattleEntryService.TryEnter(...)` - 기획서가 요구한 "하나의 Entry API". 지금은 위 4갈래 중 하나만 실제로 호출하지만, 나머지도 같은 방식으로 Context를 만들면 되므로 새 콘텐츠가 생겼을 때 이 서비스만 재사용하면 된다.

---

## Part 2. 구애·달래기

`Assets/ProjectDelta/Scripts/Application/IEventBattleCommand.cs`, `EventBattleCommandResult.cs`, `CourtEventBattleCommand.cs`, `SootheEventBattleCommand.cs`

- **구애** - 마나 10 소모, 매력-저항 차이만큼 호감도가 크게 오른다(기본 +12 ± 차이 ± 변동폭). 능력치가 좋을수록 유리하지만 실패하면(마나 부족) 아예 쓸 수 없다.
- **달래기** - 정력 8 소모, 능력치와 무관하게 안정적으로 조금(기본 +6) 오른다.
- 승리 조건은 호감도 100 도달, 패배 조건은 두 행동 다 자원이 부족해 아무것도 할 수 없을 때다. `EventBattleContext.PlayerCanAct(...)`가 이 판정을 맡는다.

---

## Part 3. 화면과 설치

`Assets/ProjectDelta/Scripts/Presentation/EventBattleController.cs`, `EventBattleRuntimeInstaller.cs`

- `EventBattleController` - 호감도 게이지 바 + 구애/달래기/포기 버튼 3개짜리 단순한 IMGUI 화면. 대상 이름은 `DungeonFloorController.TryFindMonsterDefinition`으로 실제 몬스터 정의를 찾아 표시한다.
- 이 컨트롤러는 "이겼을 때 무엇을 할지"를 스스로 모른다 - `Begin(player, target, source, onWon, onLostOrAborted)`로 호출자가 콜백을 넘겨준다. 113~116일차에서 계속 지켜온 관심사 분리 원칙을 그대로 따랐다.
- `EventBattleRuntimeInstaller`가 `NpcRuntimeInstaller`(113일차)와 같은 방식으로 씬을 건드리지 않고 Player에 자동으로 붙인다.

---

## Part 4. 유혇 성공 분기 연결

`Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`

`ConfirmInfluenceAttempt`에 `triggersEventBattle` 플래그를 추가했다 - 회유(`ConfirmPersuade`)는 여전히 성공하면 곧바로 평화롭게 끝나고, 유혇(`ConfirmSeduce`)만 성공 시 이벤트 전투를 연다.

- **이벤트 전투 승리** → 원래 전투를 `FinishBattle(BattleOutcome.Escaped)`로 평화롭게 끝낸다(117일차 이전과 같은 결과).
- **이벤트 전투 패배/포기** → 새로 뽑아낸 `ContinueBattleAfterFailedInfluence(...)`를 호출해 원래 전투 턴으로 그대로 복귀한다 - 회유/유혇 실패 시 쓰던 로직을 그대로 재사용했다(코드 중복 없이 콜백 하나로 연결).
- `EventBattleController`를 찾지 못하면(테스트 씬 등) 117일차 이전 동작(곧바로 평화 종료)으로 자동 폴백한다.

---

## 테스트

- `EventBattleSessionTests` - Idle→Active→Finished 상태 전이, 중복 시작 거부, 결과 저장, 리셋.
- `EventBattleContextTests` - 호감도 clamp(0~100), 시도 횟수 집계, 자원 부족 시 `PlayerCanAct` 판정.
- `CourtSootheEventBattleCommandTests` - 두 행동의 Id/DisplayName/자원 비용, 자원 충분/부족 시 결과, null Context 거부.
- `EventBattleEntryServiceTests` - 정상 진입, null 참가자·죽은 대상 거부.

사용자가 빌드 중 `BattleParticipant` 생성자 위치 인자와 `currentHp` 이름 인자가 겹치는 CS1744 오류를 발견해줘서 `EventBattleEntryServiceTests.cs`의 중복된 인자 하나를 제거해 고쳤다.

씬 UI(호감도 게이지·버튼)는 Unity 에디터가 없는 환경이라 실제 플레이로 확인하지 못했다.
