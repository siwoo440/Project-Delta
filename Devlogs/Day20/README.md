# Project Delta - 20일차 개발일지

## 개발 주제

**정식 방 진입·이탈과 현재 방 관리**

기획서 3.3.1절이 정의한 방 진입 처리 순서(*"이동 가능 여부 확인 → 문 상태 확인 → 방 이동 → 현재 위치 갱신 → 지도 갱신 → 최초 방문 여부 확인 → 방 이벤트 처리 → 자동 저장 요청"*)를 코드 위에 명시적으로 드러내고, 15일차부터 이어지던 "통로 컨트롤러와 방 원점을 따로따로 갈아끼우는" 방식을 19일차의 `RoomView` 하나로 정리했다.

---

## 개발 목표

- 8단계 진입 순서 중 지금 채울 수 있는 부분을 하나의 절차로 명시
- `RoomInstance`에 최초 방문 여부 추적 추가
- `passageController`/`roomOrigin` 두 필드를 `RoomView` 하나로 통합
- 시작 방도 "최초 방문" 규칙 대상으로 처리
- 아직 시스템이 없는 나머지 단계(지도 갱신·이벤트 처리·자동 저장)는 TODO로 순서 자리만 유지

---

## 구현 내용

### 1. RoomInstance — 최초 방문 여부

```text
RoomInstance.MarkVisited()
→ 처음 호출: Visited = true로 전환하고 true 반환
→ 이후 호출: false 반환 (이미 방문한 방)
```

기획서 3.3.3절 *"일반 방의 이벤트는 처음 들어온 순간 한 번만 처리한다"*를 그대로 반영했다.

---

### 2. RoomPassageController — RoomInstance 보관

18일차에는 `RoomInstance`를 만들고 `Layout`만 꺼내 쓴 뒤 버렸다. 오늘부터 `CurrentInstance` 프로퍼티로 공개해, 밖에서 방문 여부를 확인·기록할 수 있게 했다.

---

### 3. PlayerGridMovementController — RoomView로 통합

```text
Before: passageController(RoomPassageController) + roomOrigin(Transform) 따로 관리
After:  currentRoomView(RoomView) 하나로 통합
```

`CurrentPassageController`/`CurrentRoomOrigin` 프로퍼티 이름과 동작은 그대로 유지해서, 이를 사용하던 `PlayerDoorInteractionController`는 수정이 필요 없었다.

---

### 4. EnterRoom() — 3.3.1절 진입 순서를 코드로

```text
EnterRoom(roomView, entryPosition, facing)
├─ 방 이동 → currentRoomView 갱신
├─ 현재 위치 갱신 → CurrentRoomId / CurrentGridPosition
├─ 지도 갱신 (TODO, 지도 시스템 없음)
├─ 최초 방문 여부 확인 → RoomInstance.MarkVisited()
├─ 방 이벤트 처리 (TODO, 99~108일차 이벤트 시스템)
├─ 자동 저장 요청 (TODO, SaveService/RunContext 연결 이후)
└─ 목적 방 입구까지 부드럽게 이동 (17일차 MoveRoutine 재사용)
```

이동 가능 여부 확인과 문 상태 확인은 호출부(`TryMove`)에서 이미 끝난 상태로 `EnterRoom`에 들어오므로, 8단계 중 앞 두 단계는 그대로 재사용했다.

---

### 5. 시작 방도 최초 진입으로 처리

`Awake()`에서 플레이어가 처음 스폰되는 방에 대해서도 `MarkVisited()`를 호출하도록 추가했다. 시작 방이라고 해서 "최초 방문" 규칙에서 예외가 되지 않는다.

---

### 6. 씬 필드 갱신

`DungeonScene`의 Player가 참조하던 `passageController` 필드가 `currentRoomView`로 이름이 바뀌어, 기존 참조(TestRoom_A의 RoomPassageController)를 TestRoom_A의 RoomView로 다시 연결했다.

---

## 적용 중 발견된 문제 및 수정

없음. Console 확인 결과 컴파일 에러 없음, 기존 이동·방 전환 동작 그대로 유지됨.

---

## 현재 20일차 전체 흐름

```text
RoomInstance에 Visited/MarkVisited 추가
↓
RoomPassageController가 RoomInstance를 CurrentInstance로 계속 보관
↓
PlayerGridMovementController의 passageController+roomOrigin을 currentRoomView로 통합
↓
EnterRoom()으로 3.3.1절 8단계 순서를 코드에 명시 (구현 3개 + TODO 3개)
↓
시작 방도 최초 방문으로 처리
↓
씬의 Player 필드 참조 갱신
```

---

## 생성 파일

없음.

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Domain/RoomInstance.cs
Assets/ProjectDelta/Scripts/Presentation/RoomPassageController.cs
Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs
Assets/ProjectDelta/Scenes/DungeonScene.unity
Devlogs/Day20/README.md
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

20일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- 기존 이동·방 전환 동작이 그대로 유지됨
- 새 방에 처음 진입하면 로그에 "최초 방문 True"가 출력됨
- 같은 방을 다시 나갔다 들어오면 "최초 방문 False"가 출력됨
- 시작 방도 최초 방문으로 기록됨
- `PlayerDoorInteractionController`가 수정 없이 그대로 동작함

---

## 다음 개발 방향

다음 21일차부터는 371일 표 기준 **던전 생성**(26~35일차, 새 번호 기준) 이전에 남은 던전 탐험 구간 마무리와 맵 UI(36~40일차) 쪽으로 이어진다. 우선 순위가 높은 항목은 다음과 같다.

```text
지도 갱신 (TODO로 남겨둔 자리) — 3.2.6절 지도 표시 규칙 연결 준비
↓
18일차 미로 방 10개 중 하나를 실제로 DungeonScene에 배치해 EnterRoom 흐름 검증
↓
자동 저장 요청 자리 — RunContext/SaveService를 실제 이동 흐름에 연결
```
