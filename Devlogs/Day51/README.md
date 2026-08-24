# Project Delta - 51일차 개발일지

## 개발 목표

50일차까지 HP는 실제로 줄어들었지만, 0이 되어도 아무 일도 일어나지 않았다. 참가자가 죽어도 계속 행동 순서에 남을 수 있었고, 전멸해도 전투가 저절로 끝나지 않아 `Test Win`/`Test Lose` 버튼을 직접 눌러야 했다.

이번 일차의 핵심 목표는 다음과 같다.

- 전멸 여부를 자동으로 판정하는 로직 추가
- 죽은 참가자가 턴 순서에서 조용히 빠지는 "전투 이탈" 처리
- 공격 판정마다 자동으로 승패를 확인해 전투를 끝내는 흐름 연결
- 패배 시 최소한의 결과 처리(메인 메뉴 복귀)까지 연결

패배 시 게임 오버 연출·보상·재도전 같은 완성된 경험은 이번 일차에 포함하지 않는다(58일차).

---

## 구현 내용

### 1. BattleOutcomeEvaluator 추가

전멸 여부를 판정하는 정적 헬퍼를 추가했다. `BattleTargeting`이 쓰던 "Player vs Enemies" 진영 규칙을 그대로 재사용한다.

```text
TryEvaluate(context, out outcome)
→ Player 사망 → Defeat
→ 살아있는 Enemy가 하나도 없음 → Victory
→ 둘 다 생존자가 있으면 → false (전투 계속)
→ 상호 전멸(둘 다 동시에 전멸)이면 → Defeat 우선
```

### 2. BattleSession: 죽은 참가자 턴 순서에서 건너뛰기

`BattleTurnOrder.Build()`는 턴 **시작 시점**에만 생존자를 거른다. 한 턴 안에서 여러 참가자가 순서대로 행동하다 보니, 먼저 행동한 참가자가 아직 순서가 오지 않은 참가자를 죽일 수 있다. `TryEnterAwaitingAction()`이 큐에서 다음 행동자를 꺼낼 때 이를 확인하도록 보강했다.

```text
큐에서 하나 꺼냄
↓
죽어 있음 → 버리고 다음 항목 꺼냄 (반복)
↓
살아있는 참가자 발견 → AwaitingAction
↓
꺼낼 항목이 없을 때까지 전부 죽어있었다면 → 실패 반환 (호출자가 TryEndTurn() 사용)
```

이것이 "전투 이탈" — 죽은 참가자가 별도 처리 없이 이번 턴 나머지 행동에서 자연스럽게 빠지는 방식이다.

### 3. ConfirmAttack()에 자동 종료 연결

50일차에는 데미지를 적용한 뒤 곧바로 다음 행동자·다음 턴으로 넘어갔다. 51일차부터는 그 사이에 승패 확인이 끼어든다.

```text
target.ApplyDamage() 적용
↓
BattleOutcomeEvaluator.TryEvaluate()
↓
결정됨 → FinishBattle(outcome) 호출 후 즉시 반환 (턴 진행 로직 건너뜀)
결정 안 됨 → 기존처럼 다음 행동자·다음 턴 진행
```

### 4. TestWinBattle / TestLoseBattle을 FinishBattle(outcome)으로 통합

47일차부터 있던 두 테스트 버튼의 로직을 공용 메서드로 뽑아, 자동 판정과 수동 테스트가 같은 경로를 타도록 정리했다.

```text
FinishBattle(outcome)
↓ battleSession.TryFinishBattle(outcome)
↓
Victory → 46일차 EncounterResultResolver → FinalizeActiveEncounter()
          (방 완료·몬스터 비활성화·저장·Encounter Idle 복귀는 그대로 재사용)
          → battleSession.TryReset()
↓
Defeat  → ApplicationFlow.Current?.ReturnToTitle()
          (진행 중인 런 포기 + 저장 삭제 + 타이틀 씬 이동은 기존 24·26일차 구현을 그대로 사용)
```

패배 처리는 `ReturnToTitle()`을 새로 만들지 않고 기존에 `DungeonDebugMenuController`·`SettingsSceneController`가 쓰던 것과 동일한 진입점을 그대로 호출한다. 로그라이트 관례상 런 포기 = 저장 삭제이므로, 패배도 자연스럽게 같은 취급을 받는다.

### 5. 죽은 "전투 닫기" 버튼 제거

패배가 즉시 메인 메뉴로 나가버리게 되면서, Finished 상태를 닫기만 하던 `TestDismissFinishedBattle()`이 쓸모없어졌다. 관련 요소를 전부 정리했다.

```text
삭제됨
├─ ExplorationMonsterEncounterController.TestDismissFinishedBattle()
├─ ExplorationMonsterEncounterController.IsBattleFinished (다른 곳에서도 안 쓰임)
├─ BattleHudController.testDismissButton 필드·바인딩
└─ ProjectDeltaDay47BattleHudInstaller의 TestDismissButton 생성·연결 코드
```

`Build Battle HUD`를 다시 실행하면 씬에서도 버튼이 사라진다(캔버스를 통째로 지우고 새로 만드는 기존 방식 그대로).

### 6. 화면은 대부분 자동으로 반영됨

사망한 참가자의 회색 초상화·선택 불가 표시는 이미 `IsAlive` 기반이라 추가 코드 없이 그대로 맞물린다. 승패가 결정되면 상태 텍스트가 47일차부터 있던 결과 표시로 자연스럽게 전환된다.

---

## 51일차 전체 동작 흐름

### 승리 경로

```text
공격 적중으로 마지막 남은 적 처치
↓
BattleOutcomeEvaluator → Victory
↓
FinishBattle(Victory)
↓
EncounterResult.MonsterDefeated
↓
방 완료 · 몬스터 비활성화 · 던전 진행 저장
↓
Encounter Idle 복귀 / 탐험 재개
```

### 패배 경로

```text
Player HP 0
↓
BattleOutcomeEvaluator → Defeat
↓
FinishBattle(Defeat)
↓
ApplicationFlow.ReturnToTitle()
↓
진행 중이던 런 포기 + 저장 삭제
↓
TitleScene으로 이동
```

### 전투 이탈 경로

```text
같은 턴 안에서 A가 아직 순서가 안 온 B를 처치
↓
B의 차례가 옴
↓
TryEnterAwaitingAction()이 B를 건너뜀
↓
다음 살아있는 참가자로 진행 (또는 아무도 안 남았으면 TurnEnd)
```

---

## 테스트 추가

### BattleOutcomeEvaluatorTests (신규)

- Player 사망 → Defeat
- 적 전원 사망 → Victory
- 양쪽 다 생존자 있음 → 판정 없음(false)
- 상호 전멸 → Defeat 우선
- Context가 `null` → 판정 없음

### BattleSessionTests (추가)

- Speed 순서상 아직 차례가 안 온 참가자가 먼저 죽으면, 그 참가자의 차례에서 건너뛰고 다음 살아있는 참가자로 진행
- 남은 전원이 죽어 있으면 `TryEnterAwaitingAction()`이 실패하고, 그 직후 `TryEndTurn()`은 성공

---

## 이번 일차에서 제외한 내용

다음 내용은 이번 51일차에서 구현하지 않는다.

- 패배 시 실제 게임 오버 화면·연출 (지금은 곧바로 씬 전환만 일어남)
- 패배 시 보상·재도전 선택지 (58일차)
- 방어 Command (52일차)
- 여러 아군이 있을 때의 패배 조건(전원 사망 등) — 지금은 아군이 Player 1명뿐이라 Player 사망 = 즉시 Defeat

---

## 변경 파일

50일차 완료 커밋(`90f224b`) 대비 이번 커밋에서 총 8개 파일이 추가·수정되었다.

### 생성

- `Assets/ProjectDelta/Scripts/Application/BattleOutcomeEvaluator.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleOutcomeEvaluatorTests.cs`

### 수정

- `Assets/ProjectDelta/Scripts/Application/BattleSession.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleHudController.cs`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaDay47BattleHudInstaller.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleSessionTests.cs`
- `Assets/ProjectDelta/Scenes/DungeonScene.unity` (전투 닫기 버튼 제거 반영)

### 삭제

없음 (파일 삭제는 없고, 기존 파일 내부의 죽은 코드만 정리).

---

## 로컬 빌드 검증

GitHub CI가 구성되어 있지 않아, 47~50일차와 동일하게 로컬에서 각 어셈블리를 직접 빌드해 확인했다.

```text
dotnet build ProjectDelta.Application.csproj      → 오류 0개
dotnet build ProjectDelta.Presentation.csproj     → 오류 0개
dotnet build ProjectDelta.Editor.csproj           → 오류 0개
dotnet build ProjectDelta.Tests.EditMode.csproj   → 오류 0개
```

Unity Editor가 이미 실행 중인 상태였기 때문에 배치 모드를 통한 EditMode Test Runner 실행은 이번에도 수행하지 못했다. `BattleOutcomeEvaluatorTests`·갱신된 `BattleSessionTests` 통과 여부와 실제 화면에서 전멸 시 자동으로 방 완료·저장·탐험 복귀가 이어지는지, 패배 시 타이틀로 돌아가는지는 Unity Editor에서 직접 확인했다.

---

## 51일차 결과

50일차까지 HP는 줄어들었지만 죽음은 아무 의미가 없었다. 51일차에서는 `BattleOutcomeEvaluator`로 전멸을 자동으로 판정하고, `BattleSession`이 죽은 참가자를 턴 순서에서 조용히 제외하며, `ConfirmAttack()`이 공격이 끝날 때마다 승패를 확인해 전투를 스스로 종료하도록 연결했다.

승리는 46일차부터 있던 Encounter 결과 처리로 그대로 이어지고, 패배는 일단 기존 "런 포기" 경로를 빌려 메인 메뉴로 돌아가는 최소한의 처리를 갖췄다. 더 나은 패배 경험(결과 요약, 재도전)은 58일차에서 다룬다.

다음 단계에서는 이 생존 상태 처리 위에 방어 Command(52일차)를 연결한다.
