# Project Delta - 48일차 개발일지

## 개발 목표

47일차에 만든 `BattleContext`·`BattleSession` 턴 상태 머신은 "턴 하나 = 참가자 한 명이 행동"으로 단순화되어 있었다. 실제로는 한 턴 안에서 살아있는 참가자 전원이 순서대로 한 번씩 행동해야 한다.

이번 일차의 핵심 목표는 다음과 같다.

- Speed 기반 행동 순서 계산 로직 추가
- `TurnStart`에서 순서 큐를 만들고, 큐가 빌 때까지 행동을 반복한 뒤에만 `TurnEnd`로 넘어가도록 `BattleSession` 재구성
- 47일차 테스트 진행 버튼을 "참가자 한 명씩" 진행하도록 변경
- 전투 화면에 현재 행동자 표시 추가

실제 공격·데미지·명중률 계산은 이번 일차에 포함하지 않는다.

---

## 구현 내용

### 1. BattleTurnOrder 추가

살아있는 참가자를 대상으로 이번 턴의 행동 순서를 계산하는 정적 클래스를 추가했다.

```text
BattleTurnOrder.Build(context)
→ 살아있는 참가자만(IsAlive) 대상
→ Speed 내림차순 정렬
→ 동률일 때는 Player 먼저 → 적 슬롯 1→4번 순서
```

동률 우선순위는 별도 비교 로직 없이, 후보 목록을 "Player → 적 슬롯 순"으로 먼저 나열한 뒤 안정 정렬(Stable Sort)만 적용해 자연스럽게 유지되도록 구현했다.

### 2. BattleSession의 턴 개념 재정의

기존 상태 머신(`Idle → Starting → TurnStart → AwaitingAction → ResolvingAction → TurnEnd → Finished`)은 그대로 유지하되, 상태 사이를 오가는 규칙을 바꿨다.

```text
TurnStart
↓ TryStartTurn() 시점에 BattleTurnOrder로 순서 큐 생성
↓ TryEnterAwaitingAction() → 큐에서 다음 참가자 자동 선출
AwaitingAction (참가자 A)
↓ TryBeginResolveAction()
ResolvingAction
↓ 큐에 참가자 남음 → TryEnterAwaitingAction() 재호출 → AwaitingAction (참가자 B)
↓ … 전원 완료
↓ 큐 소진 → TryEndTurn()
TurnEnd
↓ TryStartTurn() → 다음 턴 (TurnNumber 증가, 새 큐 생성)
```

핵심 변경점:

```text
TryEnterAwaitingAction(actor)   → TryEnterAwaitingAction()
(대상을 인자로 받던 방식)         (큐에서 자동으로 다음 참가자를 뽑는 방식)

TryEndTurn()
→ 이제 이번 턴 순서 큐가 비어 있을 때만 허용
→ 아직 행동하지 않은 참가자가 있으면 거부
```

`PendingActorsThisTurn`(남은 순서 큐)과 `HasPendingActorsThisTurn`을 새로 노출해, 호출하는 쪽이 "다음 참가자로 넘어갈지" 아니면 "이번 턴을 끝낼지" 판단할 수 있게 했다.

`TryReset()` · `ForceReset()`에도 순서 큐 정리를 추가했다.

### 3. 47일차 테스트 진행 버튼 변경

`ExplorationMonsterEncounterController.TestAdvanceBattleTurn()`이 "라운드 전체를 한 번에 진행"에서 "참가자 한 명을 진행"으로 바뀌었다.

```text
TurnStart 또는 ResolvingAction 상태에서 클릭
↓
TryEnterAwaitingAction() → TryBeginResolveAction()
↓
이번 턴에 남은 참가자 있음 → 여기서 멈춤 (다음 클릭을 기다림)
↓
이번 턴에 남은 참가자 없음 → TryEndTurn() → TryStartTurn() 자동 진행
```

버튼을 여러 번 클릭하면 참가자가 Speed 순서대로 한 명씩 드러나고, 마지막 참가자가 끝나면 자동으로 다음 턴이 시작된다.

`CurrentBattleActor` 프로퍼티를 추가해 컨트롤러 바깥(HUD)에서도 지금 행동 중인(또는 방금 행동한) 참가자를 조회할 수 있도록 했다.

### 4. 전투 화면 상태 표시 갱신

`BattleHudController`의 상태 텍스트에 현재 행동자와 Speed를 함께 표시한다.

```text
Battle : TurnStart / Turn 1
↓ (진행 버튼 클릭)
Battle : ResolvingAction / Turn 1 / Actor MON_TEST#1 (Speed 5)
```

진행 버튼(`Test Advance`, 47일차의 `Test Next Turn`에서 이름 변경)의 활성 조건도 "이번 턴에 더 진행할 수 있는 상태(TurnStart 또는 ResolvingAction)"로 갱신했다.

### 5. Editor 설치 스크립트 갱신

`ProjectDeltaDay47BattleHudInstaller`가 만드는 버튼 라벨을 `Test Next Turn` → `Test Advance`로 바꿔, 행동 단위가 "턴 전체"에서 "참가자 한 명"으로 바뀐 것을 반영했다. 메뉴 경로(`Project Delta > Day 47 > Build Battle HUD`)는 그대로다.

---

## 48일차 전체 동작 흐름

```text
전투 시작 (BeginTestBattle)
↓
BattleSession Starting → TurnStart (순서 큐 생성)
↓
Test Advance 클릭
↓
1번째 행동자 AwaitingAction → ResolvingAction
↓
Test Advance 클릭
↓
2번째 행동자 AwaitingAction → ResolvingAction
↓
… (참가자 수만큼 반복)
↓
마지막 행동자까지 완료
↓
TurnEnd → 다음 TurnStart (TurnNumber 증가, 순서 큐 재생성)
```

Test Win / Test Lose / 전투 닫기는 47일차와 동일하게 동작한다.

---

## 테스트 추가·변경

### BattleSessionTests (전면 재작성)

새 API(`TryEnterAwaitingAction()` 무인자, 순서 큐 기반 `TryEndTurn()`)에 맞춰 다시 작성했다.

- `TryStartTurn()`이 순서 큐를 생성하는지 (Player 1 + Enemy 1 → 큐 2명)
- `TryEnterAwaitingAction()`이 TurnStart·ResolvingAction에서만 허용되고, 큐에서 다음 참가자를 뽑는지
- 동률(Speed 5) 상황에서 Player가 먼저 나오는지
- AwaitingAction 상태에서 중복 호출 거부
- 큐가 빈 상태에서 `TryEnterAwaitingAction()` 거부
- 아직 행동자가 남았을 때 `TryEndTurn()` 거부
- 전원 행동 완료 후 TurnEnd → 다음 TurnStart에서 큐가 다시 채워지는 전체 순환
- `TryFinishBattle()` / `TryReset()` / `ForceReset()`이 순서 큐까지 정리하는지

### BattleTurnOrderTests (신규)

- Speed 내림차순 정렬
- Player·Enemy 동률 시 Player 우선
- 적끼리 동률 시 왼쪽(1번) 슬롯 우선
- 사망한 참가자(MaxHp 0) 제외
- Context가 `null`이거나 적이 없을 때의 예외적 입력 처리

---

## 이번 일차에서 제외한 내용

다음 내용은 이번 48일차에서 구현하지 않는다.

- 기본 공격 · 대상 선택 · 대상 재선택
- 명중 · 회피 · 데미지 · 방어 · 관통 계산
- 실제 HP 감소 · 사망 처리 (`BattleParticipant`에 여전히 HP를 깎는 API가 없음)
- 상태이상으로 인한 행동 순서 변경(둔화·가속 등)
- 적 행동 예약 · 의도 표시 · AI
- 전투 로그 UI (상태 텍스트로만 행동자 표시)

`BattleTurnOrder`는 사망 참가자를 제외하도록 미리 만들어 두었지만, 실제로 참가자를 죽이는 기능은 아직 없다(테스트에서는 `MaxHp 0`으로 생성해 검증). 51일차 이후 HP 감소·사망 처리가 들어오면 이 필터링 로직을 그대로 재사용할 수 있다.

---

## 변경 파일

47일차 완료 커밋(`02be166`) 대비 이번 커밋에서 총 10개 파일이 추가·수정되었다.

### 생성

- `Assets/ProjectDelta/Scripts/Application/BattleTurnOrder.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleTurnOrderTests.cs`

### 수정

- `Assets/ProjectDelta/Scripts/Application/BattleSession.cs`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaDay47BattleHudInstaller.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleHudController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleSessionTests.cs`
- `Assets/ProjectDelta/Scenes/DungeonScene.unity` (`Test Advance` 버튼 라벨 갱신)
- `Project-Delta.slnx`

### 삭제

없음.

---

## 로컬 빌드 검증

GitHub CI가 구성되어 있지 않아, 47일차와 동일하게 로컬에서 각 어셈블리를 직접 빌드해 확인했다.

```text
dotnet build ProjectDelta.Application.csproj      → 오류 0개
dotnet build ProjectDelta.Presentation.csproj     → 오류 0개
dotnet build ProjectDelta.Editor.csproj           → 오류 0개
dotnet build ProjectDelta.Tests.EditMode.csproj   → 오류 0개
```

`BattleTurnOrder.cs` · `BattleTurnOrderTests.cs`는 로컬 `.csproj`에도 추가해 함께 빌드를 확인했다.

Unity Editor가 이미 실행 중인 상태였기 때문에 배치 모드를 통한 EditMode Test Runner 실행은 이번에도 수행하지 못했다. `BattleSessionTests` · `BattleTurnOrderTests` 통과 여부와 `Test Advance` 버튼의 실제 동작(참가자별 순차 진행)은 Unity Editor에서 직접 확인했다.

---

## 48일차 결과

47일차까지는 Battle의 "턴"이 사실상 참가자 한 명이 행동하는 단위와 같았다. 48일차에서는 `BattleTurnOrder`로 Speed 기반 행동 순서를 계산하고, `BattleSession`이 한 턴 안에서 살아있는 참가자 전원을 순서대로 처리한 뒤에만 다음 턴으로 넘어가도록 상태 머신을 재구성했다.

동률 처리 규칙(Player 우선, 적은 슬롯 순서)을 명확히 정해 두어, 이후 상태이상이나 속도 버프가 추가되어도 정렬 기준만 바뀌고 큐 소진·턴 종료 로직은 그대로 재사용할 수 있다.

다음 단계에서는 이 행동 순서 위에 실제 기본 공격과 대상 선택(49일차)을 연결한다.
