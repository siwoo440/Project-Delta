# Project Delta - 68일차 개발일지

## 작업 주제

**스킬 판정 로직 연결 (ConfirmSkill: 명중·피해·상태 부여·추가 행동·자원 차감)**

---

## 개발 목표

67일차에 `SkillBattleCommand`로 "선언이 유효한가"까지만 판정하는 뼈대를 만들었다. 68일차는 예고한 대로 그 뒤를 잇는 **실제 판정 로직**을 연결한다. `ConfirmAttack()`이 49일차 뼈대에 50일차 판정을 붙였던 순서를 그대로 따른다.

```text
SkillDamageType/SkillDefenseInteraction → Application enum 매핑
BattleDamageCalculator에 스킬 자체 보정치(명중·피해 배율) 연결
ConfirmSkill(): 명중·피해 계산 + 상태 부여 + 추가 행동 + 실제 자원 차감
```

---

## 주요 작업 내용

### 1. SkillEffectMapping — Data 계층 enum을 Application 계층 enum으로

66일차에 asmdef 참조 방향(Application → Data) 때문에 `SkillDamageType`·`SkillDefenseInteraction`을 `ProjectDelta.Data`에 따로 만들어뒀다. 이제 실제로 `BattleDamageCalculator`(Application 계층)를 호출할 차례가 되어, 그 값을 옮겨주는 작은 매핑 계층 `SkillEffectMapping`을 추가했다.

```csharp
DamageType SkillEffectMapping.ToDamageType(SkillDamageType)
DefenseInteraction SkillEffectMapping.ToDefenseInteraction(SkillDefenseInteraction)
```

두 enum 모두 값 이름과 순서가 완전히 같아서 매핑은 단순한 1:1 변환이다.

### 2. BattleDamageCalculator에 스킬 자체 보정치 연결

`SkillDefinition.AccuracyModifierPercent`·`DamageMultiplierPercent`를 실제로 반영할 자리가 계산 함수 자체에는 없었다. `CalculateHitChancePercent()`·`CalculateDamage()`·`Resolve()`에 각각 선택 인자를 추가했다.

```text
CalculateHitChancePercent(attacker, defender, skillAccuracyModifierPercent = 0)
  → 기본 70 + 명중 - 회피×50% + skillAccuracyModifierPercent

CalculateDamage(..., skillDamageMultiplierPercent = 100)
  → 기본 피해 × 편차% × skillDamageMultiplierPercent% × (치명타) × (방어 감소)
```

둘 다 **기본값이 있는 선택 인자**로 맨 끝에 붙여서, 기존 `AttackBattleCommand`·모든 기존 테스트 호출부는 손대지 않고 그대로 컴파일된다(기본 공격은 보정치 0·배율 100%를 넘기는 것과 동일해 결과도 그대로다).

### 3. ConfirmSkill() — 실제 판정 로직

`ExplorationMonsterEncounterController`에 `ConfirmAttack()`·`ConfirmDefend()`와 같은 자리에 `ConfirmSkill(SkillDefinition skill)`을 추가했다. 스킬마다 별도 메서드가 필요 없도록 데이터 하나만 받아 전부 처리하는 범용 메서드다.

```text
1. SkillBattleCommand.Execute()로 선언 판정 (67일차)
2. 통과하면 TryBeginResolveAction()
3. actor.TrySpendMana / TrySpendStamina로 실제 자원 차감 (66일차 API를 여기서 처음 씀)
4. TargetType이 Self면:
   - 명중 판정 없이 항상 적용 (자기 자신에게 쓰는 버프는 "빗나가지" 않는다)
   - GrantedStatusEffect가 있으면 StatusEffectApplicationService.TryApply()로 자신에게 적용
5. TargetType이 Enemy면:
   - CombatRng로 명중·편차·치명타 굴림
   - BattleDamageCalculator.Resolve()에 스킬 보정치·매핑된 DamageType·DefenseInteraction을 넘겨 판정
   - 명중했으면 피해 적용 + GrantedStatusEffect가 있으면 대상에게 적용
6. GrantsExtraAction이면 명중 여부와 무관하게 BattleSession.TryGrantExtraAction() 호출
   (스킬 사용 자체가 성공했다는 뜻이므로)
7. 전투 종료 판정 → 다음 행동자 자동 진행 (ConfirmAttack과 동일한 꼬리 구조)
```

63~65일차에 만들어놓고 아무도 안 부르던 `StatusEffectApplicationService.TryApply()`와 64일차에 만든 `BattleSession.TryGrantExtraAction()`이 여기서 처음 실전 투입된다.

상태 부여 로직(상태가 있으면 시도하고 성공/실패 문구를 만드는 부분)은 Self/Enemy 두 분기에서 똑같이 반복되므로 `ApplyGrantedStatusEffectIfAny()`라는 사설 메서드로 뽑아냈다.

`ConfirmAttack()`/`ConfirmDefend()`가 이미 "판정 후 다음 행동자로 자동 진행" 꼬리 부분을 각자 중복해서 가지고 있던 것과 같은 방식으로, `ConfirmSkill()`도 같은 형태를 그대로 반복했다 — 기존 코드 스타일을 새로 바꾸는 대신 그대로 따랐다.

### 4. EditMode 테스트 추가

```text
BattleDamageCalculatorTests (확장)
  - 스킬 명중 보정치가 실제로 명중률에 더해짐
  - 매개변수 생략 시 기존 기본 공격 결과와 동일
  - 스킬 피해 배율이 실제로 피해량에 곱해짐
  - 매개변수 생략 시 기존 기본 공격 결과와 동일
  - Resolve()에서 두 보정치가 함께 작동 (빗나갈 굴림이 명중 보정으로 명중, 배율로 피해 증가)

SkillEffectMappingTests (신규)
  - SkillDamageType 4종 → DamageType 1:1 매핑 확인
  - SkillDefenseInteraction 3종 → DefenseInteraction 1:1 매핑 확인
```

`ConfirmSkill()` 자체는 `MonoBehaviour` 기반 Presentation 컨트롤러라, 기존 `ConfirmAttack()`/`ConfirmDefend()`와 마찬가지로 전용 자동화 테스트는 두지 않았다(이 프로젝트에서 PlayMode 테스트는 부팅 시나리오 하나뿐이다). 대신 그 안에서 쓰는 순수 계산 로직(`BattleDamageCalculator`, `SkillEffectMapping`)은 EditMode 테스트로 촘촘히 검증했다.

---

## 변경 파일

```text
Assets/ProjectDelta/Scripts/Application/BattleDamageCalculator.cs
Assets/ProjectDelta/Scripts/Application/SkillEffectMapping.cs (신규)
Assets/ProjectDelta/Scripts/Application/SkillEffectMapping.cs.meta (신규)
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs

Assets/ProjectDelta/Tests/EditMode/BattleDamageCalculatorTests.cs
Assets/ProjectDelta/Tests/EditMode/SkillEffectMappingTests.cs (신규)
Assets/ProjectDelta/Tests/EditMode/SkillEffectMappingTests.cs.meta (신규)
```

---

## 확인 사항

- `SkillEffectMapping`으로 Data 계층 스킬 enum을 Application 계층 계산 enum으로 변환
- `BattleDamageCalculator`에 스킬 명중 보정·피해 배율 선택 인자 추가, 생략 시 기존 기본 공격과 동일한 결과 유지
- `ConfirmSkill()`에서 명중·피해·상태 부여·추가 행동·자원 차감을 전부 연결, 스킬 종류와 무관하게 데이터 하나로 동작
- Self 대상 스킬은 명중 판정 없이 항상 적용, Enemy 대상 스킬은 공격과 동일한 방식으로 명중·피해 판정
- 63~65일차 상태 적용 서비스, 64일차 추가 행동 API, 66일차 자원 소모 API가 모두 이번에 처음 실전 연결됨
- 새 EditMode 테스트로 계산 로직(명중 보정·피해 배율·enum 매핑)을 검증
- `ConfirmSkill()` 자체는 기존 컨트롤러 관례대로 전용 자동화 테스트 없음(Presentation 계층, PlayMode 테스트 부재)

Unity Editor에서의 실제 스크립트 컴파일과 Test Runner 통과 여부는 이 저장소 diff만으로는 확정할 수 없으므로, Unity Editor에서 EditMode Test Runner를 직접 실행해 최종 확인이 필요하다.

---

## 이번 일차 완료 상태

68일차 목표인 **스킬 판정 로직 연결**을 구현했다. 이제 공격·방어·스킬 세 전투 명령이 모두 선언부터 실제 판정까지 완전히 동작한다. "전투 명령 완성" 단계의 핵심 로직이 여기서 마무리됐다.

---

## 다음 단계

스킬 선택 UI(`BattleHudController`의 남은 행동 버튼 자리)를 실제 스킬 목록에 연결하고, 아이템 사용·도주·유혹(성인 이벤트) Command로 이어간다. 실제 스킬 콘텐츠(이름·수치)가 기획 문서 기준으로 확정되면 `Data/Skills`에 에셋을 채워 넣는 것도 남은 일이다.
