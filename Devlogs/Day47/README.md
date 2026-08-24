# Project Delta - 47일차 개발일지

## 개발 목표

46일차까지 완성된 Encounter(전투/회피 선택 → 결과 저장 → 방 완료·복원) 흐름 중간에, 실제 Battle이 진행될 수 있는 독립된 실행 공간을 만든다.

이번 일차의 핵심 목표는 다음과 같다.

- `BattleContext`·전투 참가자 런타임 구조 제작
- `BattleState` 상태 머신으로 Battle 생명주기 관리
- 전투 시작·턴 진행·전투 종료 API(`BattleSession`) 제공
- 46일차 Encounter 결과 처리와 연결 (Battle 승리 → 기존 `EncounterResult.MonsterDefeated` 경로 재사용)
- 전투 화면 Canvas 제작: 적 슬롯 4개(왼쪽부터 1~4번), 플레이어 상태 일러스트, 체력바, 행동 버튼 자리
- 47일차 테스트용 `Test Next Turn / Test Win / Test Lose` 진행 수단 제공

기본 공격, 데미지 공식, 명중률, 방어력, 실제 사망 처리, 적 AI는 이번 일차에 포함하지 않는다.

---

## 구현 내용

### 1. BattleContext와 전투 참가자 구조

`EncounterContext`(어떤 방에서 어떤 몬스터를 만났는가)와 역할을 분리해, `BattleContext`(지금 어떤 참가자들로 전투가 구성되어 있는가)를 추가했다.

```text
BattleContext
├─ Player : BattleParticipant
└─ Enemies : IReadOnlyList<BattleParticipant>
```

`BattleParticipant`는 플레이어와 몬스터를 전투에서 동일하게 다루기 위한 공통 런타임 데이터다.

```text
InstanceId
DefinitionId
Team (Player / Enemy)
MaxHp
CurrentHp
Speed
IsAlive (CurrentHp > 0)
```

적 슬롯은 화면 레이아웃 기준으로 맨 왼쪽이 1번이며, 최대 4명까지 구성할 수 있다.

```text
BattleContext.MaxEnemySlots = 4
BattleContext.TryGetEnemyAtSlot(슬롯 번호) → 해당 슬롯 참가자
BattleContext.TryGetParticipant(InstanceId) → Player·Enemy 통합 조회
```

### 2. BattleState 상태 머신

`ExplorationEncounterSession`과 동일한 패턴(Try* 메서드로만 상태 전환)으로 `BattleSession`을 추가했다.

```text
Idle
↓ TryBeginBattle(context)
Starting
↓ TryStartTurn()
TurnStart
↓ TryEnterAwaitingAction(actor)
AwaitingAction
↓ TryBeginResolveAction()
ResolvingAction
↓ TryEndTurn()
TurnEnd
↓ TryStartTurn()  (다음 턴, TurnNumber 증가)
TurnStart …
↓ TryFinishBattle(outcome)  (Starting 이후 어떤 진행 상태에서도 가능)
Finished
↓ TryReset()
Idle
```

`TurnNumber`는 `TryStartTurn()`이 호출될 때마다 증가하며, `CurrentActor`는 `TryEnterAwaitingAction()`~`TryEndTurn()` 구간에서만 채워진다.

씬 비활성화·Encounter 강제 중단에 대비해 `ForceReset()`도 `ExplorationEncounterSession`과 동일하게 제공한다.

### 3. BattleResult / BattleOutcome

Battle 종료 시점의 결과를 표현한다.

```text
BattleOutcome : Victory | Defeat
BattleResult : Outcome + TurnCount
```

실제 승패 계산(51일차 이후)이 붙기 전까지는 테스트 버튼으로만 확정한다.

### 4. Encounter ↔ Battle 연결

`ExplorationMonsterEncounterController`에 `BattleSession` 필드를 추가하고, Battle Command가 확정되는 시점에 자동으로 테스트 전투를 시작하도록 연결했다.

```text
Battle 선택
↓
Command 확정 (EncounterActionSelectionGate)
↓
BeginTestBattle()
↓
Player 1명 + Enemy 4명(접촉 몬스터 정의 재사용) 참가자 생성
↓
BattleContext 생성
↓
BattleSession.TryBeginBattle()
↓
BattleSession.TryStartTurn() → Turn 1
```

테스트용 스탯은 다음 상수로 고정했다(48~50일차에서 실제 스탯 연동으로 교체 예정).

```text
Player  MaxHp 20 / Speed 5
Enemy   MaxHp 10 / Speed 5 (슬롯 4개 동일 정의 복제)
```

### 5. 47일차 테스트 진행 API

실제 행동 Command 없이 Battle 상태 전환만 검증할 수 있도록 컨트롤러에 다음 메서드를 추가했다.

```text
TestAdvanceBattleTurn()
→ TurnStart → AwaitingAction → ResolvingAction → TurnEnd → 다음 TurnStart

TestWinBattle()
→ BattleSession Finished(Victory)
→ EncounterResultResolver로 EncounterResult(MonsterDefeated) 생성
→ 46일차 FinalizeActiveEncounter() 재사용 (방 완료 · 몬스터 비활성화 · 저장 · Encounter Idle 복귀)

TestLoseBattle()
→ BattleSession Finished(Defeat)
→ Encounter 결과 연결은 하지 않음 (패배 처리는 51·58일차 이후)

TestDismissFinishedBattle()
→ Finished 상태의 Battle을 닫고 Encounter 행동 선택으로 복귀 (패배 테스트 이후 재시도용)
```

`CompleteTestEncounter()`(46일차 TestEnd 버튼)는 Battle이 선택된 경우 더 이상 즉시 Encounter를 종료하지 않도록 가드를 추가했다. Escape 선택은 기존 46일차 흐름을 그대로 유지한다.

```text
선택 Command == Battle
→ CompleteTestEncounter() no-op (BattleSession 결과로만 종료)

선택 Command == Escape
→ 기존 46일차 흐름 그대로
```

### 6. 46일차 결과 시스템 재사용

46일차에 만든 `EncounterResult` · `EncounterResultResolver` · 방 완료 저장 흐름은 새로 만들지 않고 그대로 재사용한다.

```text
Battle 시스템
↓ 승리
EncounterResult.MonsterDefeated
↓
46일차 FinalizeActiveEncounter()
↓
RoomInstance.MarkCompleted()
↓
SaveDungeonProgress()
↓
탐험 복귀
```

즉 46일차 구현은 전투 이후의 출구이며, 47일차는 그 앞에 실제 Battle 상태 머신을 붙이는 구조다.

### 7. 전투 화면 Canvas (BattleHudController)

전투가 시작되면(`HasBattle == true`) 기존 `EncounterPanel`을 숨기고 별도의 `BattleCanvas`로 화면을 전환한다.

```text
EncounterPanelController.Update()
shouldShow = CurrentState == Active && !HasBattle
```

레이아웃 구성:

```text
위쪽       : 적 슬롯 1~4 (맨 왼쪽이 1번), 각각 일러스트 · 이름 · 체력바
오른쪽     : 플레이어 상태 일러스트 · 이름 · 체력바
왼쪽 가운데 아래 : 행동 버튼 자리 5개 (공격 · 행동 · 방어 · 아이템 · 도주, 실제 Command는 49~54일차 연결)
그 위      : 캐릭터 체력바 (HP · MP · SP, 글자가 바 왼쪽에 위치, 세 항목을 한 줄에 배치)
상단       : Battle 상태 텍스트 + 47일차 테스트 버튼(Test Next Turn / Test Win / Test Lose / 전투 닫기)
```

HP/MP/SP와 행동 버튼 영역은 뒤 배경 패널 없이 위치만 잡아주는 빈 컨테이너(`CreateContainer`)로 구성해 화면을 더 납작하고 가볍게 만들었다.

`BattleParticipantSlotView`는 일러스트 · 이름 · 체력바를 묶은 슬롯 하나를 표현하며, 적 슬롯 4개와 플레이어 상태 패널이 같은 컴포넌트를 재사용한다.

적 일러스트는 45일차 Billboard와 같은 경로(`Resources/MonsterSprites/{정의ID}`)에서 자동으로 불러온다.

### 8. Editor 설치 스크립트

44일차 `ProjectDeltaDay44EncounterUiInstaller` 패턴을 그대로 따라 `ProjectDeltaDay47BattleHudInstaller`를 추가했다.

```text
메뉴 : Project Delta > Day 47 > Build Battle HUD
```

씬을 열어 `BattleCanvas`를 생성하고, 적 슬롯 4개 · 플레이어 상태 패널 · HP/MP/SP 컨테이너 · 행동 버튼 5개 · 47일차 테스트 버튼까지 배치한 뒤 `BattleHudController`의 직렬화 필드를 자동으로 연결하고 씬을 저장한다.

기존에 44일차 `EncounterPanel`에 남아 있던 이전 버전 Battle 테스트 UI(별도 반복에서 시도했던 구성)는 재실행 시 자동으로 정리한다.

체력바 채움(Filled Image)에는 Unity 내장 리소스(`UI/Skin/UISprite.psd`)를 쓰지 않는다. 이 경로는 Unity 버전에 따라 존재하지 않을 수 있어(6000.3.21f1에서 실제로 로드 실패 발생), 흰색 스프라이트를 `Assets/ProjectDelta/Art/UI/BattleHudSolidWhite.png`로 직접 생성해 실제 에셋 파일로 저장하고 Sprite(2D and UI)로 임포트한 뒤 재사용한다.

---

## 47일차 전체 동작 흐름

```text
몬스터 접근
↓
Encounter Active
↓
전투 선택
↓
BeginTestBattle()
↓
BattleContext 생성 (Player 1 + Enemy 4)
↓
BattleSession Starting → TurnStart (Turn 1)
↓
EncounterPanel 숨김 / BattleCanvas 표시
↓
Test Next Turn 반복 가능
↓
Test Win
↓
BattleSession Finished (Victory)
↓
EncounterResult.MonsterDefeated
↓
방 완료 · 몬스터 비활성화 · 던전 진행 저장
↓
Encounter Idle 복귀 / 탐험 재개
```

패배 테스트 경로:

```text
Test Lose
↓
BattleSession Finished (Defeat)
↓
BattleCanvas에 결과만 표시 (Encounter는 아직 종료하지 않음)
↓
전투 닫기(TestDismissFinishedBattle)
↓
BattleSession Idle 복귀
↓
Encounter 행동 다시 선택 가능
```

---

## 테스트 추가

### BattleSessionTests

- 새 Session은 Idle · Context 없음으로 시작
- `TryBeginBattle()`은 유효한 Context에서만 Starting으로 전환
- 적이 없는 Context는 시작 거부
- 이미 진행 중인 Battle은 중복 시작 거부
- `TryStartTurn()`은 Starting · TurnEnd에서만 TurnStart로 전환하며 TurnNumber 증가
- `TryEnterAwaitingAction()`은 TurnStart 상태 + Context에 속한 유효 참가자에서만 허용
- Context에 속하지 않은 참가자는 거부
- TurnStart → AwaitingAction → ResolvingAction → TurnEnd → 다음 TurnStart 순환 검증
- `TryFinishBattle()`은 진행 중인 모든 상태에서 허용되며 Result·TurnCount 저장
- Idle · Finished 상태에서는 `TryFinishBattle()` 거부
- `TryReset()`은 Finished 상태에서만 허용되며 Context·Result·TurnNumber 초기화
- `ForceReset()`은 어떤 상태에서도 Idle로 복귀

### BattleContextTests

- `TryGetEnemyAtSlot()`이 맨 왼쪽부터 1~4번 순서로 적을 반환
- 범위를 벗어난 슬롯(음수, 최대 슬롯 초과)은 거부
- 빈 슬롯은 거부
- `TryGetParticipant()`가 Player·Enemy를 InstanceId로 정확히 조회하고, 존재하지 않는 InstanceId는 거부

---

## 이번 일차에서 제외한 내용

다음 내용은 이번 47일차에서 구현하지 않는다.

- 기본 공격 · 대상 선택 · 대상 재선택
- 명중 · 회피 · 데미지 · 방어 · 관통 계산
- 실제 HP 감소 · 사망 · 전투 이탈 처리
- 방어 · 아이템 사용 · 도주 · 항복 Command 실제 동작 (버튼 자리만 준비)
- 적 행동 예약 · 의도 표시 · AI
- 승리 보상 · 패배 결과의 실제 Encounter 연결 (Test Lose는 화면 표시까지만)
- 전투 중 저장 정책
- 전투 로그 UI (현재는 상태 텍스트만 제공)
- 실제 플레이어 · 몬스터 스탯 연동 (테스트 상수 사용 중)
- 플레이어 일러스트 (자리 표시만 준비)

현재 Battle 선택은 `BattleSession` 상태 머신을 실제로 진행시키지만, 승패 판정 자체는 여전히 테스트 버튼으로만 확정된다. 48~57일차에서 순서대로 실제 전투 로직을 채워 넣는다.

---

## 변경 파일

46일차 완료 커밋(`1b514d7`) 대비 이번 커밋에서 총 30개 파일이 추가·수정되었다.

### 생성

- `Assets/ProjectDelta/Art/UI/BattleHudSolidWhite.png`
- `Assets/ProjectDelta/Scripts/Application/BattleContext.cs`
- `Assets/ProjectDelta/Scripts/Application/BattleOutcome.cs`
- `Assets/ProjectDelta/Scripts/Application/BattleParticipant.cs`
- `Assets/ProjectDelta/Scripts/Application/BattleResult.cs`
- `Assets/ProjectDelta/Scripts/Application/BattleSession.cs`
- `Assets/ProjectDelta/Scripts/Application/BattleState.cs`
- `Assets/ProjectDelta/Scripts/Application/BattleTeam.cs`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaDay47BattleHudInstaller.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleHudController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleParticipantSlotView.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleContextTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleSessionTests.cs`

### 수정

- `Assets/ProjectDelta/Scenes/DungeonScene.unity` (`BattleCanvas` 추가)
- `Assets/ProjectDelta/Scripts/Presentation/EncounterPanelController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`
- `Project-Delta.slnx`

### 삭제

없음.

---

## 로컬 빌드 검증

GitHub CI가 아직 구성되어 있지 않으므로, 이번 일차는 로컬에서 각 어셈블리를 직접 빌드해 확인했다.

```text
dotnet build ProjectDelta.Application.csproj      → 오류 0개
dotnet build ProjectDelta.Presentation.csproj     → 오류 0개
dotnet build ProjectDelta.Editor.csproj           → 오류 0개
dotnet build ProjectDelta.Tests.EditMode.csproj   → 오류 0개
```

Unity Editor가 이미 실행 중인 상태였기 때문에 배치 모드(`-runTests`)를 통한 EditMode Test Runner 실행은 락 충돌로 수행하지 못했다. `BattleSessionTests` · `BattleContextTests` 통과 여부는 Unity Editor의 Test Runner 창에서 직접 확인했다.

`Project DeltaDay47BattleHudInstaller`를 통한 `BattleCanvas` 생성은 Unity Editor에서 직접 실행해 씬 저장까지 확인했다. 최초 실행 시 발생했던 `UI/Skin/UISprite.psd` 내장 리소스 로드 실패는 흰색 스프라이트를 실제 에셋 파일로 생성하도록 수정한 뒤 재실행해 해결을 확인했다.

---

## 47일차 결과

46일차까지는 Encounter에서 Battle을 "선택"하면 곧바로 테스트용 `MonsterDefeated` 결과로 변환되었다. 실제 전투라는 실행 단계 자체가 없었다.

47일차에서는 그 사이에 `BattleContext` · `BattleParticipant` · `BattleSession`으로 구성된 독립적인 Battle 실행 공간을 만들었다. Battle을 선택하면 실제로 참가자가 구성되고, `Idle → Starting → TurnStart → AwaitingAction → ResolvingAction → TurnEnd → Finished` 상태 머신을 따라 턴이 진행되며, 승리 시에만 46일차의 결과 처리 경로로 이어진다.

화면 쪽에서는 적 슬롯 4개(왼쪽부터 1~4번) · 플레이어 상태 일러스트 · 체력바 · 행동 버튼 자리를 갖춘 전투 Canvas를 만들어, 다음 일차부터 채워질 실제 공격 · 데미지 · 명중률 로직이 들어갈 자리를 미리 확보했다.

다음 단계에서는 이 Battle 상태 머신 위에 속도 기반 행동 순서(48일차)와 기본 공격 · 대상 선택(49일차)을 연결한다.
