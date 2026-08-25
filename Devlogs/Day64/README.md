# Project Delta - 64일차 개발일지

## 작업 주제

**지속 피해 실행 · 기절 차례 건너뜀 · 추가 행동 · 전투 종료 상태 정리 구현**

---

## 개발 목표

63일차가 "상태를 걸 수 있는가?"(성공률·지속시간·중첩 판정)를 구현했다면, 64일차는 "걸린 상태가 전투에서 무엇을 하는가?"를 구현하는 단계다.

기획 일정 기준 핵심 범위는 다음 네 가지다.

```text
지속 피해
기절 차례 건너뜀
추가 행동
전투 종료 후 상태 정리
```

공격 상승·방어 상승 같은 강화 상태의 실제 능력치 계산은 이번 일차에 넣지 않는다. 이는 전투 계산 시스템을 다루는 이후 일차에서 연결한다.

---

## 주요 작업 내용

### 1. StatusEffectKind 도입 — "값의 부호"가 아니라 "효과 종류"로 실행

기존 `BattleRoundStatusProcessor`는 `AppliedValue < 0`이면 피해, `AppliedValue > 0`이면 회복으로 처리했다. 이 방식은 상태 종류가 늘어나면 위험하다. 예를 들어 이후 "공격 상승 +10"을 `AppliedValue = 10`으로 넣으면 기존 구조에서는 매 라운드 HP 10 회복으로 오인한다.

이를 해결하기 위해 `ProjectDelta.Data.StatusEffectKind`를 새로 추가했다.

```text
Neutral         // 라운드 파이프라인이 자동으로 실행할 효과 없음 (약화·강화 상태 등)
DamageOverTime  // 라운드 종료 시 지속 피해 (중독·출혈)
HealOverTime    // 라운드 종료 시 지속 회복 (재생)
Stun            // 자기 차례를 건너뜀 (기절)
ExtraAction     // 이번 라운드에 추가 행동을 부여
```

`StatusEffectDefinition`과 `StatusEffectInstance`에 `EffectKind` 필드를 추가하고, `StatusEffectApplicationService.TryApply()` 두 오버로드 모두 이 값을 받아 상태 인스턴스 생성 시 그대로 전달하도록 했다. `AppliedValue`는 이제 방향을 갖지 않는 절대값 수치로 다루고, 방향은 오직 `EffectKind`가 결정한다.

기존 16종 상태 에셋(SE001~SE016)에 `effectKind` 필드를 채웠다. 중독·출혈은 `DamageOverTime`, 재생은 `HealOverTime`, 기절은 `Stun`, 나머지 약화·강화 12종은 `Neutral`이다. 또한 62~63일차까지 `0`으로 비어 있던 중독·출혈·재생의 `roundEndValue`도 실제로 동작하도록 값을 채웠다 (중독 5 / 출혈 4 / 재생 3, 정확한 밸런스 수치는 추후 조정 대상).

### 2. BattleRoundStatusProcessor — 지속 피해·회복 실행 구조 정리

`ApplyEndOfRoundDamageAndHealing()`을 `EffectKind` 기준 스위치문으로 다시 작성했다.

```text
DamageOverTime → ApplyDamage(|AppliedValue| * StackCount)
HealOverTime   → Heal(|AppliedValue| * StackCount)
그 외          → 아무 일도 하지 않음
```

63일차부터 준비돼 있던 `StackCount`를 이번에 처음으로 실제 피해·회복량 계산에 연결했다. 중첩 3의 중독은 이제 기본 피해량의 3배가 적용된다.

"지속 효과 → 지속시간 감소 → 만료 제거" 순서는 기존 그대로 유지했다.

### 3. 기절 시 행동 건너뜀 — BattleSession 행동 순서 큐에 연결

`BattleSession.TryEnterAwaitingAction()`의 큐 순회 로직에 기절 판정을 추가했다. 기존에도 죽은 참가자를 건너뛰는 while 루프가 있었으므로, 같은 루프에 "기절 중이면 건너뛴다" 조건을 하나 더했다.

```text
큐에서 후보를 꺼낸다
  → 죽었으면 건너뜀
  → 살아있고 Stun 상태(BattleParticipant.HasActiveStatusEffectOfKind(Stun))면 건너뜀
  → 그 외에는 AwaitingAction으로 전환
```

기절한 참가자는 플레이어·AI 입력을 요구받지 않고 차례만 소비된 채 다음 행동자로 자연스럽게 넘어간다. 기절 지속시간은 여기서 따로 감소시키지 않고, 기존 라운드 종료 지속시간 감소 단계(`DecrementDurationsAndRemoveExpired`)에서만 감소시켜 이중 차감을 막았다.

이 판정에 필요한 조회 API로 `BattleParticipant.HasActiveStatusEffectOfKind(StatusEffectKind)`를 추가했다 (만료되지 않은 상태만 대상으로 한다).

Presentation 레이어(`ExplorationMonsterEncounterController`)는 이미 `TryEnterAwaitingAction()`을 반복 호출하는 코루틴 구조였으므로, 큐 내부에서 몇 명을 건너뛰었는지와 무관하게 그대로 동작한다. 컨트롤러 쪽 변경은 필요 없었다.

### 4. 추가 행동 — 행동 순서 큐에 직접 구현

추가 행동은 상태 지속 피해 처리기가 아니라 `BattleSession`의 행동 순서 큐가 직접 담당하도록 구현했다. 이를 위해 순서 큐 자료구조를 `Queue<BattleParticipant>`에서 `LinkedList<BattleParticipant>`로 바꿔 큐 맨 앞에 다시 끼워 넣을 수 있게 했다.

```csharp
public bool TryGrantExtraAction(BattleParticipant actor)
```

- `AwaitingAction` 또는 `ResolvingAction` 상태에서만 호출할 수 있다.
- `actor`가 살아있고 현재 `Context`에 속한 참가자인지 확인한다.
- `actor`를 큐 맨 앞(`AddFirst`)에 끼워 넣어, 정상 순서로 넘어가기 전에 한 번 더 행동하게 한다.

무한 연쇄 방지를 위한 소비 규칙: 참가자별로 "이번 라운드에 이미 추가 행동을 받았는가"를 `HashSet<string>`(`extraActionGrantedThisRound`)에 기록하고, 이미 받은 적이 있으면 재부여를 거부한다. 이 기록은 매 라운드 시작(`TryStartRound`)과 초기화(`TryReset`/`ForceReset`)마다 비운다.

캐릭터 A가 추가 행동을 얻으면 `A → B → C`가 무조건 진행되는 대신 `A(추가 행동) → A(정상 순서) → B → C` 순서로 진행된다.

### 5. 전투 종료 시 상태 정리

`BattleSession.TryFinishBattle()`에서 `Result`를 만들고 `Finished`로 전환하기 직전에 `Context`에 속한 모든 참가자(Player + Enemies)의 상태 이상을 제거하도록 했다.

```csharp
private void ClearAllParticipantStatusEffects()
```

`BattleParticipant.RemoveAllStatusEffects()`를 새로 추가해 이 정리를 담당한다. `StatusDurationType.Rounds`·`UntilCombatEnd` 구분 없이 목록 전체를 비우므로, 전투 한정 상태(중독·기절 등)는 물론 `UntilCombatEnd` 상태도 다음 전투까지 남지 않는다.

### 6. EditMode 테스트 확장

64일차 핵심 범위를 검증하는 테스트를 추가했다.

```text
BattleRoundStatusProcessorTests
  - DamageOverTime 지속 피해
  - HealOverTime 지속 회복
  - 3중첩 지속 피해량 3배 반영
  - Neutral/Stun 종류는 라운드 틱에서 아무 일도 하지 않음
  - 사망한 참가자는 지속 효과 제외
  - 1라운드 상태는 마지막 효과 적용 후 만료
  - 만료된 항목만 제거 (지속시간 감소 확인)

BattleSessionTests
  - 기절한 참가자는 입력 없이 차례만 소비하고 다음 참가자로 정상 진행
  - 기절 지속시간은 라운드 종료 단계에서만 감소 (이중 차감 없음)
  - 추가 행동을 받은 참가자가 큐 맨 앞에서 한 번 더 행동한 뒤 정상 순서로 복귀
  - 같은 라운드 내 동일 참가자의 추가 행동 재부여 거부 (무한 연쇄 방지)
  - Context에 속하지 않은 참가자의 추가 행동 요청 거부
  - 전투 종료 시 Player·Enemy 상태 이상 전체 제거

BattleParticipantTests
  - HasActiveStatusEffectOfKind: 활성 상태만 true, 만료된 상태는 false
  - RemoveAllStatusEffects: 지속시간 종류와 무관하게 전체 제거
```

기존 63일차 상태 적용 테스트(`StatusEffectApplicationServiceTests`, `StatusEffectInstanceTests`)는 새 생성자 시그니처(`StatusEffectKind` 추가)에 맞춰 호출부만 수정했고, 검증하는 동작 자체(성공률·중첩·재적용 규칙)는 그대로 유지했다.

---

## 변경 파일

```text
Assets/ProjectDelta/Scripts/Data/StatusEffectKind.cs (신규)
Assets/ProjectDelta/Scripts/Data/StatusEffectKind.cs.meta (신규)
Assets/ProjectDelta/Scripts/Data/StatusEffectDefinition.cs

Assets/ProjectDelta/Scripts/Application/StatusEffectInstance.cs
Assets/ProjectDelta/Scripts/Application/StatusEffectApplicationService.cs
Assets/ProjectDelta/Scripts/Application/BattleRoundStatusProcessor.cs
Assets/ProjectDelta/Scripts/Application/BattleParticipant.cs
Assets/ProjectDelta/Scripts/Application/BattleSession.cs

Assets/ProjectDelta/Data/StatusEffects/*.asset (16종 전체, effectKind 필드 추가)
Assets/ProjectDelta/Data/StatusEffects/Poison.asset (roundEndValue 5)
Assets/ProjectDelta/Data/StatusEffects/Bleeding.asset (roundEndValue 4)
Assets/ProjectDelta/Data/StatusEffects/Regeneration.asset (roundEndValue 3)

Assets/ProjectDelta/Tests/EditMode/BattleRoundStatusProcessorTests.cs
Assets/ProjectDelta/Tests/EditMode/BattleSessionTests.cs
Assets/ProjectDelta/Tests/EditMode/BattleParticipantTests.cs
Assets/ProjectDelta/Tests/EditMode/StatusEffectInstanceTests.cs
Assets/ProjectDelta/Tests/EditMode/StatusEffectApplicationServiceTests.cs
```

---

## 확인 사항

- `AppliedValue` 부호가 아니라 `StatusEffectKind`로 지속 피해·회복을 판정하도록 정리
- `StackCount`를 지속 피해·회복 계산에 실제로 연결 (중첩 배증)
- 지속 효과 → 지속시간 감소 → 만료 제거 순서 유지
- 기절한 참가자는 입력 요구 없이 차례만 소비, 기절 지속시간은 라운드 종료 단계에서만 감소
- 추가 행동은 행동 순서 큐(`BattleSession`)가 직접 담당, 상태 지속 피해 처리기와 분리
- 추가 행동의 무한 연쇄를 라운드당 1회 제한으로 차단
- 전투 종료 시 Rounds·UntilCombatEnd 구분 없이 모든 참가자 상태 이상 제거
- 기존 16종 상태 에셋에 `effectKind` 채움, 중독·출혈·재생 `roundEndValue` 실제 수치 반영
- 63일차 상태 적용 테스트 호출부를 새 생성자 시그니처에 맞춰 수정, 검증 동작은 변경 없음
- 강화 상태 능력치 실제 반영은 이번 일차 범위에서 제외 (이후 전투 계산 일차에서 연결)

Unity Editor에서의 실제 스크립트 컴파일과 Test Runner 통과 여부는 이 저장소 diff만으로는 확정할 수 없으므로, Unity Editor에서 EditMode Test Runner를 직접 실행해 최종 확인이 필요하다.

---

## 이번 일차 완료 상태

64일차 목표인 **지속 피해 실행 · 기절 차례 건너뜀 · 추가 행동 · 전투 종료 후 상태 정리**를 구현했다.

```text
상태 부여
↓
라운드 진행
↓
기절 캐릭터 행동 건너뜀
↓
일반 행동 및 추가 행동 처리
↓
라운드 종료
↓
중독·출혈 지속 피해
↓
지속시간 1 감소
↓
만료 상태 제거
↓
전투 종료 여부 확인
↓
전투 종료 시 전투용 상태 전체 정리
```

63일차의 "상태 데이터가 캐릭터에게 붙는 것"에서 64일차의 "붙은 상태가 실제 전투 규칙을 바꾸는 것"까지 이어졌다.

---

## 다음 단계

강화 상태(공격·방어·속도 등) 7종이 실제 능력치 계산에 반영되도록 전투 수치 계산 파이프라인에 연결하는 작업이 다음 후보다. 스킬 Command가 도입되는 66~67일차부터는 `StatusEffectApplicationService.TryApply()`와 `BattleSession.TryGrantExtraAction()`을 실제 스킬 효과가 호출하게 된다.
