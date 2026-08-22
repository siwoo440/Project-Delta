# Project Delta - 12일차 개발일지

## 개발 주제

**시점 기준 WASD 한 칸 이동과 이동 가능 여부 검사 구현**

11일차에서 만든 `GridPosition`과 단일 테스트 방을 실제 플레이 입력과 연결했다. 이번 일차에서는 플레이어가 현재 바라보는 수평 방향을 기준으로 W/A/S/D를 해석하고, 목표 그리드 좌표가 테스트 방 범위 안에 있을 때만 정확히 한 칸 이동하도록 기본 탐험 이동 구조를 구현했다.

---

## 개발 목표

- `Exploration` Input Action Map에 W/A/S/D 이동 액션 구성
- 카메라 Yaw를 가장 가까운 북·동·남·서 4방향으로 변환
- 현재 시점을 기준으로 W/A/S/D 상대 이동 방향 계산
- `CurrentGridPosition`을 기준으로 목표 좌표 계산
- 테스트 방 범위 밖 이동 차단
- 논리 그리드 좌표와 실제 Player Transform 위치 동기화
- 이동 규칙을 Domain 계층의 순수 로직으로 분리
- 그리드 이동 방향과 범위 판정을 EditMode 테스트로 검증
- Localization 초기화에 필요한 Locale 에셋 보완

---

## 구현 내용

### 1. `GridMovement` 도메인 이동 규칙 추가

`Assets/ProjectDelta/Scripts/Domain/GridMovement.cs`를 추가했다.

이 파일에는 Unity 오브젝트와 분리된 순수 이동 규칙을 구성했다.

```text
GridMoveInput
├─ Forward
├─ Backward
├─ Left
└─ Right

CardinalDirection
├─ North
├─ East
├─ South
└─ West
```

카메라의 수평 Yaw 값은 다음 기준으로 가장 가까운 4방향에 대응한다.

```text
315° ~ 45°   → North
45° ~ 135°   → East
135° ~ 225°  → South
225° ~ 315°  → West
```

따라서 플레이어가 어느 방향을 바라보고 있더라도 W는 화면 기준 전진, S는 후진, A/D는 좌우 이동으로 해석할 수 있다.

---

### 2. 테스트 방 이동 범위 정의

`GridBounds`를 추가해 현재 테스트 방에서 이동 가능한 논리 좌표 범위를 정의했다.

```text
X : -2 ~ 2
Z : -2 ~ 2
```

현재 위치와 바라보는 방향, 이동 입력을 이용해 목표 `GridPosition`을 계산한 뒤 `GridBounds.Contains()`로 이동 가능 여부를 확인한다.

예를 들어 동쪽 끝 `(2, 0)`에서 동쪽으로 한 칸 더 이동하려 하면 목표 좌표 `(3, 0)`은 범위를 벗어나므로 이동을 거부한다.

정식 벽·문 통과 가능 데이터는 후속 일차에서 추가하고, 12일차에서는 테스트 방 외부로 나가지 않는 최소 이동 판정을 먼저 확정했다.

---

### 3. `PlayerGridMovementController` 추가

`Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs`를 추가했다.

Presentation 계층에서 다음 역할을 담당한다.

```text
Input System의 W/A/S/D 입력 수신
↓
Main Camera의 현재 Yaw 읽기
↓
GridMovement로 바라보는 4방향 계산
↓
목표 GridPosition 계산
↓
GridBounds로 이동 가능 여부 검사
↓
PlayerRunState.CurrentGridPosition 갱신
↓
Player Transform을 해당 월드 위치로 이동
```

한 칸의 크기는 11일차에서 정한 `2 Unity Units`를 그대로 사용한다.

```text
Grid (0, 0) → World (0, 0, 0)
Grid (1, 0) → World (2, 0, 0)
Grid (0, 1) → World (0, 0, 2)
```

현재 `RunContext.Current`가 존재하면 실제 `PlayerRunState`를 사용하고, `DungeonScene`을 직접 실행하는 테스트 상황에서는 임시 `PlayerRunState`를 만들어 이동을 확인할 수 있게 했다.

---

### 4. Exploration Input Action 구성

기존에 비어 있던 `Exploration` Action Map에 다음 액션을 추가했다.

```text
MoveForward  → W
MoveBackward → S
MoveLeft     → A
MoveRight    → D
```

각 액션은 Button 타입으로 구성했다.

키를 한 번 눌렀을 때 `performed` 이벤트 한 번을 이동 요청 하나로 처리하므로, 일반 FPS식 연속 이동이 아니라 그리드 기반 한 칸 이동 구조를 유지한다.

---

### 5. DungeonScene Player에 이동 컨트롤러 연결

`DungeonScene`의 다음 Player 오브젝트에 `PlayerGridMovementController`를 추가했다.

```text
===Dungeon===
└─ Player
   └─ Main Camera
```

Inspector 연결값은 다음 기준이다.

```text
Input Actions  → Assets/InputSystem_Actions.inputactions
View Transform → Main Camera
Cell Size      → 2
Min X / Max X  → -2 / 2
Min Z / Max Z  → -2 / 2
```

따라서 현재 카메라의 수평 방향을 이동 기준으로 즉시 사용할 수 있다.

마우스 자유 시점 자체는 아직 구현하지 않았지만, 이후 카메라 Yaw가 변경되면 현재 이동 코드가 그대로 새로운 시점 방향을 사용하도록 준비되어 있다.

---

### 6. Presentation 어셈블리 참조 확장

`ProjectDelta.Presentation.asmdef`에 다음 참조를 추가했다.

```text
ProjectDelta.Domain
Unity.InputSystem
```

Presentation 계층에서 `GridPosition`, `GridMovement` 등의 Domain 타입과 Unity Input System을 직접 사용할 수 있게 했다.

기존 `Application`, `Data` 참조는 유지했다.

---

### 7. EditMode 이동 테스트 4종 추가

`GridMovementTests.cs`를 추가해 이동 규칙을 Unity Scene과 독립적으로 검증하도록 했다.

```text
GetFacingFromYaw_UsesNearestCardinalDirection
→ Yaw 값이 올바른 4방향으로 변환되는지 확인

Forward_WhenFacingEast_MovesPositiveX
→ 동쪽을 볼 때 W가 +X로 이동하는지 확인

Right_WhenFacingEast_MovesNegativeZ
→ 동쪽을 볼 때 D가 -Z로 이동하는지 확인

TryGetTarget_RejectsPositionOutsideBounds
→ 테스트 방 경계를 넘어가는 이동을 거부하는지 확인
```

기존 EditMode 테스트 7종에 4종이 추가되어 현재 기준 EditMode 테스트 수는 총 11종이다.

---

## 문제 및 수정

### 8. 활성 상태의 InputActionAsset 수정 예외

12일차 자동 설정 과정에서 다음 예외가 발생했다.

```text
InvalidOperationException:
Cannot add, remove, or change elements of InputActionAsset
while one or more of its actions are enabled
```

원인은 `InputSystem_Actions` 내부 액션이 활성화된 상태에서 `Exploration` Map에 새로운 액션을 추가하려 한 것이다.

자동 설정 과정에서 액션 구조를 수정하기 전에 다음 순서로 비활성화하도록 수정했다.

```text
InputActionAsset 전체 Disable
↓
Exploration Action Map Disable
↓
MoveForward / MoveBackward / MoveLeft / MoveRight 생성
↓
W / S / A / D 바인딩 추가
↓
Input Action Asset 저장 및 재임포트
```

이를 통해 활성 상태 액션을 수정하려던 충돌을 해결했다.

---

### 9. Localization 사용 가능 Locale 부재

Play 확인 과정에서 다음 경고가 발생했다.

```text
No Locale could be selected:
No Locales were available.
```

Localization Settings는 존재했지만 실제 선택 가능한 Locale 에셋이 없던 것이 원인이었다.

다음 Locale을 추가했다.

```text
English (en)
Korean (ko)
```

또한 두 Locale을 Addressables의 `Localization-Locales` 그룹에 등록하고 `Locale` 라벨을 연결했다.

이에 따라 기존 `LocalizationService.InitializeRoutine()`이 초기화될 때 실제 사용할 Locale을 찾을 수 있는 기반을 갖췄다.

---

## 현재 12일차 전체 흐름

```text
Exploration Input Action에 W/A/S/D 등록
↓
PlayerGridMovementController에서 입력 수신
↓
카메라 Yaw를 North/East/South/West로 변환
↓
입력을 현재 시점 기준 GridPosition 변화량으로 변환
↓
목표 좌표 계산
↓
GridBounds(-2~2) 이동 가능 여부 검사
↓
가능하면 CurrentGridPosition 갱신
↓
Player Transform을 2 Unity Units 단위로 이동
↓
EditMode 이동 규칙 테스트 4종 추가
↓
InputActionAsset 활성 상태 수정 예외 해결
↓
English/Korean Locale 및 Addressables 그룹 추가
```

---

## 생성 파일

```text
Assets/AddressableAssetsData/AssetGroups/Localization-Locales.asset
Assets/AddressableAssetsData/AssetGroups/Localization-Locales.asset.meta
Assets/AddressableAssetsData/AssetGroups/Schemas/Localization-Locales_BundledAssetGroupSchema.asset
Assets/AddressableAssetsData/AssetGroups/Schemas/Localization-Locales_BundledAssetGroupSchema.asset.meta
Assets/AddressableAssetsData/AssetGroups/Schemas/Localization-Locales_ContentUpdateGroupSchema.asset
Assets/AddressableAssetsData/AssetGroups/Schemas/Localization-Locales_ContentUpdateGroupSchema.asset.meta

Assets/ProjectDelta/Localization/Locales.meta
Assets/ProjectDelta/Localization/Locales/English (en).asset
Assets/ProjectDelta/Localization/Locales/English (en).asset.meta
Assets/ProjectDelta/Localization/Locales/Korean (ko).asset
Assets/ProjectDelta/Localization/Locales/Korean (ko).asset.meta

Assets/ProjectDelta/Scripts/Domain/GridMovement.cs
Assets/ProjectDelta/Scripts/Domain/GridMovement.cs.meta

Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs
Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs.meta

Assets/ProjectDelta/Tests/EditMode/GridMovementTests.cs
Assets/ProjectDelta/Tests/EditMode/GridMovementTests.cs.meta

Devlogs/Day12/README.md
```

---

## 수정 파일

```text
Assets/AddressableAssetsData/AddressableAssetSettings.asset
Assets/InputSystem_Actions.inputactions
Assets/ProjectDelta/Scenes/DungeonScene.unity
Assets/ProjectDelta/Scripts/Presentation/ProjectDelta.Presentation.asmdef
Project-Delta.slnx
```

`Project-Delta.slnx`는 Presentation 프로젝트가 실제 C# 스크립트를 갖게 되면서 Unity/IDE가 솔루션 프로젝트 목록을 갱신한 변경이다.

---

## 삭제 파일

없음.

12일차 자동 설정에 사용한 임시 `Day12ProjectSetup.cs`는 설정 완료 후 자동 삭제되어 최종 프로젝트 파일에는 남지 않는다.

---

## 최종 확인 항목

12일차 완료 기준은 다음과 같다.

- `Exploration` Action Map에 W/A/S/D 이동 액션 4종이 존재
- `PlayerGridMovementController`가 DungeonScene의 Player에 연결됨
- Input Actions와 Main Camera 참조가 정상 연결됨
- 카메라 Yaw를 4방향 그리드 방향으로 변환할 수 있음
- 바라보는 방향을 기준으로 W/A/S/D 목표 좌표를 계산함
- 입력 한 번에 `GridPosition` 한 칸씩 이동함
- 한 칸의 월드 이동 거리가 2 Unity Units임
- 테스트 방의 `-2 ~ 2` 범위를 넘어갈 수 없음
- `CurrentGridPosition`과 Player Transform이 함께 갱신됨
- EditMode 이동 규칙 테스트 4종이 추가됨
- 기존 7종을 포함해 EditMode 테스트 기준 총 11종
- English/Korean Locale이 생성되고 Addressables Locale 그룹에 등록됨
- Localization 초기화 시 사용할 Locale 기반이 존재함

Unity Console의 최종 컴파일 결과와 실제 Test Runner 통과 여부는 로컬 Unity Editor에서 최종 확인한다.

---

## 다음 개발 방향

13일차에는 **마우스 자유 시점 회전과 수평 시선 방향 상태**를 구현한다.

예정 흐름:

```text
Mouse Delta 입력 추가
↓
Player의 Yaw와 Main Camera의 Pitch 분리
↓
수평 Yaw 자유 회전
↓
상하 Pitch 제한 적용
↓
현재 시선의 수평 방향을 이동 시스템과 연결
↓
WASD 이동이 실제 마우스 시점 변화에 따라 달라지는지 검증
↓
향후 미니맵 플레이어 방향 화살표에서 사용할 방향 기반 확보
```

위치 데이터와 시점 데이터를 분리해, 자유롭게 주변을 둘러보는 동안에는 `CurrentGridPosition`이 변하지 않고 실제 이동 입력이 발생했을 때만 그리드 위치가 변경되는 구조를 유지한다.
