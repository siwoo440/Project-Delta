# Project Delta - 75일차 개발일지

## 작업 주제

**적 최대 4명 확정·강제, 몬스터 간 비공격 규칙 회귀 테스트, 전투 조작 먹통 버그 수정**

---

## 개발 목표

74일차 devlog에는 "적 최대 3명 정합"이 다음 단계로 적혀 있었다. 코드를 확인해보니 `BattleContext.MaxEnemySlots`는 47일차부터 이미 `4`였고, 이 값을 실제로 쓰는 곳(HUD 슬롯 배열, 테스트용 적 배열, Editor 설치 스크립트)도 전부 4를 기준으로 이미 맞춰져 있었다. 3으로 줄일 경우 기존 HUD·테스트가 오히려 깨지는 방향이라 판단해, **3이 아니라 4를 정식 기준으로 확정**하기로 방향을 바꿨다.

작업 도중 사용자로부터 "전투에서 공격 등으로 행동하다가 가끔 조작이 먹통이 된다"는 버그 리포트가 들어와, 같은 날 원인을 조사하고 함께 수정했다.

```text
적 최대 인원(4명)을 BattleSession 진입 시점에 실제로 강제
몬스터 간 비공격 규칙이 우연이 아니라 의도된 규칙임을 회귀 테스트로 고정
전투 조작 먹통 버그(코루틴 정리 누락) 수정
```

---

## 주요 작업 내용

### 1. BattleContext.MaxEnemySlots — 확정 값으로 주석 정리

값 자체는 바꾸지 않고, "75일차에 4명으로 확정했다"는 주석만 추가했다. 이 상수를 참조하는 `BattleHudController.enemySlots`, `ExplorationMonsterEncounterController`의 테스트 적 배열, Editor 설치 스크립트는 이미 이 상수 하나로 관리되고 있어서 추가로 손댈 곳이 없었다.

### 2. BattleSession.TryBeginBattle() — 최대 인원 강제

지금까지는 `Enemies.Count == 0`(적이 없으면 거부)만 확인하고 상한이 없었다. `context.Enemies.Count > BattleContext.MaxEnemySlots`도 함께 거부 조건에 추가해, 5명 이상으로 전투가 시작되는 경우를 막았다.

```csharp
if (context == null
    || context.Player == null
    || context.Enemies == null
    || context.Enemies.Count == 0
    || context.Enemies.Count > BattleContext.MaxEnemySlots)
{
    return false;
}
```

### 3. 몬스터 간 비공격 규칙 확인

`BattleTargeting`을 다시 확인해보니, Enemy 차례의 `GetValidTargets()`는 애초에 "살아있는 Player"만 담고 다른 Enemy는 아예 후보에 넣지 않는 구조였다(49일차부터). 즉 몬스터가 여럿이어도 서로를 공격 대상으로 고르는 코드 경로 자체가 없다 — 별도 수정은 필요 없었다.

다만 이 동작이 "지금 우연히 그런 것"인지 "적이 여러 명일 때도 명시적으로 보장되는 것"인지 테스트로 확인된 적이 없었다. 75일차에 이를 회귀 테스트로 고정했다.

### 4. EditMode 테스트 추가

```text
BattleSessionTests
  - 적 정확히 4명(MaxEnemySlots)이면 정상적으로 시작됨
  - 적 5명(MaxEnemySlots + 1)이면 시작 거부, Idle 유지

BattleTargetingTests
  - 적 4명이 있는 상황에서 GetValidTargets(context, 특정 Enemy)는
    Player 한 명만 반환하고 나머지 Enemy 3명은 포함하지 않음
  - IsValidTarget(context, Enemy, 다른 Enemy)는 false
```

기존 `BattleContextTests`는 이미 `BattleContext.MaxEnemySlots`(4)를 기준으로 4명짜리 Context를 만들고 있어서 수정 없이 그대로 유지된다.

### 5. 전투 조작 먹통 버그 수정

사용자 리포트: "전투에서 공격 등으로 행동하다가 가끔 조작이 먹통이 된다."

**원인**: `ExplorationMonsterEncounterController.OnDisable()`이 실행 중이던 자동 진행 코루틴(`autoAdvanceRoutine`)을 정리하지 않았다.

전투 중 다음 행동자로 자동으로 넘어가는 `AdvanceBattleTurnRoutine()`은 `autoAdvanceRoutine` 필드에 자기 자신을 저장해두고, 루프를 끝까지 정상적으로 마쳤을 때만 마지막에 `autoAdvanceRoutine = null`로 되돌린다. 이 루프 중간에는 연출 대기용 `yield return new WaitForSeconds(...)`가 있다.

문제는 정확히 이 대기 타이밍에 씬 전환·오브젝트 비활성화 등으로 컨트롤러의 `OnDisable()`이 호출되면, Unity가 실행 중이던 코루틴을 그 자리에서 즉시 중단시켜버린다는 점이다. 코루틴 본문에 남아 있던 `autoAdvanceRoutine = null` 코드는 절대 실행되지 않는다.

`OnDisable()` 자체는 `battleSession.ForceReset()`으로 전투 상태를 깨끗이 되돌리지만, `autoAdvanceRoutine` 필드만 죽은 코루틴 참조를 계속 들고 남는다. 이후 다시 전투를 시작해도 `TestAdvanceBattleTurn()`이

```csharp
if (autoAdvanceRoutine != null) { return; }
```

이 조건에 걸려 아무것도 하지 않고 즉시 리턴한다. 다음 행동자로 넘어가는 절차 자체가 시작되지 않으니 `battleSession.State`는 영원히 `AwaitingAction`으로 전이되지 않고, 이를 기준으로 활성화 여부를 판단하는 HUD 버튼이 전부 반응 없는 상태가 된다. 정확히 저 대기 타이밍에 비활성화가 겹쳐야만 재현되는 타이밍 버그라서 "가끔"만 발생한다.

**수정**: `OnDisable()`에서 코루틴이 실행 중이면 명시적으로 `StopCoroutine()`하고 참조를 `null`로 되돌리도록 고쳤다.

```csharp
private void OnDisable()
{
    RestoreExplorationControl();

    if (autoAdvanceRoutine != null)
    {
        StopCoroutine(autoAdvanceRoutine);
        autoAdvanceRoutine = null;
    }

    session.ForceReset();
    battleSession.ForceReset();
    // ...
}
```

**추가로 발견한 2차 위험 요소(이번엔 수정하지 않음)**: 74일차에 추가된 "취소 대기(Pending Cancellation)" 상태 소비가 `BattleIntentRuntimeController`의 매 프레임 갱신에만 의존하고 있어, 그 컴포넌트가 씬에 없거나 비활성화되는 특수 상황이면 해당 몬스터의 취소 상태가 풀리지 않을 수 있다. 다만 버튼이 완전히 먹통되는 것과는 다르고(라운드 진행 자체는 계속됨) 이번 리포트와 직접 연결된다는 확신은 없어 범위에서 제외했다.

---

## 변경 파일

```text
Assets/ProjectDelta/Scripts/Application/BattleContext.cs
Assets/ProjectDelta/Scripts/Application/BattleSession.cs
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs

Assets/ProjectDelta/Tests/EditMode/BattleSessionTests.cs
Assets/ProjectDelta/Tests/EditMode/BattleTargetingTests.cs
```

---

## 확인 사항

- 적 최대 인원을 3이 아니라 **4명으로 확정** (기존 HUD·테스트 구조와 일치하는 방향으로 결정)
- `BattleSession.TryBeginBattle()`이 4명 초과 시 전투 시작을 거부
- 몬스터 간 비공격 규칙이 이미 `BattleTargeting`에 구현돼 있음을 확인, 별도 로직 수정 없이 회귀 테스트로 고정
- 새 EditMode 테스트 4개로 최대 인원 강제와 몬스터 간 비공격 규칙을 검증
- `OnDisable()`에서 `autoAdvanceRoutine` 코루틴을 명시적으로 정지·정리하도록 수정해 전투 조작 먹통 버그 해결
- 기존 `BattleContextTests`(4명 기준)는 변경 없이 그대로 통과

Unity Editor에서의 실제 스크립트 컴파일과 Test Runner 통과 여부, 그리고 먹통 버그가 실제로 재현되지 않는지는 이 저장소 diff만으로는 확정할 수 없으므로, Unity Editor에서 직접 최종 확인이 필요하다.

---

## 이번 일차 완료 상태

75일차 목표인 **적 최대 4명 확정·강제 및 몬스터 간 비공격 규칙 회귀 테스트**를 완료했고, 추가로 발견된 **전투 조작 먹통 버그**도 같은 날 수정했다.

---

## 다음 단계

74일차 devlog가 원래 예정했던 "몬스터별 AI 데이터 확장"과, 아직 남아 있는 아이템·유혹 Command, 스킬 선택 UI로 이어갈 수 있다. 2차 위험 요소로 남겨둔 Pending Cancellation 소비 로직도 필요하면 다음 기회에 점검한다.
