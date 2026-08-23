# Project Delta - 27일차 개발일지

## 개발 주제

**던전 탐험 구간 마무리 점검 및 수동 저장·불러오기 구현**

24~27일차로 늘렸던 보완 일정의 마지막 날이다. 새 기능을 쌓기보다 11~26일차 동안 만든 것들이 실제로 하나의 흐름으로 이어지는지 점검했고, 점검 과정에서 발견한 문제 2가지를 고쳤다. 추가로 사용자 요청에 따라 자동 저장뿐 아니라 플레이 중 직접 저장·불러오기할 수 있는 기능도 오늘 함께 만들었다.

---

## 개발 목표

- 새 게임 → 탐험 → 저장 → 재접속 → 이어하기 전체 흐름 점검
- 점검 중 발견한 문제 수정 (2층 이상 이어하기 실패, 낡은 TODO 주석)
- 던전 안에서 즉시 저장/불러오기할 수 있는 임시 버튼 추가

---

## 구현 내용

### 1. 문제 발견: 2층 이상에서 저장하면 이어하기가 깨짐

`DungeonFloorController`가 만드는 자리표시자 방(`Room_Maze_01` 등)은 계단을 실제로 밟아야만 런타임에 생성되는데, `PlayerGridMovementController`(26일차)는 씬을 새로 로드한 직후 곧바로 그 RoomId를 찾으려 했다. 1층(TestRoom_A/B)은 씬에 원래 있는 방이라 문제없었지만, 2층 이상에서 저장한 뒤 이어하면 그 방이 아직 없어서 못 찾고 조용히 1층으로 대체되는 문제였다.

```text
DungeonFloorController
├─ SpawnRoomForCurrentFloor() — 기존 TryDescend()의 방 생성 로직을 분리해 재사용 가능하게 함
├─ EnsureCurrentFloorRoomExists()(신규) — 저장된 층 번호가 1보다 크면 계단 없이도 그 층 방을 미리 생성
└─ GetDungeonState()(신규) — dungeonState를 지연 초기화. 같은 Player 오브젝트의 다른 컴포넌트가
   이 컨트롤러보다 먼저 Awake()를 실행해서 EnsureCurrentFloorRoomExists()를 불러도 안전하다.
```

`PlayerGridMovementController.Awake()`가 RoomId로 방을 찾기(`FindRoomViewById`) 직전에 `EnsureCurrentFloorRoomExists()`를 먼저 호출하도록 연결했다.

### 2. 낡은 TODO 주석 정리

- `RunContext.Begin()`/`End()` — "아직 아무도 안 부름" 주석이 24일차부터 이미 사실이 아니었다. 실제 호출부(`ApplicationFlow`)를 명시하도록 고침
- `PlayerGridMovementController.EnterRoom()`의 "지도 갱신 TODO" — 23일차 미니맵이 매 프레임 직접 조회하는 방식이라 애초에 필요 없어진 주석이었다. 그 이유를 설명하는 주석으로 대체

### 3. 던전 안 수동 저장·불러오기

기존 자동 저장(방 진입/상자 개봉 시)에 더해, 플레이어가 원할 때 직접 저장·불러올 수 있게 했다.

```text
DungeonDebugMenuController (좌측 상단 임시 버튼)
├─ 저장하기 — ApplicationFlow.SaveDungeonProgress() 호출 + "저장했습니다" 1.5초 표시
├─ 불러오기 — ApplicationFlow.ContinueGame() 호출 (씬이 곧바로 바뀌어 별도 안내 없음)
└─ 타이틀로 — 기존 24일차 버튼 그대로
```

`ApplicationFlow.ContinueGame()`은 원래 타이틀 화면 전용이라 이미 런이 진행 중이면 `RunContext.Begin()`이 예외를 던졌다. 던전 안에서 "불러오기"를 누르는 상황이 정확히 그 경우라, 먼저 `RunContext.Current`가 있으면 `RunContext.End()`로 정리한 뒤 저장 데이터로 다시 시작하도록 고쳤다.

---

## 적용 중 발견된 문제 및 수정

**2층 이상 이어하기 실패** (위 1번) — 통합 테스트를 상상해보는 과정에서 코드만 보고 미리 발견했다. 실제 플레이 확인 전에 잡아서 별도 재현·디버깅 없이 바로 고쳤다.

---

## 현재 27일차 전체 흐름

```text
통합 테스트 시나리오를 점검하며 2층 이상 이어하기 버그 발견
↓
DungeonFloorController에 EnsureCurrentFloorRoomExists() 추가로 수정
↓
낡은 TODO 주석 2곳 정리
↓
DungeonDebugMenuController에 저장하기/불러오기 버튼 추가
↓
ApplicationFlow.ContinueGame()이 던전 안에서도 안전하게 호출되도록 수정
```

---

## 생성 파일

```text
Devlogs/Day27/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Application/ApplicationFlow.cs (ContinueGame 안전성 수정)
Assets/ProjectDelta/Scripts/Domain/RunContext.cs (낡은 TODO 주석 정리)
Assets/ProjectDelta/Scripts/Presentation/DungeonDebugMenuController.cs (저장하기/불러오기 버튼 추가)
Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs (EnsureCurrentFloorRoomExists 추가)
Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs (TODO 주석 정리, 층 복원 연결)
Assets/ProjectDelta/Materials/Chest_Gold.mat / SecretWall_Gray.mat (Unity 에디터 재저장으로 인한 사소한 변경)
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

27일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- 새 게임 → 계단으로 2층 진입 → 저장 → 타이틀로 나가기 → 이어하기 → 2층에서 정확히 이어짐
- 1층 이어하기, 상자 개봉 복원 등 기존 동작 유지 (회귀 없음)
- 던전 안 "저장하기" 클릭 시 "저장했습니다" 표시, "불러오기" 클릭 시 저장 시점으로 정확히 복원

---

## 다음 개발 방향

던전 탐험 구간(11~27일차)이 오늘로 마무리된다. 28일차부터 절차적 던전 생성 구간(원래 26~35일차, 오늘까지의 보완 작업으로 28~37일차로 조정)을 시작한다. 계단을 중심으로 한 방 배치, 계단의 도달 가능성 보장 등 지금까지 자리표시자로 남겨뒀던 부분을 실제 알고리즘으로 채운다.
