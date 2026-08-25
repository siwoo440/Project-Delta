# Project Delta - 67일차 개발일지

## 작업 주제

**SkillBattleCommand 뼈대 구현 (선언 판정: 대상·자원 검사)**

---

## 개발 목표

66일차에 스킬 데이터(`SkillDefinition`)와 마나·정력 소모 API(`TrySpendMana`/`TrySpendStamina`)를 준비했다. 67일차는 `AttackBattleCommand`·`DefendBattleCommand` 옆에 세 번째 전투 명령인 `SkillBattleCommand`를 추가한다.

기존 Command들이 만들어진 순서를 그대로 따른다 — 49일차 `AttackBattleCommand`는 "대상이 유효한가"만 판정하고, 실제 명중·피해 계산은 다음 날(50일차)에 붙었다. `SkillBattleCommand`도 이번 일차에는 **선언이 유효한가(대상 유효성 + 자원 충분 여부)까지만** 판정하고, 실제 명중·피해·상태 부여·자원 차감은 다음 일차로 미룬다.

```text
SkillBattleCommand 뼈대 (IBattleCommand 구현)
대상 유효성 판정
마나·정력 충분 여부 판정 (아직 실제로 깎지 않음)
```

---

## 주요 작업 내용

### 1. SkillTargetType 추가 — 66일차에 빠져 있던 필드

`SkillBattleCommand.Execute()`가 "대상이 필요한 스킬인가"를 판정하려면 스킬 데이터에 그 정보가 있어야 하는데, 66일차 `SkillDefinition`에는 대상 종류 필드가 없었다. `ProjectDelta.Data.SkillTargetType`을 추가하고 `SkillDefinition`에 `TargetType` 필드로 연결했다.

```csharp
public enum SkillTargetType
{
    Enemy, // 상대 진영 중 살아있는 대상 하나를 선택해야 한다
    Self   // 대상 선택이 필요 없다 - 시전자 자신에게 적용 (DefendBattleCommand와 같은 방식)
}
```

지금은 아군이 Player 한 명뿐이라 Enemy/Self 둘이면 충분하다. 파티 시스템으로 아군이 여러 명 생기면 그때 Ally를 추가하면 된다.

### 2. SkillBattleCommand 구현

`AttackBattleCommand`(49일차)와 같은 형태의 `IBattleCommand` 구현체지만, **특정 스킬 하나를 나타내지 않고 어떤 `SkillDefinition`이든 받아 판정하는 범용 Command**로 만들었다. 생성자에서 `SkillDefinition`을 하나 받고, `Id`·`DisplayName`은 그 데이터에서 가져온다. 스킬이 몇 종류로 늘어나든 이 Command 클래스 하나로 전부 처리된다.

```text
Execute(context, actor, target) 판정 순서
  1. skill이 null이면 거부
  2. context·actor가 null이면 거부
  3. TargetType이 Enemy인데 유효한 대상이 아니면 거부 (BattleTargeting.IsValidTarget 재사용)
     - TargetType이 Self면 이 검사를 건너뛴다 (target이 null이어도 통과)
  4. actor.CurrentMana < skill.ManaCost 면 거부
  5. actor.CurrentStamina < skill.StaminaCost 면 거부
  6. 전부 통과하면 Accept
```

`AttackBattleCommand`가 `Execute()` 단계에서 실제로 데미지를 적용하지 않는 것과 같은 원칙으로, 여기서도 자원을 실제로 깎지 않고 "충분한가"만 확인한다. 실제 차감은 행동이 확정된 뒤(다음 일차에 만들 판정 로직)에서 이뤄져야 한다 — 그래야 대상 선택 화면에서 취소해도 자원이 낭비되지 않는다.

### 3. EditMode 테스트 추가

`SkillBattleCommandTests`를 새로 추가했다. `SkillDefinition`은 `StatusEffectDefinition`과 마찬가지로 `[SerializeField] private` 필드만 있는 ScriptableObject라 공개 생성자가 없다. `RoomEncounterPlacementTests`가 쓰던 것과 같은 방식(`ScriptableObject.CreateInstance` + private 필드 리플렉션 설정)으로 테스트용 스킬 데이터를 만들었다.

```text
Enemy 대상 스킬 - 유효한 대상 + 자원 충분 → Accept
Enemy 대상 스킬 - 아군(자기 자신)을 대상으로 지정 → Reject
Enemy 대상 스킬 - 대상 null → Reject
마나 부족 → Reject, 실제로는 마나가 깎이지 않았는지 확인
정력 부족 → Reject
Self 대상 스킬 - 대상 없이(null)도 Accept
skill이 null이어도 예외 없이 Reject
context가 null이면 Reject
```

---

## 변경 파일

```text
Assets/ProjectDelta/Scripts/Data/SkillTargetType.cs (신규)
Assets/ProjectDelta/Scripts/Data/SkillTargetType.cs.meta (신규)
Assets/ProjectDelta/Scripts/Data/SkillDefinition.cs

Assets/ProjectDelta/Scripts/Application/SkillBattleCommand.cs (신규)
Assets/ProjectDelta/Scripts/Application/SkillBattleCommand.cs.meta (신규)

Assets/ProjectDelta/Tests/EditMode/SkillBattleCommandTests.cs (신규)
Assets/ProjectDelta/Tests/EditMode/SkillBattleCommandTests.cs.meta (신규)
```

---

## 확인 사항

- `SkillTargetType`(Enemy/Self) 추가, `SkillDefinition.TargetType`으로 연결
- `SkillBattleCommand`가 `IBattleCommand`를 구현하고, 어떤 `SkillDefinition`이든 받는 범용 Command로 동작
- 대상 유효성은 기존 `BattleTargeting.IsValidTarget()`을 그대로 재사용(로직 중복 없음)
- Self 대상 스킬은 대상 선택 없이도 통과, Enemy 대상 스킬은 아군 지정·null 대상을 모두 거부
- 마나·정력이 부족하면 거부하되, `Execute()` 단계에서는 실제로 자원을 깎지 않음(취소 시 손해 없음)
- skill·context가 null이어도 예외 없이 안전하게 거부
- 새 EditMode 테스트 8개로 위 판정 분기를 모두 검증

Unity Editor에서의 실제 스크립트 컴파일과 Test Runner 통과 여부는 이 저장소 diff만으로는 확정할 수 없으므로, Unity Editor에서 EditMode Test Runner를 직접 실행해 최종 확인이 필요하다.

---

## 이번 일차 완료 상태

67일차 목표인 **SkillBattleCommand 뼈대(선언 판정)**를 구현했다. 이제 공격·방어·스킬 세 가지 전투 명령이 모두 `IBattleCommand`로 통일된 구조를 갖췄다.

---

## 다음 단계

`ConfirmAttack()`과 같은 자리에 `SkillDefinition` 하나를 받아 실제 명중·피해 계산(`BattleDamageCalculator.Resolve()`), 상태 부여(`StatusEffectApplicationService.TryApply()`), 추가 행동 부여(`BattleSession.TryGrantExtraAction()`), 실제 자원 차감(`TrySpendMana`/`TrySpendStamina`)까지 전부 연결하는 판정 로직을 만든다. 스킬이 데이터 기반이라 이 로직은 스킬별로 나눌 필요 없이 하나로 모든 스킬을 처리한다. 스킬 선택 UI도 이 로직이 갖춰진 뒤에 이어간다.
