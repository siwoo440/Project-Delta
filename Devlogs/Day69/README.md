# Project Delta - 69일차 개발일지

## 작업 주제

**전투 중 도주(Flee) Command 구현**

---

## 개발 목표

66~68일차로 스킬 하나는 완결됐다. `BattleHudController`에 원래 예정된 네 개 행동 버튼(행동·아이템·도주·유혹) 중 "아이템"과 "유혹"은 아직 없는 시스템(인벤토리, 성인 이벤트)에 기대야 해서 범위가 커진다. 반면 **도주**는 다른 시스템 없이 지금 재료만으로 끝낼 수 있어 69일차로 진행한다.

기획 문서에 전투 중 도주 성공률 공식이 없어, 56일차 명중 공식과 같은 형태의 **임의 공식**을 썼다. 정확한 수치가 확정되면 상수만 조정하면 된다.

```text
도주 성공률(%) = 기본 50% + (내 유효 Speed - 상대 진영 평균 유효 Speed), 5~95% 클램프
```

---

## 주요 작업 내용

### 1. BattleOutcome.Escaped — 기존 EncounterOutcome.Escaped와 연결

도주가 성공하면 승패와 마찬가지로 `BattleSession`의 생명주기를 정식으로 끝내야 한다(64일차에 만든 전투 종료 상태 정리도 포함). 그래서 `BattleOutcome`에 `Escaped`를 추가했다 (`Victory`·`Defeat`는 순서를 유지해 기존 저장·로그 값과 호환된다).

Encounter 결과 쪽은 이미 46일차부터 `EncounterOutcome.Escaped`가 있었다 — 전투 시작 전 "회피" 선택(`EscapeEncounterCommand`)이 쓰던 값이다. `CompletesRoom`/`RemovesMonster`가 둘 다 `false`라서 "몬스터도 안 죽었고 방도 안 끝났다"는 도주 성공 상황과 정확히 맞아떨어진다. 새 값을 만드는 대신 이미 있던 이 값을 그대로 재사용했다.

### 2. BattleEscapeCalculator

`BattleTargeting.GetValidTargets()`를 그대로 재사용해 "상대 진영"(Player면 살아있는 Enemy 전원, Enemy면 살아있는 Player)을 구하고, 그 평균 유효 Speed와 내 유효 Speed의 차이를 성공률에 반영한다. 65일차 `BattleStatModifierService`가 계산하는 유효 Speed(속도 상승·둔화 반영)를 그대로 쓴다.

### 3. FleeBattleCommand

`AttackBattleCommand`(49일차)·`DefendBattleCommand`(52일차)와 같은 원칙으로, `Execute()`는 Battle 정보가 있는지만 확인한다. 대상 선택이 필요 없어 방어와 같은 방식으로 `target`을 쓰지 않는다. 실제 성공률 판정은 `BattleEscapeCalculator`가 담당하고, Presentation에서 굴림을 넣는다.

### 4. ConfirmFlee()

`ConfirmDefend()`와 비슷한 구조지만 결과가 두 갈래로 갈린다.

```text
1. FleeBattleCommand.Execute()로 선언 판정
2. 통과하면 TryBeginResolveAction()
3. BattleEscapeCalculator로 성공률 계산, CombatRng로 굴림
4. 성공 → FinishBattle(BattleOutcome.Escaped) 호출, 전투 즉시 종료
5. 실패 → 방어 실패와 같은 취급으로 로그만 남기고 그 턴을 소모, 다음 행동자로 진행
```

`FinishBattle()`에 `BattleOutcome.Escaped` 분기를 추가해, 승리와 같은 방식으로 Encounter를 정리하되(`FinalizeActiveEncounter`) 결과만 `EncounterOutcome.Escaped`로 담도록 했다. 도주는 승리와 달리 방 완료·몬스터 제거가 일어나지 않아야 하는데, 마침 `EncounterResult.CompletesRoom`/`RemovesMonster`가 `Escaped`일 때 이미 둘 다 `false`를 반환하도록 46일차에 만들어져 있었다.

### 5. EditMode 테스트 추가

```text
BattleEscapeCalculatorTests (신규)
  - 속도가 같으면 기본 성공률 그대로
  - 내가 더 빠르면 성공률 증가
  - 내가 더 느리면 성공률 감소
  - 최소 5%·최대 95% 클램프 확인
  - 적이 여럿이면 평균 Speed 사용 확인
  - 속도 상승 상태(StatModifier)가 실제로 반영됨

FleeBattleCommandTests (신규)
  - 유효한 Context·Actor면 대상 없이도 Accept
  - Context null이면 Reject
  - Actor null이면 Reject
```

`ConfirmFlee()` 자체는 `ConfirmAttack()`/`ConfirmDefend()`/`ConfirmSkill()`과 마찬가지로 전용 자동화 테스트를 두지 않았다(이 프로젝트의 기존 관례 — Presentation 계층 MonoBehaviour는 PlayMode 테스트가 없다). 그 안에서 쓰는 순수 계산 로직(`BattleEscapeCalculator`, `FleeBattleCommand`)은 EditMode 테스트로 검증했다.

---

## 변경 파일

```text
Assets/ProjectDelta/Scripts/Application/BattleOutcome.cs
Assets/ProjectDelta/Scripts/Application/BattleEscapeCalculator.cs (신규)
Assets/ProjectDelta/Scripts/Application/BattleEscapeCalculator.cs.meta (신규)
Assets/ProjectDelta/Scripts/Application/FleeBattleCommand.cs (신규)
Assets/ProjectDelta/Scripts/Application/FleeBattleCommand.cs.meta (신규)
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs

Assets/ProjectDelta/Tests/EditMode/BattleEscapeCalculatorTests.cs (신규)
Assets/ProjectDelta/Tests/EditMode/BattleEscapeCalculatorTests.cs.meta (신규)
Assets/ProjectDelta/Tests/EditMode/FleeBattleCommandTests.cs (신규)
Assets/ProjectDelta/Tests/EditMode/FleeBattleCommandTests.cs.meta (신규)
```

---

## 확인 사항

- `BattleOutcome.Escaped` 추가 (기존 `Victory`·`Defeat` 순서·값 유지)
- `BattleEscapeCalculator`가 65일차 유효 Speed를 반영해 도주 성공률 계산 (임의 공식, 5~95% 클램프)
- `FleeBattleCommand`가 다른 Battle Command와 같은 "선언 판정만" 원칙을 따름
- `ConfirmFlee()`가 성공/실패를 나눠 처리 — 성공은 즉시 전투 종료, 실패는 턴만 소모
- 도주 성공 시 기존 `EncounterOutcome.Escaped`(46일차)를 그대로 재사용해 Encounter까지 정리
- 도주 성공 시에도 64일차 전투 종료 상태 정리, 54일차 자원 되돌리기가 그대로 적용됨(`FinishBattle()` 공통 경로를 그대로 탐)
- 새 EditMode 테스트 9개로 성공률 계산·Command 판정을 검증

Unity Editor에서의 실제 스크립트 컴파일과 Test Runner 통과 여부는 이 저장소 diff만으로는 확정할 수 없으므로, Unity Editor에서 EditMode Test Runner를 직접 실행해 최종 확인이 필요하다.

---

## 이번 일차 완료 상태

69일차 목표인 **전투 중 도주 Command**를 구현했다. 이제 공격·방어·스킬·도주 네 가지 전투 명령 중 시스템 의존성이 없는 세 가지(공격·방어·도주)와 스킬까지 전부 동작한다.

---

## 다음 단계

아이템 사용 Command는 인벤토리 시스템(기획서 6.4절)이 먼저 필요하고, 유혹 Command는 성인 이벤트 시스템이 먼저 필요하다. 스킬 선택 UI는 플레이어가 보유한 스킬 목록 데이터(`PlayerRunState`에 아직 없음)가 먼저 필요하다. 이 셋 중 어느 것을 먼저 준비할지는 기획 우선순위에 따라 정한다.
