# Project Delta - 44일차 개발일지

## 개발 목표

43일차에 구현한 `EncounterContext`와 `Idle → Starting → Active → Resolving → Finished → Idle` 상태 머신 위에 실제 인카운터 UI와 행동 선택 구조를 연결한다.

이번 일차에서는 실제 전투 계산이나 회피 확률 판정을 구현하지 않고, 플레이어가 몬스터와 Encounter에 진입했을 때 대상 정보를 확인하고 `전투` 또는 `회피` 행동을 선택할 수 있는 공통 Command 구조와 uGUI 화면을 구성한다.

- `IEncounterCommand` 공통 인터페이스 추가
- 전투 / 회피 Command 구현
- Command 실행 결과 데이터 구현
- 기존 Encounter Controller에 행동 선택 연결
- 기존 테스트 `OnGUI` 제거
- 실제 `EncounterCanvas` / `EncounterPanel` 구성
- 대상 상태·몬스터·방·GridPosition 정보 표시
- 전투 / 회피 / 테스트 종료 버튼 구성
- `EventSystem + InputSystemUIInputModule` 구성
- `EncounterPanelController` Inspector 참조 연결
- Command EditMode 테스트 추가
- Day44 UI 자동 구성·검증용 Editor 도구 추가

---

## 구현 내용

### 1. IEncounterCommand 공통 인터페이스

Encounter 화면이 개별 행동 구현에 직접 의존하지 않도록 행동 선택의 공통 계약인 `IEncounterCommand`를 추가했다.

공통 구조:

```text
Id
DisplayName
Execute(EncounterContext)
```

UI는 구체적인 전투 처리나 회피 처리 방식 대신 이 Command 계약을 통해 행동을 실행할 수 있다.

현재는 이후 실제 전투 시스템을 연결하기 위한 최소 구조만 구현한다.

### 2. BattleEncounterCommand 구현

`BattleEncounterCommand`를 추가해 전투 행동 선택을 표현한다.

현재 동작:

```text
EncounterContext 있음
→ 전투 선택 Accept

EncounterContext 없음
→ 전투 선택 Reject
```

실제 공격, 턴 진행, 데미지 계산은 이번 일차 범위에 포함하지 않는다.

### 3. EscapeEncounterCommand 구현

`EscapeEncounterCommand`를 추가해 회피 행동 선택을 표현한다.

현재 동작:

```text
EncounterContext 있음
→ 회피 선택 Accept

EncounterContext 없음
→ 회피 선택 Reject
```

실제 회피 성공률이나 도주 판정은 이후 단계에서 구현한다.

### 4. EncounterCommandResult 구현

Command 실행 결과를 UI와 이후 시스템으로 전달하기 위해 `EncounterCommandResult`를 추가했다.

보관 정보:

```text
CommandId
Accepted
Message
```

공통 생성 방식:

```text
Accept(...)
Reject(...)
```

현재 Encounter UI는 이 결과를 받아 선택 성공 또는 실패 메시지를 화면에 표시한다.

### 5. ExplorationMonsterEncounterController 행동 연결

기존 `ExplorationMonsterEncounterController`에 전투와 회피 Command를 연결했다.

추가된 주요 흐름:

```text
SelectBattleCommand()
→ BattleEncounterCommand 실행
→ LastCommandResult 저장

SelectEscapeCommand()
→ EscapeEncounterCommand 실행
→ LastCommandResult 저장
```

Command는 Encounter가 `Active` 상태이고 `CurrentContext`가 존재하는 경우에만 정상 실행된다.

43일차에서 만든 Encounter 생명주기와 몬스터 접촉 구조는 그대로 유지한다.

### 6. 기존 OnGUI 제거

43일차까지 사용하던 임시 `OnGUI` Encounter 화면을 제거했다.

이제 Encounter 화면은 Unity uGUI 기반의 실제 `Canvas`와 `EncounterPanelController`가 담당한다.

```text
기존
ExplorationMonsterEncounterController
└─ OnGUI 테스트 화면

변경
DungeonScene
└─ EncounterCanvas
   └─ EncounterPanel
```

### 7. DungeonScene EncounterCanvas 구성

`DungeonScene.unity`에 실제 `EncounterCanvas`를 추가했다.

Canvas 주요 설정:

```text
Render Mode
→ Screen Space - Overlay

Canvas Scaler
→ Scale With Screen Size

Reference Resolution
→ 1920 x 1080
```

Canvas 아래에는 실제 인카운터 패널과 행동 버튼이 배치된다.

```text
EncounterCanvas
└─ EncounterPanel
   ├─ TitleText
   ├─ StateText
   ├─ MonsterIdText
   ├─ RoomIdText
   ├─ GridPositionText
   ├─ ResultText
   ├─ BattleButton
   │  └─ Label
   ├─ EscapeButton
   │  └─ Label
   └─ TestEndButton
      └─ Label
```

### 8. 대상 정보 UI

Encounter가 `Active` 상태일 때 현재 Context의 정보를 화면에 표시한다.

표시 항목:

```text
State
Monster
Room
Grid
```

예시:

```text
State : Active
Monster : MON_TEST
Room : ROOM_001
Grid : (2, 3)
```

이를 통해 현재 어떤 몬스터와 어느 위치에서 Encounter가 진행 중인지 확인할 수 있다.

### 9. 행동 결과 UI

`ResultText`를 통해 전투 또는 회피 Command 실행 결과를 표시한다.

현재 표시 형태:

```text
선택 : 전투 선택 / Target ...
선택 : 회피 선택 / Target ...
```

Context나 상태가 올바르지 않은 경우에는 실패 결과를 표시할 수 있도록 구조가 분리되어 있다.

### 10. 행동 버튼 구성

EncounterPanel에는 다음 세 개의 버튼을 구성했다.

```text
전투
회피
테스트 종료
```

동작:

```text
전투
→ SelectBattleCommand()

회피
→ SelectEscapeCommand()

테스트 종료
→ CompleteTestEncounter()
```

실제 전투 계산과 회피 성공 여부는 이번 일차에서 처리하지 않는다.

### 11. EncounterPanelController 연결

`EncounterCanvas`에 `EncounterPanelController`를 배치하고 실제 Scene Object 참조를 연결했다.

연결 항목:

```text
encounterController
panelRoot

stateText
monsterIdText
roomIdText
gridPositionText
resultText

battleButton
escapeButton
testEndButton
```

`EncounterPanelController`는 Encounter 상태를 확인해 `Active` 상태에서만 패널을 표시한다.

```text
Idle / Starting
→ 패널 숨김

Active
→ 패널 표시

Resolving / Finished
→ 패널 숨김
```

### 12. EventSystem 구성

uGUI 버튼 입력을 위해 `DungeonScene`에 `EventSystem`을 추가했다.

구조:

```text
EventSystem
└─ InputSystemUIInputModule
```

프로젝트가 사용하는 Unity Input System과 동일한 UI 입력 방식을 사용한다.

### 13. 기존 플레이어 구성 유지 확인

DungeonScene 수정 이후 기존 Player의 주요 컴포넌트가 유지되는 것을 확인했다.

```text
PlayerGridMovementController
PlayerLookController
PlayerDoorInteractionController
ExplorationMonsterEncounterController
```

따라서 Day44 UI 추가 과정에서 기존 탐험 이동·시점·문 상호작용 구성은 제거되지 않았다.

### 14. Day44 Encounter UI Editor 도구

`ProjectDeltaDay44EncounterUiInstaller`를 추가했다.

Unity 메뉴:

```text
Project Delta
└─ Day 44
   ├─ Build Encounter UI
   └─ Validate Encounter UI
```

`Build Encounter UI`는 DungeonScene에 Day44 Encounter UI를 구성하고 필요한 참조를 자동 연결한다.

`Validate Encounter UI`는 다음 요소의 존재와 직렬화 연결 상태를 검사한다.

```text
EncounterCanvas
EventSystem
EncounterPanelController

encounterController
panelRoot

5개 Text 참조
3개 Button 참조
```

### 15. EditMode Command 테스트

`EncounterCommandTests`를 추가했다.

현재 테스트는 총 6개다.

1. Battle Command의 ID와 표시 이름 확인
2. Battle Command가 Context 존재 시 Accept
3. Battle Command가 Context 없음 시 Reject
4. Escape Command의 ID와 표시 이름 확인
5. Escape Command가 Context 존재 시 Accept
6. Escape Command가 Context 없음 시 Reject

---

## 44일차 동작 흐름

```text
탐험
↓
몬스터와 동일 RoomId / GridPosition 접촉
↓
Starting
- EncounterContext 생성
- 탐험 입력 잠금
↓
Active
↓
EncounterCanvas의 EncounterPanel 표시
↓
State / Monster / Room / Grid 정보 표시
↓
┌───────────────┬───────────────┐
│     전투      │     회피      │
└───────────────┴───────────────┘
        ↓               ↓
Battle Command     Escape Command
        ↓               ↓
EncounterCommandResult
        ↓
ResultText에 선택 결과 표시

테스트 종료
↓
Resolving
↓
Finished
↓
탐험 복귀
↓
Idle
```

---

## 이번 일차에서 제외한 내용

다음 내용은 Day44 범위에 포함하지 않는다.

- 실제 전투 턴 계산
- 플레이어 / 몬스터 공격 처리
- 데미지 계산
- 회피 성공 확률
- 승리 / 패배 결과 반영
- 보상 처리
- 행동 선택 가능 조건 세분화
- 선택 불가 사유 표시
- 행동 중복 입력 방지
- 추가 탐험 잠금 / 해제 정책

위 항목 중 행동 선택 활성 조건, 선택 불가 사유, 중복 입력 방지와 탐험 제어 확장은 다음 단계에서 처리할 수 있다.

---

## 변경 파일

### 생성

- `Assets/ProjectDelta/Scripts/Application/BattleEncounterCommand.cs`
- `Assets/ProjectDelta/Scripts/Application/BattleEncounterCommand.cs.meta`
- `Assets/ProjectDelta/Scripts/Application/EncounterCommandResult.cs`
- `Assets/ProjectDelta/Scripts/Application/EncounterCommandResult.cs.meta`
- `Assets/ProjectDelta/Scripts/Application/EscapeEncounterCommand.cs`
- `Assets/ProjectDelta/Scripts/Application/EscapeEncounterCommand.cs.meta`
- `Assets/ProjectDelta/Scripts/Application/IEncounterCommand.cs`
- `Assets/ProjectDelta/Scripts/Application/IEncounterCommand.cs.meta`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaDay44EncounterUiInstaller.cs`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaDay44EncounterUiInstaller.cs.meta`
- `Assets/ProjectDelta/Scripts/Presentation/EncounterPanelController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/EncounterPanelController.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/EncounterCommandTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/EncounterCommandTests.cs.meta`
- `_Apply_Day44_AssemblyFix.bat`

### 수정

- `Assets/ProjectDelta/Scenes/DungeonScene.unity`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDelta.Editor.asmdef`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ProjectDelta.Presentation.asmdef`

### 삭제

- 없음

---

## 최신 커밋 확인

확인한 최신 커밋:

- SHA: `26e93941871c80900b3590d8415f575a36a7e4b0`
- 현재 커밋 메시지: `a'`
- 이전 커밋: `bb6bd19dfc019c41733bf9c66d647ca53fe8c169`
- 이전 커밋 메시지: `43일차 : EncounterContext 및 인카운터 상태 머신 생명주기 구현`

최신 커밋은 43일차 커밋보다 정확히 1개 커밋 앞선 상태이며, Day44 작업으로 총 19개 파일이 변경되었다.

저장소의 실제 `DungeonScene.unity`를 다시 확인한 결과 다음 요소가 Scene에 직렬화되어 있다.

```text
EncounterCanvas
EncounterPanel
EventSystem
InputSystemUIInputModule
EncounterPanelController
```

`EncounterPanelController`의 Encounter Controller, Panel Root, 5개 Text, 3개 Button 참조도 실제 Scene 파일에 연결되어 있다.

또한 Player의 기존 `PlayerGridMovementController`, `PlayerLookController`, `PlayerDoorInteractionController`가 최신 Scene에 그대로 남아 있는 것을 확인했다.

GitHub 변경 내역과 최신 Scene 파일을 정적으로 확인한 범위에서는 Day44 목표와 충돌하는 명확한 구조적 문제는 확인되지 않았다.

다만 해당 커밋에는 GitHub CI 상태와 GitHub Actions 실행 기록이 없다. 따라서 실제 Unity Editor 컴파일 성공 여부와 `EncounterCommandTests` 6개가 Test Runner에서 통과하는지는 저장소 정보만으로 확인할 수 없다.

---

## 44일차 결과

43일차의 Encounter 생명주기 상태 머신 위에 실제 uGUI 인카운터 화면과 공통 행동 Command 구조를 연결했다.

이제 플레이어가 몬스터와 Encounter에 진입하면 `EncounterCanvas`의 패널에서 현재 상태, 대상 몬스터, 방, GridPosition을 확인할 수 있으며 `전투` 또는 `회피` 행동을 선택할 수 있다.

행동 선택은 `IEncounterCommand`를 기준으로 분리되어 있어 이후 실제 전투 시스템이나 회피 판정이 추가되더라도 UI가 구체적인 처리 로직에 직접 의존하지 않는 구조를 유지한다.

현재 전투·회피는 행동 선택 의도와 결과 메시지만 반환하며, 실제 전투 계산과 선택 제한·중복 입력 방지는 이후 단계에서 확장한다.
