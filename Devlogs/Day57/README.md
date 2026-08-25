# Project Delta - 57일차 개발일지

## 작업 주제

**방어 감소율 곡선 + 방어 가능·관통·불가 구분**

---

## 개발 목표

52일차부터 고정 50%였던 방어 피해 감소율을 기획서 4.2가 정의하는 방어력
기반 곡선으로 바꾸고, 방어 감소율이 피해 종류에 따라 다르게 적용되는
"방어 가능·관통·불가" 3분류를 도입한다.

```text
방어 피해 감소율 = 30% + 방어력 ÷ (방어력 + 100) × 30%
최종 감소율은 최대 60%를 넘지 않는다.
```

| 스킬 설정 | 처리 |
| :-: | :-: |
| 방어 가능 | 방어 감소율 전체 적용 |
| 방어 관통 | 일부 감소율만 적용 |
| 방어 불가 | 방어 감소율 적용하지 않음 |

---

## 주요 작업 내용

### 1. 방어 감소율 곡선 도입

`DefendDamageReductionPercent`(고정 50%) 상수를 제거하고, 세 상수로
나눠 곡선을 표현했다.

```text
DefendBaseReductionPercent = 30
DefendVariableReductionScalePercent = 30
DefendMaxReductionPercent = 60
```

`CalculateDefendReductionPercent(defender)`를 추가했다.

```text
variablePercent = defender.Defense * 30 / (defender.Defense + 100)
reductionPercent = 30 + variablePercent, 최대 60으로 고정
```

기획서 예상 감소율 표(방어력 25→36%, 50→40%, 100→45%, 200→50%)와
정확히 일치하는 걸 확인했다.

### 2. DefenseInteraction 3분류 추가

`DefenseInteraction` enum(신규)을 추가했다.

```text
Defendable       // 방어 가능 - 감소율 전체 적용
PenetratesDefense // 방어 관통 - 감소율 일부만 적용
IgnoresDefense    // 방어 불가 - 감소율 적용 안 함
```

`CalculateDamage`·`Resolve`에 `defenseInteraction` 매개변수를 추가했다.
기본값은 `Defendable`이라, 지금 하나뿐인 기본 공격은 기존 호출부를 고치지
않아도 자동으로 "방어 가능" 취급된다.

방어 관통의 "일부 감소율"은 정확한 비율이 문서에 없어, 56일차 회피
가중치와 같은 50%를 임시값으로 썼다(`PenetratingDefenseReductionWeightPercent`).
실제 관통 스킬이 생기는 66일차 이후 재검토가 필요하다.

### 3. EditMode 테스트 추가·갱신

방어 중 감소 테스트 2개의 기대값을 고정 50% 기준(5)에서 방어력 0의 곡선
값(30%, 결과 7)으로 갱신했다.

새로 추가한 테스트:

- `CalculateDefendReductionPercent_MatchesPlanningDocTable` — 방어력
  25/50/100/200에서 기획서 표(36/40/45/50%)와 정확히 일치하는지 확인
  (`[TestCase]` 4개)
- `CalculateDefendReductionPercent_NeverExceedsSixtyPercent` — 방어력을
  극단적으로 높여도 60%를 넘지 않는지 확인
- `CalculateDamage_PenetratesDefense_AppliesOnlyPartialReduction` — 방어
  관통 시 감소율이 절반만 적용되는지 확인 (30% × 50% = 15%)
- `CalculateDamage_IgnoresDefense_AppliesNoReductionEvenWhileDefending` —
  방어 불가 피해는 방어 중이어도 그대로 들어가는지 확인

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Application/BattleDamageCalculator.cs
Assets/ProjectDelta/Scripts/Application/DefenseInteraction.cs (신규)
Assets/ProjectDelta/Tests/EditMode/BattleDamageCalculatorTests.cs
```

---

## 남은 과제

- "방어 관통"의 정확한 감소율 비율이 기획서에 명시돼 있지 않아 임시로
  50%를 썼다. 실제 스킬 데이터가 생기면(66일차 이후) 확정해야 한다.
- 지금은 기본 공격 하나뿐이라 `DefenseInteraction.PenetratesDefense`·
  `IgnoresDefense`를 실제로 만들어내는 행동이 없다. 고정 피해·상태 이상
  등 다른 피해 유형이 생기는 58일차 이후에 실제로 연결된다.
- 치명타·피해 유형별 방어 수치(고정 피해는 방어 무시, 상태이상은 저항)는
  58일차에서 다룬다.

Unity 에디터에서 재컴파일·테스트 실행 확인이 아직 진행되지 않았다.

---

## 다음 단계

58일차에서는 치명타와 피해 유형별 방어 수치를 도입한다 — 기본 치명타
확률 0%, 고정 피해는 방어 무시, 상태이상은 저항을 사용한다.
