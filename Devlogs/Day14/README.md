# Project Delta - 14일차 개발일지

## 개발 주제

**벽·문 통과 판정과 F 상호작용·잠긴 문 열쇠 소모 구현**

13일차까지 그리드 위치, 시점 기준 한 칸 이동, 마우스 자유 시점을 구성한 뒤 실제 던전 구조물이 이동 가능 여부에 영향을 주도록 확장했다. 이번 일차에서는 방 내부 통로를 Open/Wall/Door로 구분하고, 닫힌 문과 벽이 이동을 차단하도록 만들었다. 또한 플레이어가 문 바로 앞 칸에서 해당 문을 바라볼 때 F키로 문을 열 수 있도록 상호작용을 연결했으며, 잠긴 문은 현재 보유 열쇠 수를 표시하고 열쇠 1개를 소모해 개방하도록 구현했다.

---

## 개발 목표

- 그리드 칸 사이 통로를 `Open / Wall / Door`로 구분
- 현재 칸과 이동 방향을 기준으로 통과 가능 여부 판정
- 벽과 닫힌 문 이동 차단
- 열린 문만 통과 허용
- 양쪽 인접 칸이 동일한 통로 상태를 공유하도록 구성
- `Exploration` Input Action Map에 `Interact` 추가
- F키를 문 상호작용키로 연결
- 정면의 일반 문에 `열기 [F]` 안내 표시
- 잠긴 문에 현재 보유 열쇠 수 표시
- 열쇠 보유 상태에서 F 입력 시 열쇠 1개 소모 후 잠금 해제 및 문 개방
- 열쇠가 없으면 잠긴 문 유지
- 테스트 방에서 일반 문, 잠긴 문, 벽 동작 검증
- 문·벽 관련 EditMode 테스트 추가

---

## 구현 내용

### 1. `GridPassage` 통로 상태 추가

`Assets/ProjectDelta/Scripts/Domain/GridPassage.cs`를 추가했다.

통로 종류는 다음 세 가지로 구분한다.

```text
Open
→ 일반 통로
→ 즉시 이동 가능

Wall
→ 벽
→ 이동 불가

Door
→ 문
→ 닫혀 있으면 이동 불가
→ 열리면 이동 가능
```

문은 추가로 다음 상태를 가진다.

```text
IsLocked
→ 잠금 여부

IsOpen
→ 열림 여부
```

문 열기 결과도 별도의 `DoorOpenResult`로 구분했다.

```text
NotDoor
AlreadyOpen
Opened
LockedNoKey
```

---

### 2. 플레이어 열쇠 수 추가

기존 `PlayerRunState`에 다음 값을 추가했다.

```text
KeyCount
```

이 값은 현재 런에서 플레이어가 보유한 열쇠 수를 나타낸다.

잠긴 문을 열 때는 다음 순서로 처리한다.

```text
잠긴 문 확인
↓
KeyCount 확인
↓
0개
→ 열기 실패
→ 잠금 상태 유지

1개 이상
→ KeyCount - 1
→ 잠금 해제
→ 문 열림
```

현재 14일차에서는 런타임 상태에 열쇠 수를 추가한 단계이며, 저장 DTO와 영구 저장 연결은 후속 저장 연동 단계에서 처리한다.

---

### 3. `RoomGridLayout` 추가

`Assets/ProjectDelta/Scripts/Domain/RoomGridLayout.cs`를 추가했다.

각 그리드 칸과 방향을 기준으로 해당 경계의 통로를 관리한다.

예를 들어:

```text
(0, 0) North
→ Door

(-1, 0) North
→ Wall
```

와 같은 구조로 사용할 수 있다.

통로를 등록하면 반대편 인접 칸에도 같은 `GridPassage` 객체를 공유한다.

```text
(0, 0) East
↔
(1, 0) West
```

따라서 한쪽에서 문을 열면 반대쪽에서도 동일한 문이 열린 상태로 유지된다.

---

### 4. 이동 방향 계산 확장

기존 `GridMovement`에 상대 입력을 실제 그리드 방향으로 변환하는 기능을 추가했다.

```text
현재 Facing
+
W/A/S/D
↓
실제 이동 방향
North / East / South / West
```

또한 절대 방향에서 그리드 변화량을 얻는 기능을 공개해 통로 데이터와 인접 칸 계산에서 함께 사용할 수 있게 했다.

기존 Yaw 기반 4방향 판정과 목표 GridPosition 계산 구조는 유지했다.

---

### 5. `PlayerGridMovementController` 통로 판정 연결

기존 이동 과정에 `RoomPassageController`를 통한 통과 여부 검사를 추가했다.

14일차 이동 흐름은 다음과 같다.

```text
W/A/S/D 입력
↓
현재 시점의 Facing 계산
↓
실제 이동 방향 계산
↓
목표 GridPosition 계산
↓
GridBounds 검사
↓
현재 칸의 해당 방향 Passage 확인
↓
Open 또는 열린 Door
→ 이동

Wall 또는 닫힌 Door
→ 이동 차단
```

따라서 Player Transform이 먼저 움직인 뒤 Collider에 의해 막히는 방식이 아니라, 논리 데이터에서 이동 가능 여부를 먼저 확인한다.

---

### 6. `RoomPassageController` 추가

`Assets/ProjectDelta/Scripts/Presentation/RoomPassageController.cs`를 추가했다.

현재 테스트 방의 통로 데이터를 구성하고, Presentation 계층에서 이동 및 상호작용 코드가 해당 데이터를 사용할 수 있도록 연결한다.

14일차 테스트 구조는 다음과 같다.

```text
중앙 (0, 0) 북쪽
→ 일반 닫힌 문

동쪽 (1, 0) 북쪽
→ 잠긴 문

서쪽 (-1, 0) 북쪽
→ 벽
```

일반 문과 잠긴 문이 열리면 테스트용 문 오브젝트를 비활성화해 시각적으로 열린 상태를 확인할 수 있다.

---

### 7. F 상호작용 입력 추가

`Assets/InputSystem_Actions.inputactions`의 `Exploration` Map에 다음 액션을 추가했다.

```text
Interact
→ <Keyboard>/f
```

기존 이동과 시점 입력은 그대로 유지된다.

```text
MoveForward  → W
MoveBackward → S
MoveLeft     → A
MoveRight    → D
Look         → Mouse Delta
Interact     → F
```

---

### 8. `PlayerDoorInteractionController` 추가

`Assets/ProjectDelta/Scripts/Presentation/PlayerDoorInteractionController.cs`를 추가했다.

현재 플레이어가 위치한 그리드 칸과 현재 바라보는 4방향을 이용해 바로 앞의 통로가 문인지 확인한다.

정면에 일반 닫힌 문이 있으면:

```text
열기 [F]
```

를 표시한다.

정면에 잠긴 문이 있으면:

```text
잠김 (열쇠 : n개)
```

형태로 현재 플레이어가 가진 열쇠 수를 표시한다.

예:

```text
잠김 (열쇠 : 1개)
잠김 (열쇠 : 0개)
```

---

### 9. 일반 문 상호작용

잠기지 않은 닫힌 문 앞에서 F를 누르면 즉시 열린다.

```text
문 바로 앞 칸에 위치
↓
문 방향을 바라봄
↓
열기 [F]
↓
F 입력
↓
문 열림
↓
이동 가능
```

문이 열린 뒤에는 상호작용 안내를 숨긴다.

---

### 10. 잠긴 문과 열쇠 소모

잠긴 문 앞에서는 현재 열쇠 수가 표시된다.

```text
잠김 (열쇠 : 1개)
```

열쇠가 한 개 이상 있을 때 F를 누르면:

```text
KeyCount 확인
↓
열쇠 1개 소모
↓
잠금 해제
↓
문 열림
↓
이동 가능
```

으로 처리한다.

열쇠가 없다면:

```text
잠김 (열쇠 : 0개)
```

상태를 유지하고 문은 열리지 않는다.

---

### 11. 테스트 방 구조물 추가

`DungeonScene`의 `TestRoom`에 다음 테스트 오브젝트를 추가했다.

```text
TestRoom
├─ Door_Unlocked
├─ Door_Locked
└─ Wall_Internal
```

각 구조물은 논리 그리드 경계와 맞도록 한 칸과 다음 칸 사이에 배치했다.

```text
Door_Unlocked
→ (0, 0)과 (0, 1) 사이

Door_Locked
→ (1, 0)과 (1, 1) 사이

Wall_Internal
→ (-1, 0)과 (-1, 1) 사이
```

`TestRoom`에는 `RoomPassageController`가 연결되고 Player에는 기존 이동·시점 컨트롤러와 함께 `PlayerDoorInteractionController`가 연결된다.

---

### 12. 테스트 씬용 열쇠 지급

`DungeonScene`을 실제 런 흐름 없이 직접 실행하는 테스트 상황에서는 잠긴 문 검증을 위해 임시 `PlayerRunState`에 열쇠 1개를 지급한다.

```text
Test DungeonScene 직접 실행
→ KeyCount = 1
```

실제 `RunContext.Current`가 존재하는 경우에는 실제 플레이어 런타임 상태의 `KeyCount`를 그대로 사용한다.

---

## EditMode 테스트

`RoomGridLayoutTests.cs`를 추가해 통로와 문 규칙을 검증한다.

추가한 테스트는 다음 6종이다.

```text
Wall_BlocksBothDirections
→ 하나의 벽이 양쪽 인접 칸에서 모두 이동을 차단하는지 확인

UnlockedDoor_OpensWithoutKey
→ 일반 문은 열쇠 없이 열리는지 확인

LockedDoor_WithNoKey_RemainsClosed
→ 열쇠가 없으면 잠긴 문이 계속 닫혀 있는지 확인

LockedDoor_WithKey_ConsumesOneAndOpens
→ 열쇠가 있으면 정확히 1개를 소모하고 문을 여는지 확인

ClosedDoor_BlocksMovementUntilOpened
→ 닫힌 문이 이동을 막고 열린 뒤에는 통과할 수 있는지 확인

UnregisteredPassage_IsOpenByDefault
→ 별도 장애물이 없는 내부 통로는 기본적으로 열린 상태인지 확인
```

기존 EditMode 테스트 11종에 신규 6종을 추가했으므로 현재 테스트 구성 기준은 다음과 같다.

```text
EditMode : 17종
PlayMode : 1종
```

실제 Unity Test Runner 통과 여부는 로컬 Unity Editor에서 최종 확인한다.

---

## 현재 14일차 전체 흐름

```text
Open / Wall / Door 통로 상태 정의
↓
RoomGridLayout에 양방향 통로 저장
↓
WASD 실제 이동 방향 계산
↓
GridBounds 검사
↓
Room Passage 통과 가능 여부 검사
↓
벽·닫힌 문 이동 차단
↓
F 상호작용 입력 추가
↓
정면 문 안내 표시
↓
일반 문 F로 개방
↓
잠긴 문 보유 열쇠 수 표시
↓
열쇠 보유 시 1개 소모 후 개방
↓
열린 문 이동 가능
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/GridPassage.cs
Assets/ProjectDelta/Scripts/Domain/GridPassage.cs.meta
Assets/ProjectDelta/Scripts/Domain/RoomGridLayout.cs
Assets/ProjectDelta/Scripts/Domain/RoomGridLayout.cs.meta

Assets/ProjectDelta/Scripts/Presentation/RoomPassageController.cs
Assets/ProjectDelta/Scripts/Presentation/RoomPassageController.cs.meta
Assets/ProjectDelta/Scripts/Presentation/PlayerDoorInteractionController.cs
Assets/ProjectDelta/Scripts/Presentation/PlayerDoorInteractionController.cs.meta

Assets/ProjectDelta/Tests/EditMode/RoomGridLayoutTests.cs
Assets/ProjectDelta/Tests/EditMode/RoomGridLayoutTests.cs.meta

Devlogs/Day14/README.md
```

---

## 수정 파일

```text
Assets/InputSystem_Actions.inputactions
Assets/ProjectDelta/Scenes/DungeonScene.unity
Assets/ProjectDelta/Scripts/Domain/GridMovement.cs
Assets/ProjectDelta/Scripts/Domain/PlayerRunState.cs
Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs
```

---

## 삭제 파일

없음.

14일차 자동 설정에 사용한 임시 `Day14ProjectSetup.cs`는 설정 완료 후 삭제되어 최종 변경 내역에는 남지 않는다.

---

## 최종 확인 항목

14일차 완료 기준은 다음과 같다.

- `Open / Wall / Door` 통로 종류가 존재
- 현재 칸과 인접 칸이 같은 통로 객체를 공유
- 벽은 양방향 이동을 차단
- 닫힌 문은 이동을 차단
- 열린 문은 통과 가능
- 기존 GridBounds 외곽 이동 차단 유지
- `Exploration`에 `Interact` 액션 존재
- F키가 상호작용에 연결됨
- 일반 문 앞에서 `열기 [F]` 표시
- 잠긴 문 앞에서 `잠김 (열쇠 : n개)` 표시
- 열쇠가 없으면 잠긴 문을 열 수 없음
- 열쇠가 있으면 F 입력으로 1개 소모
- 열쇠 소비 후 잠금 해제와 문 열림이 동시에 적용
- 문이 열린 뒤 해당 방향으로 이동 가능
- `DungeonScene`에 일반 문·잠긴 문·내부 벽 테스트 구조 존재
- 기존 마우스 자유 시점 유지
- 기존 시점 기준 WASD 한 칸 이동 유지
- 신규 EditMode 테스트 6종 포함 총 17종 구성
- 기존 PlayMode 테스트 1종 회귀 여부 확인 필요

GitHub 변경 내역 기준으로 14일차 구현을 막는 구조적 문제는 확인되지 않았다. 다만 이 저장소에는 해당 커밋의 자동 CI 상태가 없으므로 Unity Console과 Test Runner의 실제 결과는 로컬 Editor 확인을 최종 기준으로 한다.

---

## 다음 개발 방향

15일차에는 **문 상호작용 상태 확장과 인접 방 이동 조건**을 구현한다.

14일차에서 문을 열고 같은 테스트 방 안의 다음 그리드 칸으로 이동하는 기반을 만들었으므로, 다음 단계에서는 문이 실제 방과 방 사이의 연결점이 되도록 확장한다.

예정 흐름:

```text
문과 인접 Room 연결 정보 정의
↓
열린 문인지 확인
↓
연결된 인접 Room 존재 여부 확인
↓
방 경계 이동 조건 판정
↓
현재 Room 상태 갱신
↓
후속 방 진입/이탈 및 방 전환 시스템과 연결
```

현재 문 개방·잠금·열쇠 소비 규칙은 유지하고, 15일차부터 문을 실제 방 연결 구조와 연동한다.
