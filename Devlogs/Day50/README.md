# Project Delta - 50일차 개발일지

## 개발 목표

49일차까지는 "누구를 공격할지" 지정·확정하는 구조만 있었고, 확정해도 아무 효과가 없었다.

이번 일차의 핵심 목표는 다음과 같다.

- 참가자에게 명중·회피·피해·관통 스탯 추가
- 명중률·피해량 계산 공식을 순수 함수로 분리
- 참가자 HP를 실제로 깎는 첫 API 추가
- `ConfirmAttack()`에 실제 판정·적용 연결

사망 판정·전투 이탈은 이번 일차에 포함하지 않는다(51일차).

---

## 구현 내용

### 1. BattleParticipant 전투 스탯 확장

`MaxHp / CurrentHp / Speed`만 있던 참가자 데이터에 다섯 개 전투 스탯을 추가했다.

```text
Attack       (공격력)
Defense      (방어력)
Accuracy     (명중)
Evasion      (회피)
Penetration  (관통)
```

생성자 매개변수가 늘어나면서 `ExplorationMonsterEncounterController`와 기존 테스트 5개 파일의 `BattleParticipant` 생성 코드를 모두 갱신했다. 테스트용 임시 스탯은 48~49일차와 같은 방식으로 상수로 고정했다.

```text
Player  Attack 6 / Defense 3 / Accuracy 90 / Evasion 10 / Penetration 0
Enemy   Attack 4 / Defense 2 / Accuracy 80 / Evasion  5 / Penetration 0
```

### 2. BattleParticipant.ApplyDamage 추가

지금까지 `CurrentHp`는 생성 시점에만 정해지고 줄일 방법이 없었다. 처음으로 HP를 깎는 API를 추가했다.

```text
ApplyDamage(amount)
→ 0 이하 피해는 무시
→ 남은 HP보다 큰 피해는 잘라냄 (CurrentHp는 0 밑으로 내려가지 않음)
→ 실제로 적용된 피해량을 반환
```

죽음 처리(전투 이탈 등)는 51일차에서 다루므로, 여기서는 HP만 줄이고 기존 `IsAlive`(`CurrentHp > 0`)가 자동으로 반영되는 것까지만 다룬다.

### 3. BattleDamageCalculator 추가

명중률·피해량 공식을 엔진에 의존하지 않는 정적 클래스로 분리했다.

```text
명중률(%) = 70(기본) + 공격자 Accuracy − 방어자 Evasion   (5~100%로 고정)
피해량   = 공격력 + 관통 − 방어력                          (최소 1 보장)
```

명중 판정은 밖에서 만든 0~99 난수(`roll0To99`)를 받아 `roll < 명중률`로만 비교한다. 난수 생성 자체는 계산기 책임이 아니므로, 실제 플레이에서는 Presentation 계층이 `UnityEngine.Random`으로 만들어 넘기고 테스트에서는 고정값을 넘겨 결정론적으로 검증한다.

```text
BattleDamageResult
├─ IsHit
├─ Damage
└─ HitChancePercent (판정에 쓰인 실제 명중률, 디버그 표시용)
```

### 4. ConfirmAttack()에 판정 연결

49일차에는 `AttackBattleCommand.Execute()`로 대상 유효성만 확인하고 끝났다. 50일차부터는 확정 직후 실제 판정을 수행한다.

```text
ConfirmAttack()
↓ AttackBattleCommand.Execute() — 대상 유효성 확인 (49일차)
↓ TryBeginResolveAction()
↓ UnityEngine.Random.Range(0,100) — 난수는 Presentation에서만 생성
↓ BattleDamageCalculator.Resolve(actor, target, roll) — 순수 계산 (Application)
↓ 명중 → target.ApplyDamage() 실제 적용
   → "공격 적중 / PLAYER → MON_TEST#1 / 6 데미지 (명중률 70%)"
↓ 빗나감
   → "공격 빗나감 / PLAYER → MON_TEST#1 (명중률 70%)"
↓ 이번 턴 마지막 행동자였다면 TurnEnd → 다음 TurnStart 자동 진행 (49일차와 동일)
```

계산은 여전히 `BattleDamageCalculator`(Application, 엔진 비의존)가 담당하고, 컨트롤러(Presentation)는 난수를 만들어 넘기고 결과를 적용하는 역할만 하도록 계층을 분리했다.

### 5. 화면 반영 (코드 변경 없음)

`BattleParticipantSlotView`가 매 프레임 `CurrentHp/MaxHp`를 다시 그리고 `IsAlive` 여부로 초상화 톤을 바꾸고 있었기 때문에, 별도 UI 코드 수정 없이도 공격이 적중하면 체력바가 즉시 줄고 HP가 0이 되면 초상화가 회색(사망 톤)으로 바뀐다. `BattleHudController`의 상태 텍스트도 이미 `LastBattleCommandResult.Message`를 그대로 표시하고 있어 적중/빗나감 메시지가 자동으로 나타난다.

---

## 50일차 전체 동작 흐름

```text
Test Advance → 대상 선택 (또는 Enemy는 자동 선택) → 공격 버튼
↓
ConfirmAttack()
↓
대상 유효성 확인 (49일차)
↓
명중 판정 (50일차, 신규)
↓
명중 → HP 실제 감소 + 결과 메시지
빗나감 → 결과 메시지만
↓
이번 턴 마지막 행동자였다면 다음 턴 자동 진행
```

---

## 테스트 추가

### BattleParticipantTests (신규)

- `ApplyDamage()`가 CurrentHp를 정확히 줄이고 실제 적용량을 반환
- 남은 HP보다 큰 피해는 0에서 잘리고, 이때도 실제 적용량(남은 HP만큼)을 반환
- 0 이하 피해는 아무 효과 없음
- 여러 번 공격 시 피해가 누적

### BattleDamageCalculatorTests (신규)

- 명중률 = 기본 + Accuracy − Evasion 계산 확인
- 명중률 하한(5%) · 상한(100%) 클램프 확인
- 피해량 = 공격력 + 관통 − 방어력 계산 확인
- 피해량 최소 1 보장 확인
- `Resolve()`가 roll이 명중률보다 작으면 명중(피해 포함), 명중률 이상이면 빗나감(피해 0)을 반환하는지 경계값으로 확인

---

## 이번 일차에서 제외한 내용

다음 내용은 이번 50일차에서 구현하지 않는다.

- 사망 판정 · 전투 이탈 (HP가 0이 되어도 아직 살아있는 것처럼 계속 행동 순서에 남을 수 있음 — 51일차에서 `BattleTurnOrder`의 `IsAlive` 필터링과 실제로 맞물리게 정리)
- 방어 Command (52일차)
- 크리티컬 · 상태이상 · 속성 상성 등 추가 보정
- 실제 플레이어·몬스터 스탯 연동 (여전히 테스트 상수 사용)
- 전투 로그 UI (상태 텍스트 한 줄 표시만 유지)

---

## 변경 파일

49일차 완료 커밋(`5528b3e`) 대비 이번 커밋에서 총 12개 파일이 추가·수정되었다.

### 생성

- `Assets/ProjectDelta/Scripts/Application/BattleDamageResult.cs`
- `Assets/ProjectDelta/Scripts/Application/BattleDamageCalculator.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleParticipantTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleDamageCalculatorTests.cs`

### 수정

- `Assets/ProjectDelta/Scripts/Application/BattleParticipant.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`
- `Assets/ProjectDelta/Tests/EditMode/AttackBattleCommandTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleContextTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleSessionTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleTargetingTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleTurnOrderTests.cs`

### 삭제

없음.

씬 파일은 이번 일차에 변경되지 않았다 — 전투 화면 레이아웃이나 참조 연결은 그대로이고, 계산 로직만 추가·연결됐기 때문에 `Project Delta > Day 47 > Build Battle HUD`를 다시 실행할 필요가 없다.

---

## 로컬 빌드 검증

GitHub CI가 구성되어 있지 않아, 47~49일차와 동일하게 로컬에서 각 어셈블리를 직접 빌드해 확인했다.

```text
dotnet build ProjectDelta.Application.csproj      → 오류 0개
dotnet build ProjectDelta.Presentation.csproj     → 오류 0개
dotnet build ProjectDelta.Editor.csproj           → 오류 0개
dotnet build ProjectDelta.Tests.EditMode.csproj   → 오류 0개
```

Unity Editor가 이미 실행 중인 상태였기 때문에 배치 모드를 통한 EditMode Test Runner 실행은 이번에도 수행하지 못했다. `BattleParticipantTests` · `BattleDamageCalculatorTests` 통과 여부와 실제 화면에서 공격 적중 시 체력바가 줄어드는지는 Unity Editor에서 직접 확인했다.

---

## 50일차 결과

49일차의 "누구를 공격할지" 위에, 50일차에서는 "그 공격이 실제로 맞는지, 맞으면 얼마나 아픈지"를 채웠다. `BattleDamageCalculator`가 명중률·피해량 공식을 엔진 비의존으로 계산하고, `BattleParticipant.ApplyDamage()`가 처음으로 HP를 실제로 줄이며, `ConfirmAttack()`이 이 둘을 연결해 화면에 즉시 반영되도록 했다.

다만 HP가 0이 되어도 아직 "죽음"으로 처리되지는 않는다. 다음 단계에서는 이 위에 실제 사망 판정과 전투 이탈(51일차)을 연결한다.
