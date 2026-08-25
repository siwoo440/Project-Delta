# Project Delta - 66일차 개발일지

## 작업 주제

**스킬 데이터 정의(SkillDefinition)와 마나·정력 소모 API 구현**

---

## 개발 목표

지금까지 전투에서 실제로 쓸 수 있는 행동은 `AttackBattleCommand`(공격)와 `DefendBattleCommand`(방어) 둘뿐이었다. 65일차까지 만든 상태 이상·능력치 보정·추가 행동 시스템을 실제로 발동시키는 세 번째 전투 명령인 **스킬**을 추가하는 것이 이번 단계("전투 명령 완성")의 목표다.

66일차는 스킬 Command 로직을 바로 만들지 않고, 그 로직이 읽을 **데이터**와 소모할 **자원 API**부터 준비한다. 61~62일차가 상태 이상 데이터를 먼저 만들고 63일차에 로직을 붙였던 순서와 같은 흐름이다.

```text
SkillDefinition 데이터 정의
마나·정력 소모 API
```

실제 스킬 실행 로직(SkillBattleCommand), 스킬 선택 UI, 혼란·침묵·구속·매혹 4종의 실제 제약 효과는 이후 일차에서 처리한다.

---

## 주요 작업 내용

### 1. SkillDefinition 데이터 정의

`Data/Skills` 폴더는 이미 자리만 만들어져 있었다(빈 `.gitkeep`). `StatusEffectDefinition`과 같은 패턴으로 `SkillDefinition` ScriptableObject를 추가했다.

필드는 전부 **이미 만들어져 있는 계산 함수가 받을 수 있는 값**을 그대로 옮긴 것이다.

```text
Display: displayName

Cost: manaCost, staminaCost
  → 66일차에 추가한 BattleParticipant.TrySpendMana/TrySpendStamina가 검사한다.

Damage: damageMultiplierPercent, damageType, defenseInteraction,
        accuracyModifierPercent, criticalChancePercent, criticalMultiplierPercent
  → BattleDamageCalculator.Resolve()가 58일차부터 이미 받을 수 있던 매개변수들이다.

Status Effect: grantedStatusEffect, statusEffectBaseChancePercent,
               statusEffectDurationRounds, statusEffectAppliedValue
  → StatusEffectApplicationService.TryApply(target, definition, ...)에 그대로 넘길 값이다.

Extra Action: grantsExtraAction
  → BattleSession.TryGrantExtraAction()을 부를지 여부.
```

즉 66일차는 새 계산 규칙을 만든 게 아니라, 58~65일차에 이미 만들어 둔 계산·적용 함수들의 **입력값을 데이터로 표현**하는 작업이다.

### 2. SkillDamageType·SkillDefenseInteraction — 왜 기존 enum을 그대로 안 썼는가

`ProjectDelta.Application.DamageType`·`DefenseInteraction`이 이미 있었지만 그대로 재사용하지 않고 `ProjectDelta.Data`에 같은 값의 enum을 새로 만들었다. asmdef를 확인해보니 이유가 명확했다.

```text
ProjectDelta.Data.asmdef      references: [ProjectDelta.Domain]
ProjectDelta.Application.asmdef  references: [ProjectDelta.Data, ProjectDelta.Domain]
```

`ProjectDelta.Data` 어셈블리는 `ProjectDelta.Application`을 참조하지 않는다(참조 방향이 반대). Data 계층 스크립트가 Application 계층 enum을 직접 쓰면 컴파일 에러가 난다. 62~65일차에 만든 `StatusDurationType`·`StatusStackRule`·`StatusEffectKind`·`BattleStatType`도 전부 Data 계층에 새로 만든 것과 같은 이유·같은 패턴이다.

실제 값을 계산에 사용하는 시점(다음 일차에 만들 SkillBattleCommand)에서 `SkillDamageType → Application.DamageType`으로 옮겨 쓰는 작은 매핑 한 단계가 필요하지만, 계층 의존 방향을 지키는 대가로는 작다.

### 3. BattleParticipant 자원 소모 API

54일차에 마나·정력 그릇만 만들어두고 실제로 깎는 API가 없었다. `ApplyDamage`/`Heal`과 같은 자리에 대칭되는 API를 추가했다.

```csharp
public bool TrySpendMana(int amount)
public bool TrySpendStamina(int amount)
```

`ApplyDamage`(일부만 깎고 실제 적용량 반환)와 달리, 자원 소모는 "충분하면 전액 차감, 모자라면 아예 실패"라는 전부-아니면-전무 규칙이라 `bool`을 반환한다. 소모량이 0 이하면 항상 성공(변화 없음), 모자라면 실패하고 아무것도 바꾸지 않는다.

### 4. EditMode 테스트 추가

```text
BattleParticipantTests (확장)
  - 마나가 충분하면 정확히 차감되고 성공
  - 마나가 부족하면 실패하고 변화 없음
  - 소모량 0 이하는 항상 성공(무변화)
  - 남은 마나와 정확히 같은 양을 소모하면 0까지 도달
  - 정력도 마나와 동일한 규칙으로 동작 (충분/부족 각각 확인)
```

`SkillDefinition`은 `StatusEffectDefinition`과 마찬가지로 `[SerializeField] private` 필드만 있는 순수 데이터 ScriptableObject라, 프로젝트의 기존 관례대로 별도 단위 테스트는 두지 않았다(`StatusEffectDefinition`도 전용 테스트 파일이 없다).

---

## 변경 파일

```text
Assets/ProjectDelta/Scripts/Data/SkillDefinition.cs (신규)
Assets/ProjectDelta/Scripts/Data/SkillDefinition.cs.meta (신규)
Assets/ProjectDelta/Scripts/Data/SkillDamageType.cs (신규)
Assets/ProjectDelta/Scripts/Data/SkillDamageType.cs.meta (신규)
Assets/ProjectDelta/Scripts/Data/SkillDefenseInteraction.cs (신규)
Assets/ProjectDelta/Scripts/Data/SkillDefenseInteraction.cs.meta (신규)

Assets/ProjectDelta/Scripts/Application/BattleParticipant.cs

Assets/ProjectDelta/Tests/EditMode/BattleParticipantTests.cs
```

`Data/Skills` 폴더에는 아직 실제 스킬 에셋을 넣지 않았다. 구체적인 스킬 목록(이름·수치)이 기획 문서에서 확정되기 전에 임의로 만들어 넣는 것은 이번 범위에서 제외했다 — 62일차가 실제 상태 이상 16종을 문서 기준으로 만들었던 것과 달리, 스킬은 아직 확정된 목록이 없기 때문이다.

---

## 확인 사항

- `SkillDefinition`에 마나·정력 소모, 피해 배율·유형·방어 상호작용, 명중·치명타 보정, 상태 부여, 추가 행동 부여 여부를 담음
- 새 필드는 전부 기존 계산 함수(`BattleDamageCalculator.Resolve()`, `StatusEffectApplicationService.TryApply()`, `BattleSession.TryGrantExtraAction()`)가 이미 받을 수 있는 값과 1:1 대응
- `SkillDamageType`·`SkillDefenseInteraction`을 Data 계층에 별도로 둬 asmdef 참조 방향(Application → Data)을 지킴
- `BattleParticipant.TrySpendMana`/`TrySpendStamina` 추가, 부족하면 실패하고 상태를 바꾸지 않음
- 새 EditMode 테스트로 자원 소모 성공/실패/경계값(정확히 소진)을 검증
- 실제 스킬 에셋(콘텐츠)과 SkillBattleCommand 로직은 이번 범위에서 제외

Unity Editor에서의 실제 스크립트 컴파일과 Test Runner 통과 여부는 이 저장소 diff만으로는 확정할 수 없으므로, Unity Editor에서 EditMode Test Runner를 직접 실행해 최종 확인이 필요하다.

---

## 이번 일차 완료 상태

66일차 목표인 **스킬 데이터 정의와 마나·정력 소모 API**를 구현했다. "전투 명령 완성" 단계의 첫 조각으로, 58~65일차에 만들어 둔 계산·적용 함수들을 실제로 부를 준비가 됐다.

---

## 다음 단계

`AttackBattleCommand` 옆에 `SkillBattleCommand`를 추가해 `SkillDefinition`을 읽고 자원을 소모한 뒤, `BattleDamageCalculator.Resolve()`·`StatusEffectApplicationService.TryApply()`·`BattleSession.TryGrantExtraAction()`을 실제로 호출하는 실행 로직을 연결한다. 스킬 선택 UI와 혼란·침묵·구속·매혹 4종의 실제 효과도 이 로직이 갖춰진 뒤에 이어간다.
