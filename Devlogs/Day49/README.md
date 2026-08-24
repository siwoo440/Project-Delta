# Project Delta - 49일차 개발일지

## 개발 목표

48일차까지는 "누가 언제 행동하는가"(BattleSession + BattleTurnOrder)만 갖춰져 있었다. 실제 행동 자체는 없었다.

이번 일차의 핵심 목표는 다음과 같다.

- 전투 내부 행동(공격)을 표현하는 Command 구조 추가
- 행동자 진영에 따른 유효 대상 계산
- 대상 선택·재선택이 가능한 상태 관리
- 전투 화면에서 적 슬롯 클릭으로 대상 지정, "공격" 버튼으로 확정하는 흐름 연결

실제 명중률·데미지 계산·HP 감소는 이번 일차에 포함하지 않는다.

---

## 구현 내용

### 1. IBattleCommand / BattleCommandResult 추가

44일차 `IEncounterCommand`(Encounter 진입/탈출 선택)와 동일한 구조를, 전투 내부 행동 전용으로 분리했다.

```text
IBattleCommand
├─ Id ("Attack")
├─ DisplayName ("공격")
└─ Execute(BattleContext, actor, target) → BattleCommandResult
```

`BattleCommandResult`는 `CommandId · Accepted · Message`로 `EncounterCommandResult`와 동일한 모양이다.

### 2. BattleTargeting 추가

행동자 진영에 따라 선택 가능한 대상을 계산하는 정적 헬퍼를 추가했다.

```text
GetValidTargets(context, actor)
→ actor.Team == Player → 살아있는 Enemies만 반환
→ actor.Team == Enemy  → 살아있는 Player만 반환 (아직 아군 1명)

IsValidTarget(context, actor, target)
→ target이 살아있고
→ target.Team != actor.Team (아군 오폭 금지)
→ target이 현재 BattleContext 소속일 때만 true
```

### 3. AttackBattleCommand 추가

실제 명중률·데미지 계산(50일차) 전까지, 공격 대상 지정이 유효한지만 검증하고 의도를 반환한다.

```text
Execute(context, actor, target)
→ Context·행동자 누락 → Reject
→ BattleTargeting.IsValidTarget 실패 → Reject "대상을 선택할 수 없습니다."
→ 유효한 대상 → Accept "공격 선언 / {actor} → {target}"
```

### 4. BattleSession에 대상 선택 상태 추가

`CurrentActor`처럼 `SelectedTarget`을 세션이 직접 관리한다.

```text
TrySelectTarget(target)
→ AwaitingAction 상태에서만 허용
→ BattleTargeting.IsValidTarget 통과해야 저장
→ 여러 번 호출 시 마지막 유효한 선택으로 교체 (재선택)
```

`SelectedTarget`은 `TryEnterAwaitingAction()`으로 다음 행동자에게 넘어갈 때, 그리고 `TryFinishBattle()` · `TryReset()` · `ForceReset()`에서 초기화된다.

### 5. Encounter 컨트롤러의 턴 진행 흐름 재구성

48일차의 `TestAdvanceBattleTurn()`은 행동자를 반영하자마자 곧바로 `ResolvingAction`까지 진행했다. 49일차부터는 대상 선택을 기다리기 위해 `AwaitingAction`에서 멈춘다.

```text
TestAdvanceBattleTurn()
→ TryEnterAwaitingAction() (다음 행동자 선출)
→ 행동자가 Enemy면 유일한 대상(Player)을 자동으로 미리 선택
→ 여기서 멈춤 (Player 차례라면 대상 선택을 기다림)
```

공격 확정은 별도 메서드로 분리했다.

```text
ConfirmAttack()
→ AttackBattleCommand.Execute(context, CurrentActor, SelectedTarget)
→ Accept → TryBeginResolveAction()
→ 이번 턴에 남은 행동자 없음 → TryEndTurn() → TryStartTurn() 자동 진행
→ Reject → 상태 전환 없이 실패 메시지만 반환
```

`TrySelectBattleTarget(target)` · `GetValidBattleTargets()` · `SelectedBattleTarget` · `LastBattleCommandResult`를 컨트롤러 바깥(HUD)에 노출했다.

### 6. 적 슬롯 클릭으로 대상 선택

`BattleParticipantSlotView`에 클릭·선택 상태 표시를 추가했다.

```text
SetSelectable(bool) → 슬롯 클릭 가능 여부 + 배경색(선택 가능 시 녹색 톤)
SetSelected(bool)   → 지금 선택된 대상이면 강조 배경색
SetOnClick(callback) → 클릭 시 콜백 등록 (중복 등록 방지)
```

`BattleHudController`가 매 프레임 `GetValidBattleTargets()` 결과와 `SelectedBattleTarget`을 각 적 슬롯에 반영하고, 슬롯 클릭 시 `TrySelectBattleTarget()`을 호출하도록 연결했다.

### 7. 공격 버튼 실제 연결

행동 버튼 자리(공격·행동·방어·아이템·도주) 중 "공격" 버튼만 실제로 동작하게 됐다.

```text
AwaitingAction 상태 + 대상 지정됨
→ 공격 버튼 활성화
→ 클릭 시 ConfirmAttack() 호출
```

나머지 4개(행동·방어·아이템·도주)는 여전히 비활성 자리로 남아 있다.

### 8. Editor 설치 스크립트 갱신

`ProjectDeltaDay47BattleHudInstaller`가 적 슬롯 루트에 `Button` 컴포넌트를 추가하고(색상은 `BattleParticipantSlotView`가 직접 관리하므로 Button 자체 트랜지션은 끔), 행동 버튼 5개 중 1번(공격)을 `attackButton`으로 분리해 `BattleHudController`에 연결하도록 바꿨다. 메뉴 경로(`Project Delta > Day 47 > Build Battle HUD`)는 그대로다.

---

## 49일차 전체 동작 흐름

```text
Test Advance 클릭
↓
다음 행동자 AwaitingAction
↓
Player 차례라면: 적 슬롯 클릭 → TrySelectBattleTarget()
↓                (다른 슬롯 클릭 시 재선택)
Enemy 차례라면: Player가 자동으로 미리 선택됨
↓
공격 버튼 클릭 (대상이 지정된 경우에만 활성)
↓
ConfirmAttack() → AttackBattleCommand 검증
↓
ResolvingAction
↓
이번 턴 마지막 행동자였다면 → TurnEnd → 다음 TurnStart
```

---

## 테스트 추가

### BattleTargetingTests (신규)

- Player 행동자 → 살아있는 적만 대상
- Enemy 행동자 → 살아있는 Player만 대상
- Context·행동자 누락 시 빈 목록
- 살아있는 상대 진영 참가자는 유효 대상
- 같은 진영(자기 자신 포함) 대상 거부
- 사망한 대상 거부
- Context에 속하지 않은 참가자 거부

### AttackBattleCommandTests (신규)

- Id·DisplayName 확인
- 유효한 대상 → Accept, 메시지에 행동자·대상 포함
- Context 없음 → Reject
- 같은 진영 대상 → Reject
- 사망한 대상 → Reject

### BattleSessionTests (추가)

- `TrySelectTarget()`은 AwaitingAction에서만 허용
- 잘못된 재선택(아군 지정)은 기존 선택을 바꾸지 않음
- 다음 행동자로 넘어가면 이전 선택이 초기화됨

---

## 이번 일차에서 제외한 내용

다음 내용은 이번 49일차에서 구현하지 않는다.

- 명중 · 회피 · 데미지 · 방어 · 관통 계산
- 실제 HP 감소 · 사망 처리
- 방어 · 아이템 · 도주 Command (버튼 자리만 유지)
- 다중 아군 대상 적 AI (아직 아군이 Player 1명뿐이라 Enemy 대상 선택은 자동)
- 전투 로그 UI (상태 텍스트에 최근 결과 메시지만 한 줄 추가)

`ConfirmAttack()`은 `AttackBattleCommand`가 대상 지정이 유효한지만 확인할 뿐, 실제로 아무 효과도 발생시키지 않는다. 50일차부터 이 자리에 데미지 계산을 채운다.

---

## 변경 파일

48일차 완료 커밋(`871156c`) 대비 이번 커밋에서 총 18개 파일이 추가·수정되었다.

### 생성

- `Assets/ProjectDelta/Scripts/Application/BattleTargeting.cs`
- `Assets/ProjectDelta/Scripts/Application/BattleCommandResult.cs`
- `Assets/ProjectDelta/Scripts/Application/IBattleCommand.cs`
- `Assets/ProjectDelta/Scripts/Application/AttackBattleCommand.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleTargetingTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/AttackBattleCommandTests.cs`

### 수정

- `Assets/ProjectDelta/Scripts/Application/BattleSession.cs`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaDay47BattleHudInstaller.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleHudController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleParticipantSlotView.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleSessionTests.cs`
- `Assets/ProjectDelta/Scenes/DungeonScene.unity` (적 슬롯 Button 추가, 공격 버튼 분리 연결)
- `Project-Delta.slnx`

### 삭제

없음.

---

## 로컬 빌드 검증

GitHub CI가 구성되어 있지 않아, 47·48일차와 동일하게 로컬에서 각 어셈블리를 직접 빌드해 확인했다.

```text
dotnet build ProjectDelta.Application.csproj      → 오류 0개
dotnet build ProjectDelta.Presentation.csproj     → 오류 0개
dotnet build ProjectDelta.Editor.csproj           → 오류 0개
dotnet build ProjectDelta.Tests.EditMode.csproj   → 오류 0개
```

Unity Editor가 이미 실행 중인 상태였기 때문에 배치 모드를 통한 EditMode Test Runner 실행은 이번에도 수행하지 못했다. `BattleTargetingTests` · `AttackBattleCommandTests` · 갱신된 `BattleSessionTests` 통과 여부와 실제 화면에서의 대상 선택·재선택·공격 확정 흐름은 Unity Editor에서 직접 확인했다.

---

## 49일차 결과

48일차까지 만든 "누가 행동하는가" 위에, 49일차에서는 "무엇을 할 것인가"의 첫 조각으로 공격 대상 선택을 연결했다. `BattleTargeting`으로 진영별 유효 대상을 가려내고, `BattleSession.SelectedTarget`으로 대상을 재지정 가능한 상태로 관리하며, `AttackBattleCommand`가 그 선택이 유효한지 검증만 하는 구조를 만들었다.

화면에서는 적 슬롯을 직접 클릭해 대상을 지정·재지정할 수 있고, "공격" 버튼이 처음으로 실제 동작하는 버튼이 됐다. 다만 확정해도 아직 아무 효과도 일어나지 않는다.

다음 단계에서는 이 공격 선언 위에 명중률·데미지·방어·관통 계산(50일차)을 연결한다.
