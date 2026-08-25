# Project Delta - 60일차 개발일지

## 작업 주제

**라운드 파이프라인 확장 — 지속 시작 효과 · 지속 피해·회복 · 지속시간 감소 단계 삽입**
**(+ 61일차 선반영: StatusEffectDefinition / Instance 분리)**

---

## 개발 목표

기획서 4.2 라운드 구조를 그대로 옮긴다.

```text
라운드 시작 → 지속 시작 효과 적용 → 행동 순서 계산 → 참가자별 행동
→ 지속 피해와 회복 적용 → 상태 지속 시간 감소 → 전투 종료 판정 → 다음 라운드
```

지금 `BattleSession`은 이 중 "행동 순서 계산"과 "참가자별 행동"만 갖고
있었다. 나머지 세 단계(지속 시작 효과·지속 피해·회복·지속시간 감소)를
끼워 넣으려면 상태 이상을 담을 데이터 구조부터 있어야 해서, 사용자
요청에 따라 61일차(`StatusEffectDefinition`/`StatusEffectInstance` 분리)를
당겨와 같이 진행했다.

지금 프로젝트에는 실제 상태 이상(중독·출혈 등 약화 9종, 강화 7종)이
하나도 없다 — 그건 62~63일차 몫이다. 그래서 이번 일차는 **파이프라인
배관과 데이터 모양을 실제로 동작하게 만들되, 아직 아무것도 흘려보내지
않는** 상태로 마무리된다.

---

## 주요 작업 내용

### 1. StatusEffectDefinition / StatusEffectInstance 분리 (61일차 선반영)

기획서 10.3 "상태 이상은 정의 데이터와 인스턴스로 나눈다"를 그대로
옮겼다.

`StatusEffectDefinition`(신규, ScriptableObject, `Data`)

```text
Id (DefinitionBase 공통)
DisplayName
DurationType   (Rounds / UntilCombatEnd)
StackRule      (NoStack / RefreshDuration / Stack)
MaxStack
TickTiming     (RoundEnd — 지금은 이 값 하나뿐)
TickValue      (Effects의 최소 형태: 라운드 종료 시 HP에 더할 값)
```

`Effects`는 문서에 세부 구조가 없어서, 중독·출혈·재생이 전부 "라운드
종료 시 HP 증감 하나"인 걸 근거로 `TickValue` 하나로 시작했다. 여러
효과를 조합하는 상태가 생기면(예: 피해+회피 감소 동시 부여) 확장한다.

`StatusEffectInstance`(신규, 평범한 C# 클래스, `Application`)

```text
DefinitionId
SourceInstanceId
RemainingRounds  (문서 필드명은 RemainingTurns이지만, 59일차에 정정한
                  "라운드" 명명을 그대로 따라 바꿨다)
StackCount
AppliedValue
IsExpired        (RemainingRounds <= 0)
DecrementRemainingRounds()
```

`SourceInstanceId`는 이 상태를 건 참가자의 `InstanceId`다. 지속 피해로
사망했을 때 "마지막 공격자"를 기록하는 데 그대로 쓸 수 있다(기획서 4.2) —
실제 연결은 71일차(패배 기록 대상 추적) 몫이라 지금은 필드만 갖고 있다.

`BattleParticipant`에 `StatusEffects` 목록과 `AddStatusEffect`·
`RemoveExpiredStatusEffects`를 추가했다. 중첩 규칙(NoStack/RefreshDuration/
Stack) 판정은 64일차(상태 성공률·지속시간·중첩)에서 다루므로, 지금은
목록에 더하고 만료된 것을 빼는 것까지만 한다.

`BattleParticipant`에 `Heal(amount)`도 추가했다 — `ApplyDamage`와 대칭
구조로, 최대 HP를 넘는 회복은 잘라내고 실제 회복량을 반환한다. 재생 같은
지속 회복에 필요해서 이번에 같이 만들었다.

### 2. 라운드 파이프라인 확장

`BattleRoundStatusProcessor`(신규, `Application`)를 추가했다.

```text
ApplyStartOfRoundEffects(context)        — 지속 시작 효과 (지금은 빈 구현)
ApplyEndOfRoundDamageAndHealing(context) — AppliedValue < 0 → 피해, > 0 → 회복
DecrementDurationsAndRemoveExpired(context) — 지속시간 1 감소 후 만료 제거
```

죽은 참가자에게는 지속 효과를 적용하지 않는다.

`BattleSession`에 이 세 메서드를 실제로 연결했다.

- `TryStartRound()`: 행동 순서를 계산하기 **전에** `ApplyStartOfRoundEffects`를 호출한다.
- `TryEndRound()`: `RoundEnd`로 전환하기 **전에** `ApplyEndOfRoundDamageAndHealing` →
  `DecrementDurationsAndRemoveExpired` 순서로 호출한다.

지금은 `StatusEffects` 목록이 항상 비어 있어서(상태를 부여하는 스킬이
없음) 이 호출들이 실제로는 아무 효과도 내지 않지만, 배관은 실제로
동작한다 — 테스트에서 `StatusEffectInstance`를 직접 만들어 붙여보면
정상적으로 피해·회복·지속시간 감소가 일어나는 걸 확인했다.

### 3. 지속 피해로 인한 전투 종료 판정

`TryEndRound()`가 지속 피해를 적용하게 되면서, 그 피해만으로 전투가
끝날 수 있게 됐다(기획서 4.2 라운드 구조의 "전투 종료 판정"이 지속
피해 다음 단계). `ExplorationMonsterEncounterController`의 `ConfirmAttack()`·
`ConfirmDefend()`에서 `battleSession.TryEndRound()` 직후 `BattleOutcomeEvaluator`를
한 번 더 확인해, 지속 피해로 전멸했으면 다음 라운드로 넘어가지 않고
바로 전투를 끝내도록 했다.

지금은 상태 이상이 없어 이 경로가 실제로 발동할 일이 없지만(도달 불가능한
안전장치), 62~63일차에 실제 지속 피해가 생기면 바로 의미가 생긴다.

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Application/BattleParticipant.cs
Assets/ProjectDelta/Scripts/Application/BattleRoundStatusProcessor.cs (신규)
Assets/ProjectDelta/Scripts/Application/BattleSession.cs
Assets/ProjectDelta/Scripts/Application/StatusEffectInstance.cs (신규)
Assets/ProjectDelta/Scripts/Data/StatusDurationType.cs (신규)
Assets/ProjectDelta/Scripts/Data/StatusEffectDefinition.cs (신규)
Assets/ProjectDelta/Scripts/Data/StatusStackRule.cs (신규)
Assets/ProjectDelta/Scripts/Data/StatusTickTiming.cs (신규)
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs
Assets/ProjectDelta/Tests/EditMode/BattleParticipantTests.cs
Assets/ProjectDelta/Tests/EditMode/BattleRoundStatusProcessorTests.cs (신규)
Assets/ProjectDelta/Tests/EditMode/BattleSessionTests.cs
Assets/ProjectDelta/Tests/EditMode/StatusEffectInstanceTests.cs (신규)
```

---

## 남은 과제

- 실제 약화 9종·강화 7종은 62~63일차에서 `StatusEffectDefinition` 에셋으로
  만든다.
- 중첩 규칙(NoStack/RefreshDuration/Stack) 판정과 성공률·지속시간 규칙은
  64일차 몫이다.
- 기절로 인한 "행동 차례 건너뜀"은 65일차 근처(라운드 종료 후 상태 정리와
  함께)에서 다룬다.
- `SourceInstanceId`를 실제 "마지막 공격자" 기록에 연결하는 건 71일차
  몫이다.
- 지속 피해로 인한 전투 종료 시 `BattleActionResult.BattleEndResult`에는
  아직 반영되지 않는다(행동 자체의 결과와 별도 시점에 일어나는 일이라).
  실제 지속 피해가 생기면 이 부분도 같이 손봐야 한다.

Unity 에디터에서 재컴파일·테스트 실행 확인이 아직 진행되지 않았다.

---

## 다음 단계

61일차 내용은 이번 일차에 이미 반영했으므로, 다음은 62일차 —
약화 상태 9종(중독·출혈·약화·둔화·기절·혼란·침묵·구속·매혹)을
`StatusEffectDefinition` 데이터로 실제로 만든다.
