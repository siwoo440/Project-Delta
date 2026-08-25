# Project Delta - 65일차 개발일지

## 작업 주제

**강화·약화 상태의 능력치 보정을 실제 전투 계산에 연결**

---

## 개발 목표

64일차가 지속 피해·기절·추가 행동·전투 종료 정리로 "상태가 전투 규칙을 바꾸는 것"의 첫 갈래를 끝냈다면, 65일차는 남은 갈래인 **강화·약화 상태의 능력치 보정**을 연결하는 단계다.

62일차에 만든 강화 상태 6종(공격·방어·속도·명중·회피·저항 상승) 에셋과 약화·둔화 2종은 64일차까지 `StatusEffectKind.Neutral`로 분류돼 있어, 실제로 걸려도 전투 계산에는 아무 영향이 없었다. 이번 일차는 이 상태들이 실제로 명중률·피해량·행동 순서에 반영되도록 만든다.

수치는 정확한 기획 문서 확인 전이라 **전부 임의값(placeholder)**으로 채웠다. 이후 밸런스 수치가 확정되면 에셋 값만 바꾸면 된다.

---

## 주요 작업 내용

### 1. BattleStatType·StatusEffectKind.StatModifier 도입

전투 능력치 6종(공격·방어·속도·명중·회피·저항)을 나타내는 `ProjectDelta.Data.BattleStatType`을 새로 추가했다. 매력(Charm)은 이를 보정하는 상태가 아직 없어 제외했다.

`StatusEffectKind`에 `StatModifier`를 새로 추가했다 — **반드시 마지막 값으로만** 추가했다. 기존 16종 상태 에셋이 이 enum을 정수로 직렬화해 저장하고 있어서, 중간에 끼워 넣으면 `Stun`(3) 같은 기존 값이 다른 의미로 깨진다.

```text
Neutral, DamageOverTime, HealOverTime, Stun, ExtraAction, StatModifier
```

`StatusEffectDefinition`에 `TargetStat`(어떤 능력치를 보정하는지)과 `StatModifierValue`(보정치, 양수 강화·음수 약화)를 추가했다.

### 2. StatusEffectInstance·StatusEffectApplicationService 확장

`StatusEffectInstance` 생성자에 `TargetStat`을 추가했다. 기존 생성자 호출부(64일차까지의 프로덕션·테스트 코드)를 다시 건드리지 않기 위해 **기본값 있는 선택 인자**로 맨 끝에 붙였다 — `StatModifier`가 아닌 상태에서는 어차피 읽히지 않는 값이라 기본값이 안전하다.

`StatusEffectApplicationService.TryApply()`도 같은 방식으로 `targetStat` 선택 인자를 raw 오버로드 맨 끝에 추가했고, `StatusEffectDefinition` 기반 오버로드는 `definition.TargetStat`을 자동으로 전달하도록 연결했다.

또한 상태 적용 성공률 계산(`CalculateFinalSuccessChance`)에서 대상의 저항을 구할 때도 기본 `Resistance`가 아니라 저항 상승이 반영된 유효 저항을 쓰도록 고쳤다 — 저항 상승 상태가 걸려 있으면 상태 이상에도 더 잘 저항해야 자연스럽기 때문이다.

### 3. BattleStatModifierService 신규 추가

```csharp
public static class BattleStatModifierService
{
    public static int GetEffectiveAttack(BattleParticipant participant)
    public static int GetEffectiveDefense(BattleParticipant participant)
    public static int GetEffectiveSpeed(BattleParticipant participant)
    public static int GetEffectiveAccuracy(BattleParticipant participant)
    public static int GetEffectiveEvasion(BattleParticipant participant)
    public static int GetEffectiveResistance(BattleParticipant participant)
}
```

`BattleParticipant`의 기본 스탯 값 자체는 바꾸지 않고("무엇이 기본값인가"는 그대로 유지), 계산이 필요한 지점에서 "기본값 + 만료되지 않은 StatModifier 상태의 합"을 구해 돌려준다. 64일차 지속 피해와 동일하게 `StackCount`를 곱해 중첩을 반영한다(현재 강화·약화 상태는 전부 `NoStack`/`RefreshDuration`이라 항상 1이지만, 이후 중첩형 강화 상태가 추가돼도 그대로 동작한다). 결과가 음수로 내려가면 0으로 고정한다.

### 4. 전투 계산 파이프라인에 연결

- `BattleDamageCalculator.CalculateHitChancePercent()` — 공격자 명중, 방어자 회피를 유효 스탯으로 교체
- `BattleDamageCalculator.CalculateBaseDamage()`/`GetDefenseValue()` — 공격력·방어력·저항을 유효 스탯으로 교체
- `BattleDamageCalculator.CalculateDefendReductionPercent()` — 방어 감소율 계산의 방어력을 유효 스탯으로 교체
- `BattleTurnOrder.Build()` — 행동 순서 정렬 기준을 유효 Speed로 교체

`BattleParticipant`가 직접 계산에 쓰이던 자리를 전부 `BattleStatModifierService` 호출로 바꿨을 뿐이라, 상태 이상이 없는 기존 호출(63~64일차까지의 모든 테스트)은 동일한 결과를 낸다.

### 5. 기존 16종 상태 에셋에 임의 수치 반영

| 상태 | EffectKind | TargetStat | 값(임의) |
| --- | --- | --- | --- |
| 공격 상승 | StatModifier | Attack | +5 |
| 방어 상승 | StatModifier | Defense | +5 |
| 속도 상승 | StatModifier | Speed | +3 |
| 명중 상승 | StatModifier | Accuracy | +10 |
| 회피 상승 | StatModifier | Evasion | +10 |
| 저항 상승 | StatModifier | Resistance | +10 |
| 약화 | StatModifier | Attack | -5 |
| 둔화 | StatModifier | Speed | -3 |

혼란·침묵·구속·매혹 4종은 스탯 보정이 아니라 스킬 사용 제약형 효과라 이번 일차 범위에서 제외했고, 계속 `Neutral`로 남겨뒀다 (스킬 Command 시스템이 생기는 66~67일차 이후 대상).

### 6. EditMode 테스트 추가

```text
BattleStatModifierServiceTests (신규)
  - 활성 상태 없으면 기본값 그대로
  - 강화 상태가 있으면 보정치만큼 증가
  - 약화 상태가 있으면 보정치만큼 감소
  - 만료된 상태는 무시
  - 다른 능력치를 대상으로 하는 상태는 무시
  - EffectKind가 StatModifier가 아니면 AppliedValue·TargetStat이 같아도 무시
  - 속도 감소가 0 아래로 내려가지 않음
  - 같은 능력치에 여러 상태가 걸리면 누적

BattleDamageCalculatorTests (확장)
  - 공격 상승이 실제 기본 피해 계산에 반영됨
  - 방어 상승이 실제 받는 피해를 줄임
  - 회피 상승이 실제 명중률을 낮춤

BattleTurnOrderTests (확장)
  - 속도 상승으로 원래 더 느린 참가자가 더 빠른 참가자를 앞지를 수 있음
  - 둔화로 원래 더 빠른 참가자가 더 느린 참가자에게 뒤처질 수 있음
```

기존 `BattleDamageCalculatorTests`·`BattleTurnOrderTests`·`StatusEffectApplicationServiceTests`·`StatusEffectInstanceTests`는 상태 이상을 쓰지 않는 호출부라 수정 없이 그대로 통과해야 한다 (선택 인자로 확장했기 때문에 기존 시그니처와 호환된다).

---

## 변경 파일

```text
Assets/ProjectDelta/Scripts/Data/BattleStatType.cs (신규)
Assets/ProjectDelta/Scripts/Data/BattleStatType.cs.meta (신규)
Assets/ProjectDelta/Scripts/Data/StatusEffectKind.cs
Assets/ProjectDelta/Scripts/Data/StatusEffectDefinition.cs

Assets/ProjectDelta/Scripts/Application/BattleStatModifierService.cs (신규)
Assets/ProjectDelta/Scripts/Application/BattleStatModifierService.cs.meta (신규)
Assets/ProjectDelta/Scripts/Application/StatusEffectInstance.cs
Assets/ProjectDelta/Scripts/Application/StatusEffectApplicationService.cs
Assets/ProjectDelta/Scripts/Application/BattleDamageCalculator.cs
Assets/ProjectDelta/Scripts/Application/BattleTurnOrder.cs

Assets/ProjectDelta/Data/StatusEffects/*.asset (16종 전체, targetStat·statModifierValue 필드 추가)
Assets/ProjectDelta/Data/StatusEffects/{AccuracyUp,AttackUp,DefenseUp,EvasionUp,ResistanceUp,SpeedUp,Weakness,Slow}.asset (effectKind → StatModifier)

Assets/ProjectDelta/Tests/EditMode/BattleStatModifierServiceTests.cs (신규)
Assets/ProjectDelta/Tests/EditMode/BattleDamageCalculatorTests.cs
Assets/ProjectDelta/Tests/EditMode/BattleTurnOrderTests.cs
```

---

## 확인 사항

- `BattleStatType`·`StatusEffectKind.StatModifier` 추가, 기존 enum 값은 순서를 바꾸지 않아 기존 에셋 데이터가 깨지지 않음
- `StatusEffectInstance`·`StatusEffectApplicationService.TryApply()`에 `TargetStat` 연결, 기존 호출부는 선택 인자 기본값으로 그대로 컴파일됨
- `BattleStatModifierService` 신규 추가 — 만료 여부·EffectKind·TargetStat을 모두 확인 후 합산, 결과 0 미만 방지
- `BattleDamageCalculator`(명중률·기본 피해·방어 감소율)와 `BattleTurnOrder`(행동 순서)가 유효 스탯을 쓰도록 연결
- 상태 적용 성공률 계산도 저항 상승이 반영된 유효 저항을 쓰도록 연결
- 강화 상태 6종 + 약화·둔화 2종에 임의 수치 반영, 나머지 4종(혼란·침묵·구속·매혹)은 범위 제외
- 새 EditMode 테스트로 보정 누적·만료 무시·다른 스탯 무시·0 미만 방지·실제 계산 반영을 검증
- 상태 이상이 없는 기존 호출은 결과가 그대로라 63~64일차 테스트에 영향 없음

Unity Editor에서의 실제 스크립트 컴파일과 Test Runner 통과 여부는 이 저장소 diff만으로는 확정할 수 없으므로, Unity Editor에서 EditMode Test Runner를 직접 실행해 최종 확인이 필요하다.

---

## 이번 일차 완료 상태

65일차 목표인 **강화·약화 상태의 능력치 보정을 실제 전투 계산에 연결**을 구현했다. 62일차에 데이터만 준비돼 있던 강화 상태 6종과 약화·둔화 2종이 이제 실제로 명중률·피해량·행동 순서를 바꾼다.

---

## 다음 단계

수치는 전부 임의값이므로, 기획 문서 기준 정확한 밸런스 수치가 확정되면 각 상태 에셋의 `statModifierValue`만 조정하면 된다 (계산 파이프라인 재수정 불필요). 혼란·침묵·구속·매혹 4종과 실제 스킬 데이터·Command 연결은 66~67일차 스킬 시스템에서 이어간다.
