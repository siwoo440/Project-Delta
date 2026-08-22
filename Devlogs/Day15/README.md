# Project Delta - 15일차 개발일지

## 개발 주제

**인접 방 양방향 이동·방 경계 조건·이동 가능 칸 가이드 구현**

14일차에서 벽·문 통과 판정과 F 상호작용, 잠긴 문과 열쇠 소모를 구현한 뒤 문을 실제 방과 방 사이의 연결점으로 확장했다. 이번 일차에서는 두 테스트 방을 연결하고, 열린 문을 통해서만 방 경계를 넘어갈 수 있도록 구성했다. 또한 현재 플레이어가 이동할 수 있는 그리드 칸을 바닥의 선으로 표시하고, 테스트용 문이 벽과 구분되도록 회색 재질을 적용했다.

---

## 개발 목표

- `TestRoom_A`와 `TestRoom_B` 두 테스트 방 구성
- A방 북쪽과 B방 남쪽을 양방향 연결
- 현재 방·경계 칸·이동 방향을 이용한 인접 방 이동 조건 판정
- 닫힌 문 상태에서는 방 경계 이동 차단
- 열린 문을 통과할 때만 인접 방 진입 허용
- 다음 방의 대응 입구 칸으로 Player 배치
- `CurrentRoomId`와 `CurrentGridPosition` 동기화
- 방 이동 후 문 상호작용이 현재 방을 기준으로 계속 동작하도록 수정
- 현재 방의 5×5 이동 가능 칸을 바닥 선으로 표시
- 방이 바뀌면 이전 방 가이드를 숨기고 현재 방 가이드만 표시
- 문 오브젝트에 회색 재질 적용
- 양방향 방 연결 규칙 EditMode 테스트 추가

---

## 구현 내용

### 1. `RoomConnection` 도메인 연결 데이터 추가

`Assets/ProjectDelta/Scripts/Domain/RoomConnection.cs`를 추가했다.

두 방 사이 연결을 다음 정보로 표현한다.

```text
RoomConnectionEnd
├─ RoomId
├─ BoundaryPosition
└─ ExitDirection
```

15일차 테스트 연결은 다음과 같다.

```text
TestRoom_A
BoundaryPosition : (0, 2)
ExitDirection    : North

↕ 양방향 연결

TestRoom_B
BoundaryPosition : (0, -2)
ExitDirection    : South
```

현재 방 식별자, 플레이어가 서 있는 경계 칸, 이동 방향이 모두 일치할 때만 목적 방과 진입 위치를 반환한다.

---

### 2. 두 개의 테스트 방 구성

기존 `TestRoom`은 다음 이름으로 변경했다.

```text
TestRoom_A
```

그리고 A방 북쪽에 동일한 크기의 두 번째 테스트 방을 추가했다.

```text
TestRoom_B
```

두 방은 한 변 길이 10 Unity Units 기준으로 서로 맞닿도록 배치했다.

주요 구조는 다음과 같다.

```text
===Dungeon===
├─ TestRoom_A
├─ TestRoom_B
├─ TestRoomTransition
└─ Player
```

A방에서 사용하던 일반 문, 잠긴 문, 내부 테스트 벽은 유지하고 B방에서는 중복 테스트 구조물을 제거해 방 연결 검증에 집중하도록 구성했다.

---

### 3. 두 방 사이 경계 출입구 구성

A방 북쪽과 B방 남쪽의 전체 벽을 그대로 사용하면 통과할 수 없기 때문에 중앙 출입구가 남도록 벽을 분리했다.

```text
A방 북쪽
Wall_North_Left
Door_RoomConnection
Wall_North_Right

B방 남쪽
Wall_South_Left
공유 출입구
Wall_South_Right
```

`Door_RoomConnection`은 두 방 사이 실제 경계 문 역할을 한다.

---

### 4. 경계 문 상태 공유

A방과 B방이 각각 다른 문 상태를 가지면 한쪽에서 연 문이 반대쪽에서는 닫혀 있는 모순이 생길 수 있다.

이를 방지하기 위해 두 방이 동일한 `GridPassage` 객체를 공유하도록 구성했다.

```text
TestRoom_A North Door
        ↕
동일 GridPassage
        ↕
TestRoom_B South Door
```

따라서 A방에서 문을 열면 B방에서도 같은 문이 열린 상태로 인식된다.

---

### 5. `TestRoomTransitionController` 추가

`Assets/ProjectDelta/Scripts/Presentation/TestRoomTransitionController.cs`를 추가했다.

이 컨트롤러는 현재 테스트 단계에서 두 방의 연결 정보를 관리한다.

방 경계 이동 조건은 다음과 같다.

```text
현재 방 확인
↓
현재 GridPosition 확인
↓
이동 방향 확인
↓
RoomConnection과 일치하는가?
↓
목적 방 존재 여부 확인
↓
목적 방과 입구 GridPosition 반환
```

15일차에서는 정식 `RoomDefinition / RoomInstance` 구조를 만들지 않고, 이후 방 시스템을 구현하기 전 단계의 최소 연결 구조만 구성했다.

---

### 6. `PlayerGridMovementController` 방 경계 이동 확장

14일차까지는 `GridBounds`를 벗어나는 모든 이동을 차단했다.

15일차에서는 이동 흐름을 다음과 같이 변경했다.

```text
W/A/S/D 입력
↓
현재 시점 기준 이동 방향 계산
↓
현재 방향의 Passage 검사
↓
벽 또는 닫힌 문
→ 이동 차단

통과 가능한 Passage
↓
목표 GridPosition 계산
↓
현재 방 Bounds 내부
→ 기존 한 칸 이동

현재 방 Bounds 외부
↓
RoomConnection 검사
↓
연결 방 있음
→ 다음 방 진입

연결 방 없음
→ 이동 차단
```

즉 방 경계를 벗어난다고 바로 이동하는 것이 아니라, 열린 문과 유효한 방 연결이 모두 존재해야 한다.

---

### 7. 방 진입 위치 처리

A방 북쪽에서 B방으로 이동하면 Player는 B방의 남쪽 경계 안쪽 칸에 배치된다.

```text
A : (0, 2) North
↓
B : (0, -2)
```

반대로 B방 남쪽에서 이동하면 A방의 북쪽 경계 칸으로 돌아온다.

```text
B : (0, -2) South
↓
A : (0, 2)
```

이를 통해 같은 문을 이용한 양방향 방 이동을 검증할 수 있다.

---

### 8. 현재 방 상태 갱신

방 경계를 통과하면 다음 런타임 상태를 함께 갱신한다.

```text
PlayerRunState.CurrentRoomId
PlayerRunState.CurrentGridPosition
```

또한 이동 컨트롤러가 사용하는 현재 `RoomPassageController`와 방 원점도 목적 방 기준으로 변경한다.

월드 좌표는 다음 방식으로 계산한다.

```text
현재 방 Transform
+
방 내부 GridPosition × Cell Size
↓
Player World Position
```

따라서 서로 다른 위치에 배치된 방에서도 같은 논리 그리드 좌표 체계를 사용할 수 있다.

---

### 9. 문 상호작용의 현재 방 추적

14일차의 `PlayerDoorInteractionController`는 최초 연결된 방의 `RoomPassageController`를 사용했다.

15일차에서는 방을 이동한 후에도 문 상호작용이 정상 동작하도록 현재 이동 컨트롤러가 가진 `CurrentPassageController`를 우선 사용하도록 변경했다.

```text
A방
→ A방 문 판정

A → B 이동

B방
→ B방 문 판정
```

기존 F 상호작용 규칙은 유지한다.

```text
일반 닫힌 문
→ 열기 [F]

잠긴 문
→ 잠김 (열쇠 : n개)

잠긴 문 + 열쇠 보유
→ F 입력
→ 열쇠 1개 소모
→ 문 개방
```

---

### 10. 이동 가능한 칸 바닥 선 추가

`Assets/ProjectDelta/Scripts/Presentation/GridFloorGuideController.cs`를 추가했다.

현재 테스트 방의 이동 가능한 논리 범위는 계속 다음과 같다.

```text
X : -2 ~ 2
Z : -2 ~ 2
```

따라서 총 5×5, 25칸의 외곽선을 바닥 위에 표시한다.

```text
┌───┬───┬───┬───┬───┐
│   │   │   │   │   │
├───┼───┼───┼───┼───┤
│   │   │   │   │   │
├───┼───┼───┼───┼───┤
│   │   │   │   │   │
├───┼───┼───┼───┼───┤
│   │   │   │   │   │
├───┼───┼───┼───┼───┤
│   │   │   │   │   │
└───┴───┴───┴───┴───┘
```

각 칸은 런타임 `LineRenderer`로 생성한다.

선은 바닥보다 약간 위에 배치해 바닥과 동일한 깊이에서 발생하는 깜빡임을 줄였다.

```text
Line Height : 0.015
Line Width  : 0.025
```

---

### 11. 현재 방의 가이드만 표시

두 방의 그리드 선을 동시에 표시하지 않고 현재 플레이어가 위치한 방의 가이드만 활성화한다.

```text
현재 TestRoom_A
→ A 가이드 표시
→ B 가이드 숨김

A → B 이동

현재 TestRoom_B
→ A 가이드 숨김
→ B 가이드 표시
```

이를 통해 현재 플레이 가능한 방과 그리드 범위를 바로 구분할 수 있다.

---

### 12. 문 회색 재질 추가

테스트용 문이 기존 벽과 비슷하게 보여 식별하기 어려운 문제를 보완하기 위해 다음 재질을 추가했다.

```text
Assets/ProjectDelta/Materials/Door_Gray.mat
```

기본 색상은 중간보다 약간 어두운 회색으로 설정했다.

```text
Base Color
R : 0.42
G : 0.42
B : 0.42
A : 1
```

재질에는 낮은 금속성과 낮은 광택을 적용했다.

```text
Metallic   : 0.1
Smoothness : 0.25
```

다음 테스트용 문에 같은 재질을 사용한다.

```text
Door_Unlocked
Door_Locked
Door_RoomConnection
```

---

## EditMode 테스트

`RoomConnectionTests.cs`를 추가했다.

신규 테스트는 총 5종이다.

```text
FromA_NorthBoundary_ReturnsBEntry
→ A방 북쪽 경계에서 B방 남쪽 입구를 반환하는지 확인

FromB_SouthBoundary_ReturnsAEntry
→ B방 남쪽 경계에서 A방 북쪽 입구를 반환하는지 확인

WrongDirection_IsRejected
→ 올바른 경계 칸이라도 잘못된 방향이면 연결을 거부하는지 확인

WrongBoundaryPosition_IsRejected
→ 올바른 방향이라도 잘못된 칸이면 연결을 거부하는지 확인

UnknownRoom_IsRejected
→ 등록되지 않은 방에서는 연결이 발생하지 않는지 확인
```

14일차까지의 EditMode 테스트 17종에 5종을 추가했으므로 현재 테스트 구성 기준은 다음과 같다.

```text
EditMode : 22종
PlayMode : 1종
```

실제 Test Runner 통과 여부는 로컬 Unity Editor에서 최종 확인한다.

---

## 현재 15일차 전체 흐름

```text
TestRoom_A / TestRoom_B 구성
↓
두 방 경계 출입구 생성
↓
RoomConnection 양방향 연결 정의
↓
경계 문 GridPassage 공유
↓
현재 방 내부 WASD 이동
↓
방 경계 도달
↓
문 통과 가능 여부 확인
↓
RoomConnection 확인
↓
다음 방의 대응 입구 칸으로 이동
↓
CurrentRoomId / CurrentGridPosition 갱신
↓
문 상호작용도 현재 방 기준으로 전환
↓
현재 방 5×5 이동 가능 칸 바닥 선 표시
↓
방 이동 시 가이드 표시 대상 변경
↓
테스트 문 회색 재질 적용
```

---

## 생성 파일

```text
Assets/ProjectDelta/Materials.meta
Assets/ProjectDelta/Materials/Door_Gray.mat
Assets/ProjectDelta/Materials/Door_Gray.mat.meta

Assets/ProjectDelta/Scripts/Domain/RoomConnection.cs
Assets/ProjectDelta/Scripts/Domain/RoomConnection.cs.meta

Assets/ProjectDelta/Scripts/Presentation/GridFloorGuideController.cs
Assets/ProjectDelta/Scripts/Presentation/GridFloorGuideController.cs.meta
Assets/ProjectDelta/Scripts/Presentation/TestRoomTransitionController.cs
Assets/ProjectDelta/Scripts/Presentation/TestRoomTransitionController.cs.meta

Assets/ProjectDelta/Tests/EditMode/RoomConnectionTests.cs
Assets/ProjectDelta/Tests/EditMode/RoomConnectionTests.cs.meta

Devlogs/Day15/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scenes/DungeonScene.unity

Assets/ProjectDelta/Scripts/Presentation/PlayerDoorInteractionController.cs
Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs
Assets/ProjectDelta/Scripts/Presentation/RoomPassageController.cs

ProjectSettings/URPProjectSettings.asset
```

`ProjectSettings/URPProjectSettings.asset`에는 Unity URP가 재질 작업 과정에서 프로젝트 설정 폴더 경로를 기록한 변경이 포함되었다.

---

## 삭제 파일

없음.

15일차 씬 자동 구성에 사용한 `Day15ProjectSetup.cs`와 문 회색 재질 자동 적용에 사용한 `Day15DoorGrayFix.cs`는 작업 완료 후 자동 삭제되어 최종 커밋에는 남지 않는다.

---

## 최종 확인 항목

15일차 완료 기준은 다음과 같다.

- `TestRoom_A`와 `TestRoom_B`가 존재
- A방 북쪽과 B방 남쪽이 양방향으로 연결됨
- A/B 양쪽에서 같은 경계 문 상태를 공유
- 닫힌 경계 문은 방 이동을 차단
- F로 경계 문을 연 뒤 이동 가능
- 열린 문이어도 연결되지 않은 위치나 방향에서는 방 이동 불가
- A방 `(0, 2)`에서 B방 `(0, -2)`로 진입
- B방 `(0, -2)`에서 A방 `(0, 2)`로 복귀 가능
- 방 이동 후 `CurrentRoomId` 갱신
- 방 이동 후 `CurrentGridPosition` 갱신
- 방 이동 후 문 상호작용이 현재 방을 기준으로 동작
- 기존 일반 문·잠긴 문·열쇠 소비 규칙 유지
- 각 방의 5×5 이동 가능 칸 바닥 선 존재
- 현재 방의 바닥 선만 표시
- 테스트 문에 회색 재질 적용
- 신규 EditMode 테스트 5종 포함 총 22종 구성
- 기존 PlayMode 테스트 1종 회귀 여부 확인 필요

GitHub 변경 내역 기준으로 15일차 구현을 막는 구조적 문제는 확인되지 않았다. 다만 해당 커밋에는 자동 CI 상태가 없으므로 Unity Console의 컴파일 결과와 EditMode/PlayMode Test Runner의 실제 통과 여부는 로컬 Unity Editor에서 최종 확인한다.

---

## 다음 개발 방향

16일차에는 **이동 입력 잠금과 중복 이동 방지**를 구현한다.

현재 이동은 입력 한 번에 즉시 한 칸 또는 다른 방 입구로 위치가 변경된다. 다음 단계에서는 이동 처리 중 추가 입력이 중복 적용되지 않도록 명확한 이동 상태를 구성한다.

예정 흐름:

```text
이동 요청
↓
현재 이동 처리 중인지 확인
↓
이동 가능 여부 판정
↓
이동 시작
↓
추가 WASD 입력 잠금
↓
이동 완료
↓
입력 잠금 해제
```

이 상태는 17일차의 카메라 위치·회전 보간과 연결할 수 있도록 구성한다.
