# Project Delta - 52일차 개발일지

## 개발 목표

52일차의 목표는 전투 행동 중 **방어**를 실제 Command로 연결하고, 방어 중에는 일정 시간 동안 받는 피해가 감소하도록 구현하는 것이다.

- 대상 선택이 필요 없는 방어 행동 구현
- 방어 상태를 전투 참가자 데이터에 저장
- 방어 중 받는 피해 감소
- 자신의 다음 차례에 방어 상태 자동 해제
- 전투 HUD에서 방어 상태 확인 가능
- 관련 EditMode 테스트 추가

---

## 구현 내용

### 1. BattleParticipant 방어 상태 추가

전투 참가자가 현재 방어 중인지 저장할 수 있도록 `BattleParticipant`에 방어 상태를 추가했다.

- `IsDefending`
- `SetDefending(bool)`

방어 상태를 단순한 UI 표시가 아니라 전투 참가자의 실제 상태값으로 관리하도록 구성했다.

---

### 2. DefendBattleCommand 구현

기존 `IBattleCommand` 구조를 사용해 새로운 `DefendBattleCommand`를 추가했다.

방어는 공격과 달리 적 대상을 선택하지 않고 현재 행동자 자신에게 적용된다.

동작 흐름:

```text
방어 선택
→ DefendBattleCommand.Execute()
→ actor.SetDefending(true)
→ 행동 성공 처리
```

현재 Battle 정보나 행동자가 존재하지 않으면 Command가 거부되도록 방어 처리도 추가했다.

---

### 3. 방어 중 피해 감소 적용

`BattleDamageCalculator`에 방어 상태를 확인하는 계산을 추가했다.

현재 방어 효과는 **최종 피해 50% 감소**이다.

```text
기본 피해 계산
→ 최소 피해 1 보정
→ 대상이 방어 중이면 피해 50% 감소
→ 최소 피해 1 재보정
```

따라서 방어 중에도 피해가 완전히 0이 되지는 않으며 최소 피해 1은 유지된다.

---

### 4. 방어 지속시간 처리

방어는 선택 즉시 활성화되고 **자신의 다음 차례가 시작될 때 해제**된다.

```text
내 차례
→ 방어 선택
→ IsDefending = true

상대 행동 진행
→ 방어 효과 유지

내 다음 차례 시작
→ IsDefending = false
```

`BattleSession.TryEnterAwaitingAction()`에서 새 행동자가 자신의 차례를 시작할 때 이전 방어 상태를 해제하도록 연결했다.

---

### 5. ConfirmDefend() 전투 흐름 연결

`ExplorationMonsterEncounterController`에 `ConfirmDefend()`를 추가했다.

방어는 대상 선택이 필요하지 않기 때문에 공격보다 짧은 흐름으로 처리된다.

```text
AwaitingAction 확인
→ DefendBattleCommand 실행
→ ResolveAction 진입
→ 남은 행동자 확인
→ 필요 시 Turn 종료
→ 다음 Turn 시작
```

방어는 HP를 직접 변화시키지 않기 때문에 방어 행동 자체에서는 승패 판정을 다시 실행하지 않는다.

---

### 6. 방어 버튼 실제 연결

전투 HUD의 방어 버튼을 실제 동작하도록 연결했다.

```text
방어 버튼 클릭
→ OnDefendButtonClicked()
→ ConfirmDefend()
→ DefendBattleCommand
```

방어 버튼은 현재 전투 상태가 `AwaitingAction`일 때만 사용할 수 있다.

---

### 7. 방어 중 UI 표시

플레이어와 적의 전투 슬롯에 **방어중** 배지를 추가했다.

`BattleParticipant.IsDefending` 값을 기준으로 배지가 자동으로 표시되거나 숨겨지도록 구성했다.

이를 통해 현재 누가 방어 상태인지 전투 화면에서 바로 확인할 수 있다.

---

## 테스트

52일차 구현에 맞춰 EditMode 테스트를 추가·보강했다.

### BattleParticipantTests

- `SetDefending(true)` 적용 확인
- `SetDefending(false)` 해제 확인

### DefendBattleCommandTests

- 정상 실행 시 방어 상태 활성화
- 대상이 없어도 실행 가능
- Battle Context가 없을 때 Reject 처리

### BattleDamageCalculatorTests

- 방어 중 피해 50% 감소 확인
- 방어 후에도 최소 피해 1 유지 확인

### BattleSessionTests

- 다른 참가자의 행동 동안 방어 상태 유지
- 자신의 다음 차례 시작 시 방어 상태 해제

---

## 검증

52일차 변경 사항은 다음 프로젝트 어셈블리 기준으로 빌드 오류가 없도록 확인된 상태이다.

```text
ProjectDelta.Application
ProjectDelta.Presentation
ProjectDelta.Editor
ProjectDelta.Tests.EditMode
```

Unity Editor에서 최종적으로 다음 항목을 확인한다.

1. 방어 버튼이 `AwaitingAction` 상태에서 활성화되는지
2. 방어 선택 후 `방어중` 표시가 나타나는지
3. 방어 상태에서 받는 피해가 절반으로 감소하는지
4. 자신의 다음 차례에 방어 상태와 표시가 해제되는지
5. EditMode Test Runner의 관련 테스트가 모두 통과하는지

---

## 52일차 결과

52일차에서 기존 공격 중심의 전투 행동 구조에 **방어 Command**를 추가했다.

방어는 대상 선택 없이 즉시 실행되며, 사용 후 자신의 다음 차례가 돌아오기 전까지 받는 피해를 50% 감소시킨다. 또한 방어 상태를 `BattleParticipant`가 직접 보유하도록 하여 이후 스킬, 상태이상, AI 전투 행동에서도 같은 상태를 활용할 수 있는 기반을 마련했다.

전투 HUD에도 방어 상태 표시를 추가하여 게임 로직과 화면 표시가 동일한 상태를 참조하도록 구성했다.

---

## 기준 커밋

- Commit: `2f2294711677bfd3f5611beeb839e894f480bb04`
- 기존 커밋 제목: `52일차 : 방어 행동 Command 구현 및 유혹 행동 버튼 자리 추가`
- 계획 제목: `방어 행동과 일시 방어 보너스 구현`

이번 개발일지는 계획 제목에 맞춰 **방어 행동과 일시 방어 보너스 구현 내용만 기록**한다.
