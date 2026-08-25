# Project Delta - 58일차 개발일지

## 작업 주제

**치명타 + 피해 유형별 방어 수치 — 기본 0%, 고정피해는 방어 무시, 상태이상은 저항**

---

## 개발 목표

기획서 4.2의 나머지 두 항목을 추가한다.

```text
최종 피해 = 기본 피해 × 95~105% 무작위 편차 × 치명타 배율 × 기타 보정
```

| 피해 유형 | 적용 방어 수치 |
| :-: | :-: |
| 일반 공격 / 직접 공격 스킬 | 방어력 |
| 상태 이상 | 저항 |
| 지속 피해 | 저항 또는 효과별 수치 |
| 고정 피해 | 방어력 무시 |
| 정력 피해 | 전용 계산식 |

치명타는 플레이어 기본 확률 0%, 무기·스킬·장비·유물·상태 효과로만 발생하고,
치명타 배율이 지정되지 않은 피해는 애초에 치명타가 발생하지 않는다.

지금은 기본 공격 하나뿐이라 실제로 상태 이상·지속 피해·고정 피해·치명타를
만들어내는 스킬이 없다. 이번 일차는 계산 로직과 분류 체계를 만들어두는
데 집중했고, 정력 피해(성인 이벤트 전투 전용 계산식)는 그 시스템이
생기기 전까지 범위에서 제외했다.

---

## 주요 작업 내용

### 1. DamageType 4분류 추가

`DamageType` enum(신규)을 추가했다.

```text
Normal          // 일반 공격·직접 공격 스킬 - 방어력 사용
StatusEffect    // 상태 이상 - 저항 사용
DamageOverTime  // 지속 피해 - 저항 또는 효과별 수치(기본값으로 저항 사용)
Fixed           // 고정 피해 - 방어력 무시
```

정력 피해는 전용 계산식이 필요해 이 목록에 넣지 않았다.

`CalculateBaseDamage`에 `damageType` 매개변수(기본값 `Normal`)를 추가하고,
내부에서 `GetDefenseValue()`로 피해 유형에 맞는 방어 수치를 고른다.
고정 피해는 방어 수치를 0으로 취급해 방어력을 완전히 무시한다.

### 2. 치명타 도입

치명타 판정용 상수 두 개를 추가했다.

```text
NoCriticalChancePercent = 0
NoCriticalMultiplierPercent = 0
```

`CanCriticalHit(criticalMultiplierPercent)`가 배율이 지정됐는지
(0보다 큰지)만 확인하고, `IsCriticalHit(...)`가 여기에 더해
`criticalRoll`(0~99 난수)이 확률보다 작은지 확인한다. 배율이 0이면
확률이 얼마든 무조건 `false`를 반환해 기획서 규칙을 그대로 지킨다.

`CalculateDamage`·`Resolve`에 `damageType`·`criticalChancePercent`·
`criticalMultiplierPercent`·`criticalRoll` 4개를 모두 기본값과 함께
추가했다. 기본 공격처럼 아무 값도 넘기지 않으면 `DamageType.Normal` +
치명타 불가로 자동 처리돼, 기존 호출부(`ExplorationMonsterEncounterController`)는
전혀 수정하지 않아도 됐다.

### 3. BattleDamageResult에 IsCritical 추가

디버그 표시·향후 UI 연결을 위해 치명타 발생 여부를 결과에 담았다.
`Resolve()`가 명중 판정 이후 `IsCriticalHit(...)`를 한 번 더 계산해
`BattleDamageResult.Hit(...)`에 전달한다(55일차에 `BaseDamage`·
`VariancePercent`를 디버그용으로 다시 계산해 담던 것과 같은 패턴).

### 4. EditMode 테스트 추가

- `CalculateBaseDamage_StatusEffectOrDamageOverTime_UsesResistanceInsteadOfDefense`
  — 상태 이상·지속 피해가 방어력이 아니라 저항을 쓰는지 확인 (`[TestCase]` 2개)
- `CalculateBaseDamage_Fixed_IgnoresDefense` — 고정 피해가 방어력을
  완전히 무시하는지 확인
- `CanCriticalHit_MultiplierNotSpecified_ReturnsFalseRegardlessOfChance`
- `IsCriticalHit_RollBelowChanceWithMultiplier_ReturnsTrue` /
  `RollAtOrAboveChance_ReturnsFalse` /
  `NoMultiplierSpecified_ReturnsFalseEvenIfRollWouldHit`
- `CalculateDamage_CriticalHit_AppliesMultiplierOnTopOfVariance` —
  편차 적용 후 치명타 배율이 곱해지는 순서 확인
- `CalculateDamage_DefaultParameters_NeverCritical` — 매개변수를
  생략한 기존 호출부가 절대 치명타가 아닌지 확인
- `Resolve` 테스트에 `IsCritical`이 `false`인지 확인하는 어서션 추가

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Application/BattleDamageCalculator.cs
Assets/ProjectDelta/Scripts/Application/BattleDamageResult.cs
Assets/ProjectDelta/Scripts/Application/DamageType.cs (신규)
Assets/ProjectDelta/Tests/EditMode/BattleDamageCalculatorTests.cs
```

---

## 남은 과제

- 지금은 기본 공격 하나뿐이라 `DamageType.StatusEffect`·`DamageOverTime`·
  `Fixed`와 치명타를 실제로 만들어내는 행동이 없다. 스킬 데이터가
  생기는 66일차 이후 실제로 연결된다.
- 정력 피해(성인 이벤트 전투 전용 계산식)는 이번 일차 범위에서 완전히
  제외했다. 해당 전투 시스템이 생길 때 별도로 설계해야 한다.
- "기타 보정"(최종 피해 공식의 마지막 항)은 아직 아무것도 곱하지 않는다.
  장비·유물 옵션이 생기면 연결이 필요하다.

Unity 에디터에서 재컴파일·테스트 실행 확인이 아직 진행되지 않았다.

---

## 다음 단계

4장(조우와 전투)의 정합성 회수 항목은 58일차로 마무리된다. 다음
일차부터는 단계 B(라운드와 상태 이상) — 라운드 파이프라인 확장과
StatusEffectDefinition/Instance 분리를 시작한다.
