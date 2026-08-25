# Project Delta - 56일차 개발일지

## 작업 주제

**명중 공식 정합 — 스킬 기본값, 회피×0.5, 5~95%**

---

## 개발 목표

53일차부터 쓰던 임시 명중 공식(`70 + 명중 - 회피`, 5~100% 클램프)을 기획서 4.2
기준으로 정정한다.

```text
명중률 = 스킬 기본 명중률 + 공격자 명중 - 방어자 회피 × 0.5
5~95% 사이로 고정
```

바뀌는 부분은 세 가지다.

- 기본 명중률의 성격이 "고정 상수"에서 "스킬 기본값"으로 바뀐다.
- 회피가 100% 그대로 빠지지 않고 50%만 반영된다.
- 명중률 상한이 100%에서 95%로 낮아진다.

---

## 주요 작업 내용

### 1. 상수 정리

`BaseHitChancePercent`를 `BaseSkillHitChancePercent`로 이름을 바꿨다.
실제 스킬 데이터(`SkillDefinition`)는 66일차 이후에 생기므로, 그 전까지는
기본 공격의 고정 명중률로 취급한다는 걸 주석으로 남겼다.

회피 가중치를 `EvasionWeightPercent = 50` 상수로 분리했다.

`MaxHitChancePercent`를 `100` → `95`로 낮췄다.

### 2. 명중률 공식 정정

```text
weightedEvasion = defender.Evasion * EvasionWeightPercent / 100
rawHitChance = BaseSkillHitChancePercent + attacker.Accuracy - weightedEvasion
```

회피 가중치는 정수 나눗셈으로 버림 처리한다. 예를 들어 회피 15는
`15 × 50 ÷ 100 = 7.5`가 아니라 `7`로 깎인다.

최소·최대 클램프(5~95%)는 기존 `Clamp` 헬퍼를 그대로 재사용했다.

### 3. EditMode 테스트 갱신

`CalculateHitChancePercent_AddsAccuracyAndSubtractsEvasion`을
`CalculateHitChancePercent_AddsAccuracyAndSubtractsHalfEvasion`으로 바꾸고
회피 값을 20으로 조정해 새 공식(70 + 20 - 10 = 80)을 검증하도록 했다.

회피 가중치의 정수 버림을 확인하는 테스트
(`CalculateHitChancePercent_WeightsEvasionByFiftyPercentWithFloor`)를
새로 추가했다. 회피 15 × 50% = 7.5 → 7로 버려지는지 확인한다.

명중률 상한 테스트(`CalculateHitChancePercent_NeverExceedsMaximum`)는
`BattleDamageCalculator.MaxHitChancePercent`를 그대로 참조하고 있어
값 자체를 고치지 않아도 95%로 자동 반영됐다.

`Resolve` 테스트 2개는 방어자 회피가 0이라 가중치 적용 여부와 무관하게
기존 명중률(70%)이 그대로 유지돼 수정하지 않았다.

### 4. "다음 턴" 버튼 없이 바로 진행

전투를 시작하거나 공격·방어를 확정할 때마다 항상 "다음 턴" 버튼을 눌러야
다음 행동자가 나타나던 구조를 바꿔, `BeginTestBattle()`·`ConfirmAttack()`·
`ConfirmDefend()`가 각자 끝나는 지점에서 `TestAdvanceBattleTurn()`을
자동으로 호출하도록 했다.

다음 행동자가 적이면 자동으로 이어지고, 플레이어면 `AwaitingAction`에서
멈춰 공격·방어 버튼이 곧바로 활성화된다. "다음 턴" 버튼은 남겨뒀지만
정상 흐름에서는 거의 항상 비활성 상태가 된다.

### 5. 적 턴 행동을 눈에 보이게 — 코루틴 전환 + 슬롯 튀어오르기

적 턴이 버튼 없이 자동으로 진행되면서, 적이 여러 마리면 한 프레임 안에서
전부 처리돼 아무 것도 안 보이는 문제가 있었다. `TestAdvanceBattleTurn()`을
코루틴(`AdvanceBattleTurnRoutine`) 기반으로 바꿔, 적 한 명이 행동할
때마다 0.45초씩 대기한 뒤 다음 행동자로 넘어가도록 했다. 그 행동으로
전투가 끝나면 대기 없이 바로 멈춘다.

`ExplorationMonsterEncounterController`에 `LastActingParticipant`·
`LastActionSequence`를 추가해 실제 행동이 확정될 때마다 누가 행동했는지
기록한다.

`BattleParticipantSlotView`에 `PlayActionBump()`를 추가했다. 일러스트를
위로 14px 튀었다가 원위치로 돌아오는 연출(0.12초씩)을 코루틴으로
재생한다.

`BattleHudController`가 매 프레임 `LastActionSequence`가 바뀌었는지
확인해서, 바뀌었으면 행동한 참가자가 플레이어인지 몇 번 적 슬롯인지
찾아 해당 슬롯에 `PlayActionBump()`를 재생하도록 했다.

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Application/BattleDamageCalculator.cs
Assets/ProjectDelta/Scripts/Presentation/BattleHudController.cs
Assets/ProjectDelta/Scripts/Presentation/BattleParticipantSlotView.cs
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs
Assets/ProjectDelta/Tests/EditMode/BattleDamageCalculatorTests.cs
```

---

## 남은 과제

- 방어 감소율 곡선(현재는 고정 50%)은 57일차에서 다룬다.
- 치명타·피해 유형별 방어 수치는 58일차에서 다룬다.
- "스킬 기본값"이 실제 스킬 데이터에서 오도록 연결하는 작업은 66일차
  이후(스킬 데이터 구조가 생기는 시점)로 남아 있다.
- 적 행동 간격(0.45초)·튀어오르는 높이(14px)·재생 시간(0.12초)은
  임시로 정한 값이라 실제 플레이해보고 조정이 필요할 수 있다.

Unity 에디터에서 재컴파일·플레이 확인까지 진행했다.

---

## 다음 단계

57일차에서는 방어 감소율을 고정 50%가 아니라 곡선으로 바꾸고,
방어 가능·관통·불가 피해 구분을 도입한다.
