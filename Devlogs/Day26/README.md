# Project Delta - 26일차 개발일지

## 개발 주제

**던전 진행 상태 저장·불러오기 연동**

`RunContext.cs`(Domain)에 5일차부터 남아있던 주석 — *"this is what gameplay code reads and writes during play; a later SaveService flattens it into RunData when writing to disk"* — 의 "나중"을 오늘 처리했다. 저장(`WriteRun`)과 불러오기(`ReadRun`)를 함께 만들어서, 방문 상태·완료 상태·상자 개봉 여부·현재 층이 실제로 디스크에 남고 "이어하기"로 되돌아오게 했다. 추가로 Alt 키로 마우스 커서를 켜고 끄는 기능도 함께 넣었다.

---

## 개발 목표

- `RunContext`(Domain, 런타임)와 `RunData`(Data, 저장 DTO) 사이를 오가는 변환기 마련
- 방 진입·상자 개봉 시점에 자동 저장
- `TitleScene`에 "이어하기" 흐름 추가 (저장된 런이 있을 때만 표시)
- 방-방 연결 그래프가 없는 지금 상황에 맞는 임시 복원 방식(좌표 대신 RoomId) 채택, 실제 좌표 체계는 던전 생성 이후로 명확히 미룸
- Alt 키를 누르고 있는 동안 마우스 커서 활성화

---

## 구현 내용

### 1. DungeonSaveMapper — RunContext ↔ RunData 변환의 중심

```text
DungeonSaveMapper (Data, 신규)
├─ BuildFromRunContext(context) — 저장: 층 번호·현재 방/칸·방별 Visited/Completed/ChestOpened·인벤토리 → RunData
├─ ApplyBasics(context, savedRun) — 불러오기: 층 번호·현재 방/칸·인벤토리를 갓 시작한 RunContext에 주입
├─ BeginRestore(savedRun) — 씬 로드 중 각 방이 조회할 수 있도록 방별 저장 상태를 사전에 준비
└─ TryGetRoomState(roomId) — 방 하나가 자신의 저장 상태를 조회 (여러 번 조회해도 안전)
```

`Coordinate`/`ConnectedDirections`/`Discovered`/`SecretFound`/`TrapTriggered`/`IsStairs`/`StairsDiscovered`/`RestRoomUsed`는 4일차부터 DTO에 이미 있었지만 그 값을 채울 시스템(던전 생성, 발견/함정/휴게실 등)이 아직 없어서 기본값(false/0) 그대로 둔다. 필드는 이미 있으니 해당 시스템이 생기는 날 값을 채우기만 하면 된다.

### 2. 저장 시점 연결

```text
PlayerGridMovementController.EnterRoom() — 20일차부터 비어있던 "// TODO: 자동 저장 요청" 자리에 연결
ChestInteractionController — 상자 개봉 시 + 아이템을 실제로 가져온 시점에 저장
```

### 3. 이어하기 — RoomId 기반 임시 복원

방-방 연결 그래프가 없어서 `RunBasicInfo.CurrentRoomCoordinate`로는 위치를 못 찾는다. 대신 `CurrentRoomId`(신규 필드)를 저장해뒀다가, `PlayerGridMovementController.Awake()`가 복원된 RoomId와 씬 시작 방이 다르면 씬 전체에서 그 RoomId를 가진 `RoomView`를 찾아(`FindRoomViewById`) 그 방으로 시작 위치를 바꾼다. 테스트 방(TestRoom_A/B, Room_Maze_01)처럼 고정 배치된 씬에서만 유효한 임시 방식이고, 좌표/연결 데이터가 생기면(28일차 이후) 걷어낼 코드라는 걸 주석에 명시해뒀다.

각 방의 `RoomPassageController.Awake()`는 자기 RoomId에 맞는 저장 상태가 있으면 `RoomInstance.ApplySavedState()`로 Visited/Completed/ChestOpened를 그대로 복원한다. `ChestContentMarker`도 `Start()`에서(Awake보다 늦게 실행되어 복원이 끝난 뒤 확인 가능) 상자가 "이미 개봉됨" 상태면 빈 상자로 시작한다.

### 4. TitleScene — 이어하기 버튼

`ApplicationFlow.HasSavedRun()`이 true일 때만 "이어하기" 버튼을 보여준다. 누르면 `ContinueGame()` → `ReadRun()`으로 저장 데이터 로드 → `RunContext.Begin()`(저장된 RunId로) → `DungeonSaveMapper.ApplyBasics()`+`BeginRestore()` → 로딩 화면을 거쳐 던전 진입.

런을 포기(`ReturnToTitle()`)할 때는 `SaveService.DeleteRun()`도 같이 호출한다 — 로그라이트 관례상 "포기 = 그 회차 저장 삭제"가 맞는 방향이라 판단했다.

### 5. Alt 키로 마우스 커서 활성화

`PlayerLookController`에 `isUiRequestingFreeCursor`(25일차 상자 패널용)와 `isAltHeld`(오늘 추가)를 분리해서 관리하고, 둘 중 하나라도 켜져 있으면 커서를 푼다. 상자 패널이 열린 채로 Alt를 뗐다고 커서가 도로 잠기는 일이 없도록 두 요청을 독립적으로 추적했다.

---

## 적용 중 발견된 문제 및 수정

없음. 다만 세이브/로드 핵심 구현은 이전 작업 세션에서 이미 만들어져 있었고(사용량 한도 재설정으로 대화 기록에선 요약되어 안 보였지만 디스크엔 남아있었다), 오늘은 그 상태를 다시 읽어 검증(중괄호 짝, 참조 관계, 설계 일관성)한 뒤 Alt 키 기능만 새로 추가했다.

---

## 현재 26일차 전체 흐름

```text
DungeonSaveMapper로 RunContext ↔ RunData 변환 마련
↓
방 진입·상자 개봉 시점에 자동 저장 연결
↓
RoomId 기반 임시 복원 방식으로 "이어하기" 위치 결정
↓
RoomPassageController/ChestContentMarker가 각자 자기 저장 상태를 스스로 복원
↓
TitleScene에 "이어하기" 버튼, 런 포기 시 저장 삭제
↓
PlayerLookController에 Alt 키 커서 활성화 추가 (상자 패널 요청과 독립적으로 관리)
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Data/DungeonSaveMapper.cs
Assets/ProjectDelta/Scripts/Data/DungeonSaveMapper.cs.meta
Devlogs/Day26/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Application/ApplicationFlow.cs (HasSavedRun/ContinueGame/SaveDungeonProgress 추가)
Assets/ProjectDelta/Scripts/Data/RunData.cs (CurrentRoomId/CurrentGridPositionInRoom 추가)
Assets/ProjectDelta/Scripts/Domain/RoomInstance.cs (ChestOpened, MarkChestOpened, ApplySavedState 추가)
Assets/ProjectDelta/Scripts/Domain/RunSubStates.cs (DungeonRunState.SetFloor 추가)
Assets/ProjectDelta/Scripts/Infrastructure/AppRoot.cs (ApplicationFlow 생성자에 saveService 전달)
Assets/ProjectDelta/Scripts/Presentation/ChestContentMarker.cs (개봉 상태 복원)
Assets/ProjectDelta/Scripts/Presentation/ChestInteractionController.cs (개봉·획득 시 자동 저장)
Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs (RoomId 기반 시작 위치 복원, 방 진입 시 자동 저장)
Assets/ProjectDelta/Scripts/Presentation/PlayerLookController.cs (Alt 키 커서 활성화)
Assets/ProjectDelta/Scripts/Presentation/RoomPassageController.cs (Awake 시 저장 상태 복원)
Assets/ProjectDelta/Scripts/Presentation/TitleSceneController.cs (이어하기 버튼)
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

26일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- 새 게임 → 상자 열기 → 방 이동 → 타이틀로 나가기 → "이어하기"로 같은 방·같은 층·상자 개봉 상태가 복원됨
- 던전 안에서 Alt를 누르고 있는 동안 커서가 보이고 시점 회전이 멈추며, 떼면 원래대로 돌아옴
- 상자 패널이 열린 상태에서 Alt를 눌렀다 떼도 커서가 계속 유지됨(Esc로만 닫힘)
- 런을 포기하고 타이틀로 돌아가면 저장 파일이 삭제되어 "이어하기" 버튼이 다시 숨겨짐

**참고**: RoomId 기반 이어하기 위치 복원은 고정 배치된 테스트 씬(TestRoom_A/B, Room_Maze_01)에서만 유효한 임시 방식이다. 실제 좌표/연결 데이터가 생기는 28일차 이후 던전 생성 구간에서 `Coordinate`/`ConnectedDirections` 기반 매핑으로 교체할 예정이다.

---

## 다음 개발 방향

던전 탐험 구간을 보완하기 위해 늘렸던 24~27일차 일정의 마지막 날이다. 27일차에는 **새 게임 → 탐험 → 저장 → 재접속 → 이어하기** 전체 흐름을 통합 테스트하고, 지금까지 던전 탐험 구간에서 발견된 자잘한 문제를 정리한 뒤 28일차부터 절차적 던전 생성으로 넘어간다.
