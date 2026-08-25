# Project Delta - 59일차 개발일지

## 작업 주제

**라운드 명명 정정 · CombatRng 분리 · BattleActionResult 전환 — 문자열 메시지 → 변화 목록·로그·SaveRequired**

---

## 개발 목표

기획서 4.2·9.3·10.3에서 확인한 세 가지 정합성 회수 작업을 진행한다.

1. 코드 전반의 "Turn"을 기획서가 쓰는 "라운드" 용어로 정정한다.
2. 전투 핵심 판정에 `UnityEngine.Random`을 직접 쓰던 것을, 목적별로 분리된
   `CombatRng`(기획서 9.3 난수 분리 표)로 바꾼다.
3. 문자열 메시지 하나로 뭉뚱그리던 전투 행동 결과를, 실제 변화 목록·로그·
   저장 필요 여부·전투 종료 결과를 담는 `BattleActionResult`(기획서 10.3)로
   바꾼다.

---

## 주요 작업 내용

### 1. 라운드 명명 정정

기획서는 처음부터 "라운드"만 쓰는데, 47일차부터 코드는 전부 "Turn"이었다.
라운드 번호·상태를 나타내는 심볼만 골라 바꿨다.

```text
BattleSession.TurnNumber → RoundNumber
BattleSession.TryStartTurn() → TryStartRound()
BattleSession.TryEndTurn() → TryEndRound()
BattleSession.HasPendingActorsThisTurn → HasPendingActorsThisRound
BattleSession.PendingActorsThisTurn → PendingActorsThisRound
BattleState.TurnStart / TurnEnd → RoundStart / RoundEnd
BattleResult.TurnCount → RoundCount
ExplorationMonsterEncounterController.BattleTurnNumber → BattleRoundNumber
```

`BattleTurnOrder`와 `TestAdvanceBattleTurn()`/`AdvanceBattleTurnRoutine()`은
그대로 뒀다. 기획서는 "라운드"와 별개로 "행동 순서"를 구분해서 쓰는데
(4.2 라운드 구조에 "행동 순서"가 별도 항목으로 나온다), 이 둘은 라운드
번호가 아니라 "이번 라운드 안에서 누구 차례인가"를 뜻하는 영어의 표준적인
"Turn(차례)" 개념이라 그대로 두는 게 맞다고 판단했다. TRPG의 라운드
(전체 한 바퀴) vs 턴(한 명의 차례) 구분과 같다.

`BattleSessionTests`의 테스트/헬퍼 이름과 어서션 메시지도 전부 맞춰 고쳤다.
그 과정에서 기존 테스트 하나(`CalculateDefendReductionPercent_NeverExceedsSixtyPercent`,
57일차에 이미 커밋된 테스트)가 방어력이 아무리 커져도 곡선 공식이 정확히
60%에 도달하지 않는다는 사실과 충돌해 실패했던 것도 별도로 고쳤다
(`AreEqual` → `LessOrEqual`).

### 2. CombatRng 분리

`IRandomSource`(신규)를 추가했다. 지금은 `CombatRng`가 실제로 쓰는
`NextInt`만 정의한다. 기획서 10.3의 `NextFloat`·`Shuffle`·`CaptureState`는
그 기능을 실제로 쓰는 시스템(던전/조우/이벤트/보상 Rng 이전, 전투 상태
저장)이 생길 때 추가한다.

`CombatRng`(신규)는 `System.Random`을 감싸는 구현체다. 시드를 주면 같은
결과를 재현하고, 시드가 없으면 `Environment.TickCount`로 매번 다르게
진행한다. "저장"은 아직 연결하지 않았다 — 전투 상태 자체가 `RunData`에
저장되지 않아 되돌릴 대상이 없기 때문이다 (9.3 자동 저장 파이프라인은
훨씬 이후 일차 몫).

`ExplorationMonsterEncounterController`가 `UnityEngine.Random.Range(...)`로
직접 만들던 `hitRoll`·`varianceRoll`을 `combatRng.NextInt(...)`로 바꿨다.
참고로 던전 생성(`DungeonGenerator`)은 이미 자체 `System.Random`을 시드로
초기화해 쓰고 있어 전역 상태 문제는 없지만, `IRandomSource`를 구현하고
있지는 않다 — DungeonRng로 옮기는 건 이번 일차 범위 밖으로 남겨뒀다.

### 3. BattleActionResult 전환

`BattleActionResult`(신규)와 `BattleDamageChange`(신규)를 추가했다.

```text
BattleActionResult
├── CommandId
├── Accepted
├── Logs                (문자열 목록 — 기존 Message 하나를 대체)
├── DamageChanges        (BattleDamageChange 목록)
├── RemovedParticipants  (이번 행동으로 죽은 참가자)
├── SaveRequired         (실제 게임 데이터가 바뀌었는지)
└── BattleEndResult      (전투가 끝났으면 그 결과, 아니면 null)
```

`ResourceChanges`·`StatusChanges`·`UpdatedIntents`는 기획서 10.3에 있지만
이번 일차에는 넣지 않았다. 이걸 만들어내는 시스템(자원 소모 Command,
상태 이상, 몬스터 행동 예고)이 하나도 없어서, 지금 넣으면 영원히 빈
목록으로 남는 필드가 된다. 58일차에서 `DamageType.Stamina`를 뺐던 것과
같은 이유다 — 실제로 채울 수 있게 되는 일차(상태 이상은 60~65일차,
스킬·자원 소모는 66일차 이후)에 추가한다.

`IBattleCommand.Execute()`는 그대로 `BattleCommandResult`(행동 선언이
유효한가)를 반환한다. 실제 판정(명중·피해·사망·전투 종료)은 아직
`ExplorationMonsterEncounterController`(Presentation)에서 이뤄지므로,
이 결과도 거기서 조립한다. Command가 판정까지 직접 마치고
`BattleActionResult`를 반환하는 구조로 옮기는 건 스킬 Command가 여럿
생기는 66일차 이후 재검토한다.

`ConfirmAttack()`·`ConfirmDefend()`의 반환 타입을 `BattleCommandResult`에서
`BattleActionResult`로 바꾸고, 컨트롤러의 `LastBattleCommandResult`를
`LastBattleActionResult`로 바꿨다. 전투 종료 처리(`FinishBattle`)가
승리 시 내부에서 `battleSession.TryReset()`을 호출해 `Result`를 곧바로
지워버리는 걸 발견해서, `FinishBattle`이 지워지기 전 결과를 반환하도록
시그니처를 `void → BattleResult`로 바꿔 `BattleEndResult`에 제대로 담기게
했다 (이 문제는 리팩터링 도중 발견한 버그이지 원래 있던 버그는 아니다 —
기존에는 승리 시 곧바로 `return`해서 겉으로 드러나지 않았다).

`BattleHudController`의 상태 텍스트도 `commandResult.Message` 대신
`actionResult.Logs`를 줄바꿈으로 이어붙여 표시하도록 바꿨다.

### 4. EditMode 테스트

- `BattleSessionTests` — 이름·어서션 전면 라운드 명명 반영
- `BattleActionResultTests`(신규) — `Reject`/`Accept` 팩토리가 필드를
  올바르게 채우는지, `BattleEndResult`가 없을 때 `null`로 남는지 확인
- `CombatRngTests`(신규) — 요청한 범위 안에 항상 들어오는지, 같은 시드가
  같은 순서를 만드는지, 다른 시드는 다른 순서를 만드는지 확인
- 57일차 테스트 버그 수정 (`CalculateDefendReductionPercent_NeverExceedsSixtyPercent`)

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Application/BattleActionResult.cs (신규)
Assets/ProjectDelta/Scripts/Application/BattleCommandResult.cs
Assets/ProjectDelta/Scripts/Application/BattleDamageChange.cs (신규)
Assets/ProjectDelta/Scripts/Application/BattleResult.cs
Assets/ProjectDelta/Scripts/Application/BattleSession.cs
Assets/ProjectDelta/Scripts/Application/BattleState.cs
Assets/ProjectDelta/Scripts/Application/BattleTurnOrder.cs
Assets/ProjectDelta/Scripts/Application/CombatRng.cs (신규)
Assets/ProjectDelta/Scripts/Application/IRandomSource.cs (신규)
Assets/ProjectDelta/Scripts/Presentation/BattleHudController.cs
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs
Assets/ProjectDelta/Tests/EditMode/BattleActionResultTests.cs (신규)
Assets/ProjectDelta/Tests/EditMode/BattleSessionTests.cs
Assets/ProjectDelta/Tests/EditMode/CombatRngTests.cs (신규)
```

---

## 남은 과제

- `IBattleCommand.Execute()`가 직접 `BattleActionResult`를 반환하도록
  판정 로직을 Command 쪽으로 옮기는 작업은 66일차 이후로 미룬다.
- `ResourceChanges`·`StatusChanges`·`UpdatedIntents`는 각각의 시스템이
  생길 때 추가한다.
- `IRandomSource`에 `NextFloat`·`Shuffle`·`CaptureState`를 추가하고
  던전·조우·이벤트·보상 난수도 각자 분리하는 건 그 저장 파이프라인이
  붙는 일차의 몫이다.
- `CombatRng`는 시드를 저장·복원하지 않는다. 전투 상태가 `RunData`에
  저장되는 일차에서 `CaptureState`를 추가해야 한다.

Unity 에디터에서 재컴파일·테스트 실행 확인이 아직 진행되지 않았다.

---

## 다음 단계

60일차부터는 단계 B(라운드와 상태 이상)를 시작한다 — 라운드 파이프라인에
지속 시작 효과·지속 피해·회복·지속시간 감소 단계를 삽입한다.
