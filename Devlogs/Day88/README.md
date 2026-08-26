# Project Delta - 88일차 개발 일지

- 개발일: 2026-08-26
- 최신 커밋: `73a2282bb8b716910b768253b9dc7b2c05955081`
- 기준 커밋: `bc3c47dd3da5e17aeb216e8ba51f2f2c7b089e79`
- 현재 커밋 메시지: `88`
- 개발 주제: 기존 조우 선택 UI 제거 및 몬스터 접촉 즉시 전투 전환 연출 구현

---

## 1. 개발 목표

이번 일차에서는 기존의 조우 선택 화면을 제거하고, 탐험 중 몬스터와 접촉하면 별도의 선택 없이 바로 전투로 진입하도록 흐름을 변경했다.

기존 구조:

`몬스터 접촉 → Encounter Panel → 전투/회피 선택 → Battle`

변경 구조:

`몬스터 접촉 → 탐험 입력 잠금 → 화면 암전 → Battle 준비 → 화면 복원 → 첫 행동 시작`

포켓몬 시리즈처럼 탐험 화면에서 전투 화면으로 자연스럽게 넘어가는 짧은 암전 전환을 목표로 했다.

---

## 2. 기존 Encounter UI 제거

기존 조우 선택 UI를 담당하던 다음 스크립트를 제거했다.

- `Assets/ProjectDelta/Scripts/Presentation/EncounterPanelController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/EncounterPanelController.cs.meta`

DungeonScene에서는 기존 Encounter Panel 아래에 있던 다음 UI 요소도 제거했다.

- EncounterPanel
- MonsterIdText
- StateText
- RoomIdText
- GridPositionText
- ResultText
- BattleButton
- EscapeButton
- TestEndButton
- 관련 Label / Image / Button 구성

이제 플레이어는 전투 시작 전에 전투 또는 회피를 선택하지 않는다.

전투가 시작된 뒤 사용하는 기존 `도주` 기능은 그대로 유지한다.

---

## 3. 오래된 Day44 Encounter UI Installer 제거

88일차 작업 중 기존 `EncounterPanelController`를 제거하자 다음 과거 Editor 스크립트가 삭제된 타입을 직접 참조하면서 컴파일 오류가 발생했다.

`Assets/ProjectDelta/Scripts/Editor/ProjectDeltaDay44EncounterUiInstaller.cs`

오류:

`CS0246: EncounterPanelController 형식 또는 네임스페이스 이름을 찾을 수 없음`

원인은 Day44 Installer가 기존 조우 UI 생성과 검증을 위해 다음 방식으로 `EncounterPanelController`를 직접 사용하고 있었기 때문이다.

- `typeof(EncounterPanelController)`
- `GetComponent<EncounterPanelController>()`
- `BindPanelController(EncounterPanelController ...)`
- 조우 UI 검증 로직

88일차부터 기존 조우 UI 자체를 사용하지 않으므로 해당 Installer도 함께 제거했다.

제거 파일:

- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaDay44EncounterUiInstaller.cs`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaDay44EncounterUiInstaller.cs.meta`

이렇게 해서 삭제된 UI 타입을 과거 Editor 도구가 다시 참조하는 문제를 정리했다.

---

## 4. BattleTransitionController 추가

신규 파일:

`Assets/ProjectDelta/Scripts/Presentation/BattleTransitionController.cs`

탐험 화면과 전투 화면 사이의 암전 연출을 전담한다.

기본 전환 시간:

- Fade Out: `0.20초`
- 완전 검정 유지: `0.10초`
- Fade In: `0.20초`

전체 전환은 약 `0.50초`이다.

전환 속도는 `Time.unscaledDeltaTime`과 `WaitForSecondsRealtime`을 사용하므로 86일차의 전투 `1× / 2×` 속도 설정과 독립적으로 동작한다.

---

## 5. 런타임 전환 Canvas 자동 생성

BattleTransitionController는 별도 Scene / Prefab / Inspector 연결 없이 런타임에 다음 구조를 자동 생성한다.

`BattleTransitionController`
`└─ BattleTransitionCanvas`
`   └─ BlackOverlay`

Canvas 설정:

- Screen Space - Overlay
- Sorting Order: `20000`
- 기준 해상도: `1920 × 1080`

BlackOverlay는 화면 전체를 Stretch하여 검은색 Image로 덮는다.

전환 중에는 Raycast를 차단해 UI 입력도 막고, Fade In이 끝나면 다시 입력을 통과시킨다.

---

## 6. 몬스터 접촉 즉시 자동 전투

`ExplorationMonsterEncounterController`의 Encounter 활성화 이후 흐름을 변경했다.

기존:

`Encounter Active → 플레이어 명령 선택 대기`

변경:

`Encounter Active → StartAutomaticBattleEntry()`

따라서 탐험 중 몬스터가 있는 방에 도착하면 기존 Encounter 내부 상태를 만든 뒤 즉시 전투 전환 코루틴을 시작한다.

EncounterContext와 ExplorationEncounterSession 자체는 유지했다.

즉 UI만 제거했으며 다음 내부 정보와 흐름은 그대로 사용한다.

- 현재 Room
- MonsterDefinitionId
- MonsterGroupDefinitionIds
- Encounter 상태
- 승리/도주 결과
- 방 완료 처리
- 탐험 제어 잠금 및 복원

---

## 7. 자동 전투 진입 순서

새 자동 전투 진입은 다음 순서로 진행된다.

1. Encounter 시작
2. 플레이어 이동 및 탐험 입력 잠금
3. `FadeToBlack()`
4. 화면이 완전히 검정으로 전환
5. 기존 `SelectBattleCommand()` 자동 실행
6. BattleContext 및 BattleSession 준비
7. Battle HUD 갱신
8. 검은 화면 `0.10초` 유지
9. `FadeFromBlack()`
10. 전투 화면이 완전히 표시됨
11. 첫 행동 진행

이 구조를 통해 플레이어가 별도의 조우 UI를 조작할 필요가 없어졌다.

---

## 8. 첫 Enemy 행동 시점 보정

기존 `BeginTestBattle()`은 Battle Round를 시작한 직후 바로 `TestAdvanceBattleTurn()`을 호출했다.

이 상태에서 첫 행동자가 Enemy인 경우 전환 화면이 검은 동안 Enemy 공격이 먼저 처리될 수 있다.

88일차에서는 `BeginTestBattle()` 내부의 즉시 첫 행동 호출을 제거했다.

첫 행동은 `AutomaticBattleEntryRoutine()`의 Fade In 완료 이후에만 시작하도록 변경했다.

따라서 흐름은 다음처럼 유지된다.

`검은 화면 → Battle 준비 → 전투 화면 표시 → 첫 행동`

검은 화면 뒤에서 Enemy가 먼저 공격하여 플레이어가 전투 화면을 봤을 때 이미 HP가 감소해 있는 문제를 방지하는 구조다.

---

## 9. 전환 중 비활성화 안전 처리

ExplorationMonsterEncounterController가 비활성화되는 경우 다음 상태를 정리하도록 추가했다.

- `battleEntryRoutine` 정지
- 코루틴 참조 초기화
- `BattleTransitionController.Current.ForceReveal()` 실행
- 검은 Overlay 알파를 즉시 0으로 복원
- UI 입력 차단 해제

Scene 전환 또는 비정상 Encounter 종료 시 검은 화면이 남는 상황을 방지한다.

---

## 10. 기존 전투 시스템 유지

88일차에서는 전투 계산 로직 자체를 변경하지 않았다.

다음 시스템은 기존 구조를 그대로 유지한다.

- BattleSession
- BattleDamageCalculator
- BattleActionResult
- Battle Intent
- Monster AI
- 전투 중 도주
- BattleSpeedState
- F1 Battle Debug Log
- Battle Reward
- 경험치 및 성장
- 골드 및 아이템 Drop
- 패배 처리
- 전투 체크포인트

이번 변경 범위는 탐험에서 전투로 진입하는 UX와 화면 전환에 집중했다.

---

## 11. BattleTransition EditMode 테스트

신규 파일:

`Assets/ProjectDelta/Tests/EditMode/BattleTransitionControllerTests.cs`

다음 알파 보간 동작을 테스트하도록 구성했다.

1. Fade 시작 시 시작 알파 반환
2. 전환 절반 시점에서 0.5 알파 계산
3. Fade In 종료 시 알파 0
4. 전환 시간을 초과해도 목표 알파 이상으로 넘어가지 않음
5. 전환 시간이 0이면 즉시 목표 알파 반환

전환 계산 로직을 MonoBehaviour 실행과 분리하여 EditMode에서도 확인할 수 있도록 했다.

---

## 12. 변경 파일

87일차 기준 커밋보다 정확히 1커밋 앞서 있으며 총 10개 파일이 변경됐다.

### 생성

- `Assets/ProjectDelta/Scripts/Presentation/BattleTransitionController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleTransitionController.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/BattleTransitionControllerTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleTransitionControllerTests.cs.meta`

### 수정

- `Assets/ProjectDelta/Scenes/DungeonScene.unity`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`

### 삭제

- `Assets/ProjectDelta/Scripts/Presentation/EncounterPanelController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/EncounterPanelController.cs.meta`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaDay44EncounterUiInstaller.cs`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaDay44EncounterUiInstaller.cs.meta`

---

## 13. 최종 플레이 흐름

`탐험`
`↓`
`몬스터와 접촉`
`↓`
`EncounterContext 생성`
`↓`
`탐험 입력 잠금`
`↓`
`0.20초 Fade Out`
`↓`
`화면 완전 검정`
`↓`
`Battle 자동 선택 및 준비`
`↓`
`0.10초 검정 유지`
`↓`
`0.20초 Fade In`
`↓`
`전투 화면 표시`
`↓`
`첫 행동 시작`

전투가 시작된 이후에는 기존 공격, 방어, 스킬, 도주 시스템을 그대로 사용한다.

---

## 14. 검증 결과

최신 GitHub 커밋:

`73a2282bb8b716910b768253b9dc7b2c05955081`

현재 커밋 메시지:

`88`

기준 커밋:

`bc3c47dd3da5e17aeb216e8ba51f2f2c7b089e79`

비교 결과:

- 상태: ahead
- 커밋 차이: 1
- 변경 파일: 10개
- `EncounterPanelController.cs` 삭제 확인
- `ProjectDeltaDay44EncounterUiInstaller.cs` 삭제 확인
- DungeonScene의 `EncounterPanel` 제거 확인
- DungeonScene에서 기존 `EncounterPanelController` GUID 참조 제거 확인
- `BattleTransitionController.cs` 존재 확인
- `BattleTransitionControllerTests.cs` 존재 확인
- `ExplorationMonsterEncounterController`에서 Encounter Active 직후 자동 전투 전환 시작 확인
- Battle 생성 직후 첫 행동을 즉시 호출하지 않도록 변경 확인
- 비활성화 시 전환 코루틴 및 검은 화면 정리 확인

GitHub에는 해당 커밋의 CI status와 workflow run이 등록되어 있지 않다.

따라서 GitHub 저장소와 소스 구조 기준으로 진행을 막는 문제는 발견되지 않았지만, 실제 Unity 컴파일과 EditMode Test Runner 통과 여부는 GitHub에서 확인할 수 없다.

---

## 15. 정리 참고

DungeonScene에서 실제 `EncounterPanel`과 관련 UI 및 Controller는 제거됐지만 `EncounterCanvas`라는 이름의 빈 Canvas 루트는 남아 있다.

현재 해당 Canvas 아래에는 기존 Encounter Panel 자식이 없고, 삭제된 `EncounterPanelController`의 Script GUID 참조도 남아 있지 않으므로 전투 진입 기능에는 영향을 주지 않는다.

향후 Hierarchy 정리 시 사용하지 않는 빈 Canvas 루트를 제거할 수 있다.

---

## 16. 88일차 완료 내용

88일차에서는 기존 조우 선택 UI를 제거하고 몬스터 접촉 즉시 전투로 넘어가는 구조로 변경했다.

화면 전체 암전 전환을 런타임에서 자동 생성하고, 완전히 검어진 상태에서 Battle을 준비한 뒤 전투 화면이 다시 표시되고 나서 첫 행동이 시작되도록 순서를 조정했다.

또한 과거 Day44 Encounter UI Installer가 삭제된 `EncounterPanelController`를 계속 참조해 발생한 컴파일 오류의 원인을 제거하기 위해 오래된 Installer를 함께 삭제했다.

결과적으로 탐험 중 몬스터와 접촉하면 별도의 선택 화면 없이 짧은 암전 연출을 거쳐 바로 전투가 시작되는 흐름으로 정리했다.
