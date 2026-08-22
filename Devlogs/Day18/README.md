# Project Delta - 18일차 개발일지

## 개발 주제

**RoomDefinition / RoomInstance 데이터 구조 도입 및 미로형 방 프리팹 10종 제작**

기획서 10.2절 정적 정의 데이터 목록의 "RoomDefinition | 방 프리팹과 연결 규칙"과 10.3절 "던전 생성은 Unity 오브젝트를 배치하기 전에 순수 데이터 구조를 먼저 만든다" 원칙에 따라, 14~15일차에 하드코딩되어 있던 방 통로 배치를 데이터로 옮겼다. 추가로 내부에 벽을 둔 미로형 방 프리팹 10종을 요청받아 함께 제작했다.

---

## 개발 목표

- 방의 문/벽 배치를 코드가 아닌 데이터(`RoomDefinition`)로 표현
- 정의 데이터(Data)와 런타임 배치 상태(Domain)를 분리 (`RoomInstance`)
- `RoomPassageController`의 하드코딩된 `ConfigurePrimaryRoom()`/`ConfigureSecondaryRoom()` 제거
- 내부에 벽이 있는 5×5 미로형 방 프리팹 10종 제작 (모든 칸 연결 보장)
- 데이터(RoomDefinition)와 시각(프리팹의 실제 벽 오브젝트)이 항상 일치하도록 구성

---

## 구현 내용

### 1. PassageEntry — 계층 간 순환 참조 방지

`RoomDefinition`(Data)이 문/벽 배치 데이터를 가지려면 `CardinalDirection`/`PassageType`(Domain 소속)이 필요하다. 반대로 `RoomInstance`(Domain)가 `RoomDefinition`을 직접 참조하면 Domain이 Data를 참조하게 된다.

```text
PassageEntry (Domain, 순수 데이터 구조체)
├─ X, Z
├─ Direction (CardinalDirection)
├─ Type (PassageType)
└─ IsLocked
```

이 구조체를 Domain에 두고, `RoomDefinition`은 이를 목록으로 담기만 하며, `RoomInstance.Create()`는 `RoomDefinition`이 아니라 `PassageEntry` 목록만 받는다. Data → Domain 참조만 생기고 반대 방향은 생기지 않는다 (`ProjectDelta.Data.asmdef`에 `ProjectDelta.Domain` 참조 추가).

---

### 2. RoomDefinition (Data)

3일차의 `DefinitionBase`를 그대로 상속한다.

```text
RoomDefinition
├─ Id (DefinitionBase 상속)
├─ Width, Height
└─ Passages: List<PassageEntry>
```

---

### 3. RoomInstance (Domain)

```text
RoomInstance.Create(roomId, definitionId, passages)
→ 빈 RoomGridLayout 생성
→ 각 PassageEntry를 실제 GridPassage로 변환해 등록
→ RoomInstance 반환
```

14일차에 만든 `RoomGridLayout`/`GridPassage`를 그대로 재사용한다 — 데이터 출처만 코드에서 정의 데이터로 바뀌었다.

---

### 4. RoomPassageController 하드코딩 제거

```text
Before: Awake()에서 layout.SetPassage()를 코드로 직접 여러 번 호출
After:  Awake()에서 RoomInstance.Create(roomId, definition.Id, definition.Passages)로 위임
```

`[SerializeField] private RoomDefinition roomDefinition;` 필드를 추가했고, 미지정 시 에러 로그를 남기고 모든 방향이 열린 상태로 안전하게 동작하도록 했다 (크래시 방지).

---

### 5. 미로형 방 프리팹 10종 (추가 요청)

내부에 벽을 둔 5×5 방을 10개 더 만들어 달라는 요청에 따라, 데이터(RoomDefinition)와 시각(프리팹의 벽 오브젝트)을 **하나의 생성 스크립트**로 함께 만들어 둘이 항상 일치하도록 구성했다.

```text
생성 절차
25칸에 대해 재귀 백트래커로 신장 트리 생성 (모든 칸 연결 보장)
↓
방마다 2~4개 추가 통로를 랜덤으로 열어 순환 경로 추가
↓
BFS로 문에서 25칸 전부 도달 가능한지 자체 검증
↓
같은 통로 목록으로 RoomDefinition 에셋 + 프리팹 벽 오브젝트를 함께 생성
```

각 방은 남쪽 벽 중앙에 문이 하나 있고, 내부는 방마다 다른 미로형 벽 배치를 갖는다. 벽 메시는 검증된 기존 `TestRoom_A`의 `Wall_Internal` 오브젝트(위치·크기 공식)를 그대로 재사용했다.

```text
생성 결과 (10개)
Assets/ProjectDelta/Prefabs/Dungeon/Room_Maze_01~10.prefab
Assets/ProjectDelta/Data/Rooms/RoomDefinition_Maze01~10.asset
```

---

## 적용 중 발견된 문제 및 수정

### 6. 인접한 내부 벽 사이에 틈 발생

생성된 프리팹을 확인한 결과, 내부 벽이 여러 개 연달아 있는 구간에서 벽 사이에 틈이 보였다.

원인: 벽 조각 크기를 문(Door) 전용으로 검증했던 값(칸 너비의 80%인 1.6)으로 모든 벽에 동일하게 적용했다. `TestRoom_A`에는 인접한 내부 벽이 두 개 이상 연달아 있는 구간이 없어서 이 문제가 드러나지 않았는데, 미로 생성기는 같은 줄에 벽을 여러 개 연속 배치하는 경우가 흔해 칸과 칸 사이 0.4 유닛 틈이 시각적으로 드러났다.

```text
수정 전: 모든 벽 1.6 (문과 동일)
수정 후: 일반 벽(Wall) 2.0(칸 전체 폭) / 문(Door)만 1.6 유지
```

10개 방을 전부 다시 생성해 반영했다. `RoomDefinition`의 통로 논리 데이터는 변경되지 않았고, 벽 조각의 실제 크기만 조정되었다.

---

## 현재 18일차 전체 흐름

```text
PassageEntry를 Domain에 두어 Data↔Domain 순환 방지
↓
RoomDefinition(Data) / RoomInstance(Domain) 구현
↓
RoomPassageController의 하드코딩 제거, 데이터 기반으로 전환
↓
미로형 방 프리팹 10종 생성 (데이터+시각 동시 생성, 연결성 자체 검증)
↓
인접 벽 틈 발견 → 벽 폭 수정 → 10개 재생성
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/RoomInstance.cs
Assets/ProjectDelta/Scripts/Data/RoomDefinition.cs
Assets/ProjectDelta/Prefabs/Dungeon/Room_Maze_01~10.prefab (+.meta)
Assets/ProjectDelta/Data/Rooms/RoomDefinition_Maze01~10.asset (+.meta)
Devlogs/Day18/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Data/ProjectDelta.Data.asmdef (Domain 참조 추가)
Assets/ProjectDelta/Scripts/Presentation/RoomPassageController.cs (하드코딩 제거)
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

18일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- `RoomInstance.Create()`가 `PassageEntry` 목록으로부터 올바른 `RoomGridLayout`을 생성함
- `RoomPassageController`가 하드코딩 없이 `RoomDefinition`만으로 동작함
- 미로형 방 프리팹 10개 모두 문이 정확히 1개씩 존재
- 미로형 방 프리팹 10개 모두 25칸 전체가 연결됨 (생성 스크립트 자체 검증)
- 인접한 벽 사이 시각적 틈이 없음 (사용자 확인 완료)

**미완료 항목 — 다음에 이어서 처리 필요**

- `TestRoom_A`/`TestRoom_B`에 실제 `RoomDefinition` 에셋이 아직 연결되지 않은 상태다. 현재 `DungeonScene`을 실행하면 두 방 모두 "RoomDefinition이 지정되지 않았습니다" 에러가 계속 발생한다. `RoomDefinition_TestRoom_A`/`RoomDefinition_TestRoom_B` 에셋을 만들어 각 방의 `Room Passage Controller`에 연결해야 이 구간이 완전히 끝난다.

---

## 다음 개발 방향

다음 19일차에는 **RoomView 프리팹**을 구현한다. 오늘 만든 미로 프리팹 10개가 이 작업의 좋은 출발점이 된다 — 다만 지금은 방 통로/벽만 있고 계단·상자·비밀 벽·NPC 상호작용 지점 같은 실제 콘텐츠 배치 지점은 없다. 그 전에 `TestRoom_A`/`TestRoom_B`의 `RoomDefinition` 연결부터 마무리한다.

예정 흐름:

```text
TestRoom_A / TestRoom_B RoomDefinition 연결 마무리 (이월)
↓
RoomView 표준 구조 정의 (기획서 10.3절: 테마·문·계단·상자·비밀 벽·NPC 지점·환경 소품)
↓
미로 프리팹 10개를 RoomView 표준에 맞게 확장
↓
RoomView가 조우 확률·이벤트 결과를 직접 결정하지 않도록 표시 전용으로 유지
```
