# Project Delta - 70일차 개발일지

## 작업 주제

**항복 확인창 + 패배 기록 대상 추적 구현**

---

## 개발 목표

69일차까지 공격·방어·스킬·도주 등 전투 내부 행동 흐름이 구성되었지만, 패배 시에는 단순히 타이틀 화면으로 복귀할 뿐 **왜 패배했는지**, **마지막으로 플레이어에게 실제 피해를 준 대상이 누구인지**, **플레이어가 스스로 항복한 것인지**를 구분해서 남기는 공통 기록 계층이 없었다.

70일차에서는 이후 패배 기록·통계·게임 오버 UI가 사용할 수 있도록 다음 기반을 구축했다.

```text
1. 전투 시작 시 패배 추적 정보 초기화
2. 플레이어에게 실제 피해를 준 마지막 공격자 추적
3. 지속 피해(DoT)의 상태 부여자까지 공격자로 추적
4. 항복과 적에 의한 패배를 별도로 기록
5. 항복 확인창 UI 추가
6. 패배 처리 후 기존 ReturnToTitle() 흐름으로 임시 복귀
```

이번 일차에서는 완성형 패배 화면이나 통계 저장 UI까지 만들지 않고, 이후 시스템이 사용할 **패배 데이터와 공통 처리 흐름**을 먼저 만드는 데 집중했다.

---

## 주요 작업 내용

### 1. BattleDefeatReason / BattleDefeatRecord 추가

패배 원인을 단순한 `BattleOutcome.Defeat` 하나로만 취급하지 않고, 실제 기록에서는 다음 두 종류를 구분하도록 했다.

```text
EnemyAttack
Surrender
```

`BattleDefeatRecord`에는 다음 정보를 저장한다.

```text
Reason
AttackerInstanceId
AttackerDefinitionId
RoundNumber
HasAttacker
```

일반 패배라면 마지막 공격자 정보를 남기고, 항복이라면 공격자 없이 `Surrender` 사유만 기록한다.

---

### 2. BattleDefeatService 구현

패배 관련 상태를 한곳에서 관리하는 `BattleDefeatService`를 추가했다.

전투 시작 시:

```text
BattleDefeatService.BeginBattle()
```

를 호출해 이전 전투의 공격자·항복·패배 기록을 초기화한다.

플레이어가 실제 피해를 받았을 때는:

```text
BattleDefeatService.RecordAppliedDamage(...)
```

를 통해 마지막 공격자를 갱신한다.

여기서 중요한 점은 **실제 적용 피해량이 0보다 클 때만** 마지막 공격자를 바꾼다는 것이다. 명중했더라도 실제 피해가 0이라면 기존 공격자 기록을 덮어쓰지 않는다.

---

### 3. 일반 공격과 공격형 스킬의 마지막 공격자 추적

`ExplorationMonsterEncounterController`의 일반 공격과 공격형 스킬 피해 처리에 마지막 공격자 기록을 연결했다.

```text
target.ApplyDamage(...)
→ 실제 적용 피해량 확인
→ BattleDefeatService.RecordAppliedDamage(...)
```

따라서 플레이어가 여러 적에게 연속으로 공격받았을 경우 가장 최근에 실제 피해를 준 적이 최종 패배 기록의 공격자로 남는다.

---

### 4. 지속 피해(DoT) 공격자 추적

직접 공격뿐 아니라 독·화상 같은 지속 피해로 플레이어가 쓰러지는 경우도 추적할 수 있도록 `BattleRoundStatusProcessor`를 수정했다.

상태 효과의:

```text
SourceInstanceId
```

를 이용해 상태를 처음 부여한 전투 참가자를 찾고, 지속 피해가 실제로 적용되면 해당 참가자를 마지막 공격자로 기록한다.

전투 참가자를 찾을 수 없는 경우에도 `SourceInstanceId` 자체는 보존해 이후 기록 시스템이 최소한의 출처 정보를 확인할 수 있게 했다.

---

### 5. SurrenderBattleCommand 추가

항복도 기존 공격·방어·도주와 같은 `IBattleCommand` 계약을 따르도록 `SurrenderBattleCommand`를 추가했다.

항복은 다음 조건에서만 허용된다.

```text
BattleContext 존재
Actor 존재
Actor가 Player 팀
Actor가 생존 상태
```

적 참가자가 항복 Command를 실행하거나 잘못된 전투 상태에서 실행하면 Reject 결과를 반환한다.

---

### 6. 항복 확인창 UI 구현

전투 중 실수로 항복하는 것을 방지하기 위해 전용 `BattleSurrenderCanvas`를 추가했다.

구조는 다음과 같다.

```text
BattleSurrenderCanvas
├─ SurrenderButton
└─ SurrenderConfirmation
   ├─ MessageText
   ├─ ConfirmButton
   └─ CancelButton
```

플레이어 차례의 `BattleState.AwaitingAction` 상태에서만 항복 버튼이 활성화된다.

항복 버튼을 누르면:

```text
정말 항복하시겠습니까?
```

확인창이 열리고, 확인 시 `BattleDefeatService.RecordSurrender()`로 패배 사유를 먼저 기록한 뒤 기존 `TestLoseBattle()` → `FinishBattle()` 흐름에 진입한다.

취소를 누르면 전투 상태를 변경하지 않고 확인창만 닫는다.

---

### 7. 패배 종료 공통 흐름 연결

기존 패배 처리에서는 `FinishBattle()`이 곧바로:

```text
ApplicationFlow.Current?.ReturnToTitle()
```

을 호출했다.

70일차부터는 다음 공통 패배 처리 계층을 거친다.

```text
FinishBattle(BattleOutcome.Defeat)
→ BattleDefeatService.ReturnToTitleAfterDefeat(...)
→ 패배 기록 생성 또는 기존 항복 기록 유지
→ ApplicationFlow.Current?.ReturnToTitle()
```

현재는 여전히 타이틀로 복귀하지만, 이후 패배 화면·통계·히스토리 시스템을 추가할 때 `BattleDefeatService.LastRecord`를 이용할 수 있는 기반이 마련됐다.

---

### 8. Editor 설치 도구 추가

`Day70SurrenderInstaller`를 추가해 다음 작업을 자동화했다.

```text
Project Delta/70일차/항복 시스템 적용 + Canvas 생성
Project Delta/70일차/항복 Canvas만 다시 생성
```

설치 도구는:

```text
전투 시작 패배 추적 초기화 삽입
일반 공격/스킬 피해 추적 코드 삽입
패배 종료 흐름 교체
BattleSurrenderCanvas 생성
버튼 및 확인창 생성
BattleSurrenderController 참조 자동 연결
```

을 수행한다.

이미 적용된 코드와 Canvas를 중복 생성하지 않도록 확인 절차도 포함했다.

---

## 개발 중 오류 및 수정

70일차 초기 설치 도구에서 기존 `ExplorationMonsterEncounterController.cs`를 자동 수정하는 과정 중 줄바꿈을 잘못 생성해, 실제 소스에 문자 형태의 `\n`이 삽입되는 문제가 발생했다.

이 때문에 Unity C# 파서가 일반 공격과 스킬 공격 구간 이후를 정상적으로 읽지 못하면서 `CS1056`, `CS1002`, `CS1003` 등의 문법 오류가 연쇄적으로 다수 발생했다.

최종 70일차 커밋에서는 다음 두 부분을 정상적인 실제 줄바꿈으로 복구했다.

```csharp
appliedDamage =
    target.ApplyDamage(
        damageResult.Damage);

BattleDefeatService.RecordAppliedDamage(
    actor,
    target,
    appliedDamage);
```

일반 공격과 공격형 스킬 두 구간 모두 동일하게 수정되었고, `Day70SurrenderInstaller` 역시 이후 동일 문제가 다시 발생하지 않도록 실제 줄바꿈을 생성하는 방식으로 정정했다.

저장소 루트에는 해당 문제를 복구하기 위해 사용한 다음 보조 파일도 함께 남아 있다.

```text
DAY70_FIX_README.txt
Fix-Day70-CorruptedEncounter.bat
Fix-Day70-CorruptedEncounter.ps1
```

이 파일들은 런타임 게임 코드에는 포함되지 않는 복구용 보조 파일이다.

---

## EditMode 테스트 추가

### BattleDefeatServiceTests

```text
실제 플레이어 피해 시 공격자 저장
0 피해가 기존 공격자를 덮어쓰지 않는지 확인
여러 적 중 가장 최근 실제 공격자 저장
일반 패배 기록에 마지막 공격자 연결
항복 기록에는 공격자가 없는지 확인
```

### SurrenderBattleCommandTests

```text
플레이어 Actor의 항복 승인
Enemy Actor의 항복 거절
BattleContext가 없는 항복 거절
```

---

## 변경 파일

```text
Assets/ProjectDelta/Editor/Day70SurrenderInstaller.cs (신규)

Assets/ProjectDelta/Scenes/BootstrapScene.unity

Assets/ProjectDelta/Scripts/Application/BattleDefeatReason.cs (신규)
Assets/ProjectDelta/Scripts/Application/BattleDefeatRecord.cs (신규)
Assets/ProjectDelta/Scripts/Application/BattleDefeatService.cs (신규)
Assets/ProjectDelta/Scripts/Application/BattleRoundStatusProcessor.cs
Assets/ProjectDelta/Scripts/Application/SurrenderBattleCommand.cs (신규)

Assets/ProjectDelta/Scripts/Presentation/BattleSurrenderController.cs (신규)
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs

Assets/ProjectDelta/Tests/EditMode/BattleDefeatServiceTests.cs (신규)
Assets/ProjectDelta/Tests/EditMode/SurrenderBattleCommandTests.cs (신규)

DAY70_FIX_README.txt (복구 보조)
Fix-Day70-CorruptedEncounter.bat (복구 보조)
Fix-Day70-CorruptedEncounter.ps1 (복구 보조)
```

Unity가 생성한 `.meta` 파일들도 신규 스크립트와 함께 추가되었다.

---

## 확인 사항

- 항복과 적 공격에 의한 패배를 서로 다른 사유로 기록
- 전투 시작마다 이전 패배 추적 정보 초기화
- 일반 공격의 실제 피해량 기준 마지막 공격자 추적
- 공격형 스킬의 실제 피해량 기준 마지막 공격자 추적
- 지속 피해의 `SourceInstanceId`를 이용한 공격자 추적
- 항복 시 공격자 없이 `Surrender` 기록 생성
- 플레이어 행동 가능 상태에서만 항복 버튼 활성화
- 항복 확인/취소 UI 동작 구조 연결
- 패배 종료 전에 공통 기록 계층을 거치도록 `FinishBattle()` 수정
- 기존 임시 `ReturnToTitle()` 흐름 유지
- 이전에 발생한 문자 형태 `\n` 삽입 문제 복구
- 신규 Application 코드가 기존 `IBattleCommand`, `BattleCommandResult`, `BattleParticipant`, `BattleContext`, `StatusEffectInstance` API와 정적으로 호환되는 것을 확인

GitHub 최신 커밋에는 별도의 CI/Unity Test Runner 결과가 등록되어 있지 않다. 따라서 저장소 코드 정적 점검에서는 추가적인 컴파일 차단 문제를 확인하지 못했지만, Unity Editor 실제 컴파일 및 EditMode Test Runner 실행 결과는 로컬 Unity에서 최종 확인해야 한다.

---

## 이번 일차 완료 상태

70일차 목표인 **항복 + 패배 기록 대상 추적 기반**을 구현했다.

이제 전투 패배가 발생했을 때 단순히 타이틀로 이동하기 전에:

```text
일반 패배인지
항복인지
마지막 실제 공격자가 누구인지
몇 라운드에서 패배했는지
```

를 공통 데이터로 남길 수 있다.

현재는 `LastRecord` 형태의 런타임 기록만 유지하고 있으며, 실제 패배 히스토리 저장·통계 UI·게임 오버 화면은 이후 일차에서 이 데이터를 사용하도록 확장한다.

---

## 다음 단계

다음 패배 시스템 작업에서는 `BattleDefeatRecord`를 실제 런 저장 데이터 또는 영구 기록과 연결하고, 패배 화면에서 사유·공격자·라운드 정보를 표시하는 구조로 확장할 수 있다.

항복 UI 역시 현재는 기능 확인용 기본 Canvas이므로 정식 UI 디자인 단계에서 기존 Battle HUD 스타일에 맞춰 통합하는 작업이 필요하다.
