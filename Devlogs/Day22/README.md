# Project Delta - 22일차 개발일지

## 개발 주제

**층 입구/출구와 계단 상호작용 기본 구현**

문(14일차)과 같은 F 상호작용으로 다음 층에 내려갈 수 있는 계단을 만들었다. 실제 절차적 던전 생성은 아직 없으므로, 오늘은 계단 상호작용과 층 전환의 최소 골격만 만들고 "다음 층"은 미리 만들어둔 방 프리팹을 그대로 갖다 놓는 자리표시자로 대체했다.

---

## 개발 목표

- 계단 상호작용(F)으로 다음 층에 진입하는 흐름 구현
- `DungeonRunState`(21일차)에 층 번호 개념 추가
- "절차적 맵 생성"과 "계단이 반드시 도달 가능한 위치에 존재"하는 실제 생성 알고리즘은 예정대로 26~35일차로 미루고, 오늘은 그 앞단(상호작용 + 층 전환 배관)만 구현
- 계단 모형을 시각적으로 표현하되, 밟고 올라가는 것처럼 보이지 않도록 처리

---

## 구현 내용

### 1. DungeonRunState — 층 번호 추가

```text
DungeonRunState (21일차: 방 레지스트리)
└─ CurrentFloor (오늘) — 기본값 1, AdvanceFloor() 호출 시 증가
```

되돌아가는 방향은 없다(기획서 3.1절)는 원칙을 그대로 따라 `CurrentFloor`는 증가만 한다.

---

### 2. RoomContentMarker — 그리드 좌표 데이터 추가

19일차부터 "빈 자리 표시만" 하던 `RoomContentMarker`에 `gridX`/`gridZ`와 `GridPosition` 공개 프로퍼티를 추가했다. 계단이 정확히 어느 칸에 있는지 판정하려면 Transform 위치가 아니라 명시적인 그리드 좌표가 필요했다.

---

### 3. 계단 상호작용 — 처음에는 "서서", 결국 "문처럼 정면에서"

처음에는 플레이어가 계단 칸 위에 서 있을 때 F로 상호작용하는 방식으로 만들었다. 그런데 계단 모형(납작한 회색 판)을 배치하고 확인해보니 플레이어가 그 칸으로 걸어 들어가 판 위로 올라가는 것처럼 보였다. 계단은 "밟고 올라가는" 물체가 아니라 "문처럼 막혀 있는 구조물"이어야 했으므로, 설계를 문 상호작용과 완전히 같은 패턴으로 바꿨다.

```text
StairsInteractionController (최종)
├─ 플레이어 정면 방향 계산 (PlayerDoorInteractionController와 동일한 GetFacingDirection)
├─ 정면 칸에 Stairs 마커가 있는지 조회
└─ F 입력 시 DungeonFloorController.TryDescend() 호출
```

정면 칸 자체는 그리드 이동 판정에서 막혀 있어(아래 4번), 플레이어가 애초에 그 칸에 들어갈 수 없다. 물리 충돌이 아니라 이 프로젝트가 원래 쓰던 그리드 논리 차단(벽/문과 동일한 방식)으로 처리했다.

---

### 4. RoomDefinition_TestRoom_B — 계단 칸을 벽으로 봉쇄

계단 모형이 차지하는 두 칸 (0,0)과 (1,0)을 사방 벽 통로로 둘러쌌다.

```text
(0,0): North/South/West 벽
(1,0): North/East/South 벽
(두 칸 사이 경계는 어차피 바깥에서 못 들어오므로 그대로 둠)
```

이제 이 두 칸은 문/벽과 완전히 같은 방식으로 `RoomGridLayout.CanPass`가 이동을 막는다.

---

### 5. 계단 모형 — 회색 납작 판

`TestRoom_B`의 계단 칸 위에 Cube 메시를 얇게 눌러(스케일 4×0.1×2, 2칸 정도 차지) 배치했다. 재질은 기존 벽에 쓰이던 `Door_Gray.mat`을 재사용해 회색으로 통일했다. 콜라이더는 넣지 않았다 — 이동 자체가 물리 충돌이 아니라 그리드 논리 판정이라 필요 없고, 접근 자체가 3번/4번에서 이미 막혀 있다.

---

### 6. DungeonFloorController — 층 전환 (자리표시자)

```text
TryDescend(movementController)
├─ dungeonState.AdvanceFloor() — 층 번호 증가
├─ nextFloorRoomPrefabs 중 하나를 순환 선택 (오늘은 Room_Maze_01 하나만 등록)
├─ 기존 방과 겹치지 않는 위치(원점에서 Z+200, 층마다 X+200)에 Instantiate
├─ 이전에 만든 자리표시자 방이 있으면 정리(Destroy) — 씬 원본 테스트 방은 건드리지 않음
└─ movementController.EnterRoom()으로 정식 진입 절차 실행 (20일차와 동일 8단계)
```

`PlayerGridMovementController.EnterRoom()`을 `private`에서 `public`으로 바꿔 재사용했다. 문 통과든 계단 하강이든 "방에 들어간다"는 절차 자체는 같기 때문이다.

`Room_Maze_01.prefab`에는 `RoomView` 컴포넌트가 없어서(18일차 제작 당시 프리팹 용도로만 만들어짐) 오늘 추가했다 — 다음 층으로 쓰려면 `RoomView`가 있어야 `EnterRoom()`이 요구하는 최소 조건을 만족한다.

---

## 적용 중 발견된 문제 및 수정

**계단 모형 위로 "올라가는" 현상.** 처음 방식(칸 위에 서서 상호작용)에서는 계단 모형이 순수 시각 오브젝트였고 그 칸으로 걸어 들어가는 것 자체를 막지 않아서, 플레이어가 판 위로 걸어 올라가는 것처럼 보였다. 원인은 시각과 판정이 분리되어 있었던 것 — 판정(그리드 이동 가능 여부)은 그대로 두고 모형만 얹었기 때문이다. 문/벽과 같은 방식으로 그 칸 자체를 벽 통로로 막고, 상호작용도 문과 같은 "정면에서" 방식으로 바꿔서 해결했다.

---

## 현재 22일차 전체 흐름

```text
DungeonRunState에 CurrentFloor 추가
↓
RoomContentMarker에 그리드 좌표(gridX/gridZ) 추가
↓
RoomDefinition_TestRoom_B의 계단 칸(0,0)/(1,0)을 벽으로 봉쇄
↓
StairsInteractionController: 문과 같은 "정면 감지 + F" 패턴으로 계단 상호작용
↓
DungeonFloorController: 자리표시자 방(Room_Maze_01)을 생성해 층 전환
↓
PlayerGridMovementController.EnterRoom() 공개해 문 진입과 같은 절차로 재사용
↓
계단 모형(회색 납작 판, Door_Gray 재질)을 벽으로 막힌 칸 위에 배치
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Presentation/StairsInteractionController.cs
Assets/ProjectDelta/Scripts/Presentation/StairsInteractionController.cs.meta
Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs
Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs.meta
Devlogs/Day22/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Domain/RunSubStates.cs (DungeonRunState.CurrentFloor/AdvanceFloor 추가)
Assets/ProjectDelta/Scripts/Presentation/RoomContentMarker.cs (gridX/gridZ, GridPosition 추가)
Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs (EnterRoom public화)
Assets/ProjectDelta/Prefabs/Dungeon/Room_Maze_01.prefab (RoomView 컴포넌트 추가)
Assets/ProjectDelta/Data/Rooms/RoomDefinition_TestRoom_B.asset (계단 칸 벽 통로 추가)
Assets/ProjectDelta/Scenes/DungeonScene.unity (계단 마커·모형, Player에 계단·층 전환 컨트롤러 부착)
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

22일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- `TestRoom_B`의 계단 칸(0,0)/(1,0)에 걸어 들어갈 수 없음 (벽과 동일하게 막힘)
- 계단 정면에서 "계단 내려가기 [F]" 안내가 뜨고, F를 누르면 `Room_Maze_01` 복제본으로 이동함
- 이동 후 Console에 층 번호 로그(`계단 이동: 2층 / Room_Maze_01(Clone)`)가 출력됨
- 새 방에서도 기존 이동/카메라 조작이 정상 동작함

**참고**: `DungeonFloorController`가 갖다 놓는 다음 층은 미리 만든 방 프리팹을 고정 위치에 배치하는 자리표시자다. "계단을 중심으로 한 절차적 생성"과 "하행 계단이 항상 도달 가능한 위치에 존재"를 보장하는 실제 알고리즘은 26~35일차 던전 생성 구간에서 만든다.

---

## 다음 개발 방향

던전 탐험 구간(11~25일차)의 남은 항목을 계속 진행한다. 절차적 던전 생성 자체는 26~35일차로 예정되어 있으므로, 그 전까지는 몬스터 조우·이벤트 판정 등 나머지 탐험 요소를 채워나간다.
