# Project Delta - 36일차 개발일지

## 개발 주제

**GeneratedDungeon 실제 RoomView 배치 및 절차 생성 던전 플레이 연결**

35일차까지는 던전 생성 결과가 `GeneratedDungeon`과 `DungeonLayoutGraph` 형태의 논리 데이터로만 존재했다.

36일차에서는 이 데이터를 실제 Unity Scene의 `RoomView` 프리팹으로 배치하고, 플레이어 시작 위치, 방 사이 문 연결, 계단, 천장, 그리드 표시와 기존 테스트 방 정리까지 연결했다.

---

## 개발 목표

- `GeneratedDungeon`을 실제 Unity GameObject로 변환
- `RoomNode.DefinitionId`와 실제 `RoomView` 프리팹 연결
- `MacroCoordinate`를 실제 월드 좌표로 변환
- 생성된 모든 방을 `RoomView`로 Instantiate
- `RoomId`와 생성된 `RoomView` 연결
- 실제 `RoomExit` 쌍을 기준으로 방 사이 문 연결
- 연결되지 않은 출구를 고정 벽으로 처리
- 연결된 문을 회색으로 시각 구분
- 문 위 빈 공간 제거
- 생성 던전에 천장 추가
- 생성 던전에 이동 그리드 표시
- 플레이어가 생성된 EntryRoom에서 시작
- 생성 맵이 플레이어 Transform을 따라 움직이지 않도록 수정
- 기존 `TestRoom_A`, `TestRoom_B` 제거
- `StairsRoom`에 런타임 계단 배치
- 다음 층 이동 시 새 절차 던전 생성

---

## 구현 내용

### 1. DungeonRoomPrefabBinding 추가

논리 방 정의와 실제 Unity 프리팹을 연결하기 위한 바인딩 구조를 추가했다.

```text
DungeonRoomPrefabBinding
├─ RoomDefinition
├─ RoomView Prefab
├─ UseAsEntry
└─ IncludeInGenerationPool
```

이를 통해:

```text
DefinitionId
→ RoomDefinition
→ RoomView Prefab
```

형태로 실제 생성 프리팹을 찾을 수 있게 했다.

---

## 2. DungeonFloorController 절차 생성 연결

기존 `DungeonFloorController`는 층 이동 시 지정된 프리팹 하나만 생성하는 자리표시자 구조였다.

36일차에서는 다음 흐름으로 확장했다.

```text
DungeonGenerationService
↓
Validated GeneratedDungeon
↓
DungeonFloorController
↓
RoomNode 전체 순회
↓
RoomView Instantiate
```

기존 자리표시자 방식은 절차 생성 기능을 사용하지 않을 때의 fallback으로 유지했다.

---

## 3. 실제 방 배치

각 `RoomNode`의:

```text
MacroCoordinate
```

를 실제 Unity 월드 좌표로 변환한다.

현재 기준:

```text
RoomWorldSize = 10

(0, 0)  → (0, 0, 0)
(1, 0)  → (10, 0, 0)
(0, 1)  → (0, 0, 10)
(-1, 0) → (-10, 0, 0)
```

각 방은 생성된 층 루트 아래에 배치된다.

```text
GeneratedFloor_<Floor>_Seed_<Seed>
├─ Room_...
├─ Room_...
└─ ...
```

---

## 4. 생성 맵 월드 고정

초기 구현에서는 생성 층 루트가 `DungeonFloorController.transform`의 자식으로 생성되었다.

`DungeonFloorController`가 Player GameObject에 붙어 있었기 때문에 플레이어가 움직일 때 생성 던전 전체가 함께 이동하는 문제가 발생했다.

이를 수정하여 생성 층 루트를 Scene 최상위에 배치하도록 변경했다.

```text
Player

GeneratedFloor
├─ Room
├─ Room
└─ ...
```

이후 플레이어 이동과 생성 맵 Transform이 분리됐다.

---

## 5. RoomId와 RoomView 런타임 연결

같은 프리팹을 여러 생성 방에서 재사용할 수 있도록 `RoomPassageController`를 런타임 정보로 다시 초기화한다.

```text
RoomNode.RoomId
RoomDefinition
↓
RoomPassageController.ConfigureRuntime()
```

따라서 실제 생성된 각 방은 고유한 `RoomId`를 가진다.

---

## 6. 정확한 RoomExit 기반 문 연결

34일차에서 `RoomConnectionEdge`에 저장한:

```text
LocalExit
NeighborExit
```

을 실제 RoomView의 `RoomExitMarker`와 비교한다.

연결 흐름:

```text
RoomConnectionEdge
↓
LocalExit / NeighborExit
↓
RoomView.FindExitMarker()
↓
실제 프리팹 출구 확인
↓
공유 GridPassage 생성
```

그래프 출구와 실제 프리팹 출구가 일치하지 않으면 연결 실패로 처리한다.

---

## 7. 두 방이 같은 문 상태 공유

실제로 연결된 두 방에는 같은 `GridPassage` 객체를 전달한다.

예:

```text
Room A East
      │
      │ Shared GridPassage
      │
Room B West
```

따라서 한쪽에서 문 상태가 바뀌면 반대쪽에서도 같은 상태를 사용한다.

---

## 8. 생성 방 이동 연결

기존 `TestRoomTransitionController`는 `TestRoom_A`, `TestRoom_B` 두 방만 이동할 수 있었다.

36일차에서는 절차 생성 던전이 존재하면 `GeneratedDungeon`의 그래프를 우선 사용하도록 확장했다.

```text
GeneratedDungeon 존재
→ 그래프 연결 기준 방 전환

GeneratedDungeon 없음
→ 기존 TestRoom A/B 방식
```

이를 통해 생성된 방 사이를 실제 연결 방향과 출구 위치를 기준으로 이동할 수 있다.

---

## 9. 플레이어 EntryRoom 시작

던전 배치 완료 후:

```text
GeneratedDungeon.EntryRoom
↓
RoomView 검색
↓
플레이어 Transform 즉시 이동
↓
EnterRoom()
```

순서로 시작 방을 지정한다.

기존 테스트 맵 위치에서 새 방까지 이동 애니메이션처럼 끌려가지 않도록 플레이어를 먼저 EntryRoom 월드 위치로 이동시킨 뒤 `EnterRoom()`을 실행한다.

---

## 10. 기존 TestRoom_A/B 정리

절차 생성 모드에서는 기존 Scene에 배치되어 있던 `TestRoom_A`, `TestRoom_B`가 더 이상 실제 플레이에 필요하지 않다.

36일차 설정 메뉴 실행 시 두 테스트 방을 DungeonScene에서 제거하고 Scene을 저장하도록 정리했다.

런타임에서도 절차 생성이 활성화된 경우 기존 `RoomView`를 제거하도록 안전 처리를 유지한다.

---

## 11. Day36 자동 설정 메뉴

다음 Editor 메뉴를 추가했다.

```text
Project Delta
→ Day36
→ Configure Procedural Dungeon Floor
```

이 메뉴는:

```text
절차 생성 활성화
첫 층 자동 생성 활성화
Day31 RoomDefinition/Prefab 연결
Player 참조 연결
생성 규칙 기본값 설정
TestRoom_A/B 제거
Scene 저장
```

을 처리한다.

---

## 12. Day31 다중 출구 방 활용

36일차 검증용 생성 방으로 Day31에서 만든 다음 테스트 프리팹을 연결한다.

```text
Room_Test_NS
Room_Test_NE
Room_Test_T
Room_Test_CROSS
```

현재 기본 자동 생성 풀에서는 안정적인 초기 검증을 위해 `ROOM_TEST_CROSS`를 중심으로 사용한다.

---

## 13. 런타임 계단 생성

`GeneratedDungeon.StairsRoom`에만 런타임 계단을 생성한다.

```text
StairsRoom
└─ Runtime_Stairs
```

계단에는 `RoomContentMarker`를 추가한다.

```text
RoomContentType.Stairs
GridPosition.Zero
```

기존 계단 상호작용 시스템이 이 마커를 찾을 수 있도록 `RoomView.RefreshMarkers()`를 호출한다.

---

## 14. 다음 층 절차 생성

계단을 사용하면:

```text
AdvanceFloor
↓
기존 GeneratedFloor 제거
↓
새 RequestedSeed 계산
↓
DungeonGenerationService.GenerateWithRetry()
↓
새 GeneratedDungeon 생성
↓
RoomView 전체 재배치
↓
새 EntryRoom으로 플레이어 이동
```

순서로 다음 층을 생성한다.

---

## 15. 생성 방 천장 추가

초기 생성 방에는 천장이 없어 방 위쪽이 열린 상태였다.

각 생성 `RoomView`에 런타임 `Ceiling`을 추가하도록 변경했다.

현재 기준:

```text
Room 크기 = 10 × 10
천장 Y ≈ 2.55
천장 두께 ≈ 0.1
```

바닥 Renderer의 Material을 찾아 천장에도 같은 Material을 적용한다.

---

## 16. 생성 방 그리드 표시 복구

Day31 생성 프리팹에는 기존 `GridFloorGuideController`가 포함되어 있지 않아 절차 생성 방에서 이동 그리드 선이 표시되지 않았다.

36일차에서 생성된 RoomView에:

```text
GridFloorGuideController
```

가 없으면 런타임으로 자동 추가한다.

기본 그리드:

```text
X = -2 ~ 2
Z = -2 ~ 2
CellSize = 2
```

총 5×5 이동 칸이 다시 표시된다.

---

## 17. 생성 문 회색 표시

실제로 연결되어 사용할 수 있는 문은 벽과 쉽게 구분할 수 있도록 회색으로 표시한다.

`MaterialPropertyBlock`을 사용하므로 원본 Material 자산 자체는 변경하지 않는다.

```text
실제 Door
→ 회색 DoorVisual

고정 벽
→ 기존 벽 Material
```

---

## 18. 문 위 빈 공간 수정

Day31 방 규격은:

```text
WallHeight = 2.5
DoorHeight = 2.2
```

이므로 문 위에:

```text
0.3
```

높이의 빈 공간이 존재했다.

36일차 후반 수정에서 실제 Door가 있는 경우:

```text
Door
+
GeneratedDoorLintel
```

구조로 변경했다.

```text
벽 상단
████████████  ← 고정 상부 벽
┌── Door ──┐
```

문이 열리더라도 상부 벽은 그대로 유지된다.

---

## 19. 연결되지 않은 출구를 고정 벽으로 처리

기존에는 연결되지 않은 출구에도 `DoorVisual`이 남아 있어 실제 문인지 막힌 벽인지 구분하기 어려웠다.

이를 다음과 같이 변경했다.

```text
GridPassage.Type == Door
→ 회색 DoorVisual
→ 상부 고정 벽

GridPassage.Type != Door
→ DoorVisual 숨김
→ GeneratedWallBlocker 표시
```

`GeneratedWallBlocker`는 주변 `Wall_*` Renderer의 Material을 사용한다.

따라서 연결되지 않은 출구는 시각적으로 실제 고정 벽처럼 보인다.

---

## 20. RoomContentMarker 런타임 설정

런타임으로 생성하는 계단에 콘텐츠 마커를 붙일 수 있도록 `RoomContentMarker.Configure()`를 추가했다.

```text
ContentType
GridPosition
```

을 생성 후 코드에서 설정할 수 있다.

---

## 21. RoomView 런타임 마커 갱신

동적으로 계단 등의 콘텐츠 마커가 추가되므로 `RoomView`에:

```text
RefreshMarkers()
```

를 추가했다.

또한 그래프의 정확한 `RoomExit`과 실제 프리팹 마커를 연결하기 위해:

```text
FindExitMarker(RoomExit)
```

를 추가했다.

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Editor/Day36ProceduralFloorSetup.cs
Assets/ProjectDelta/Scripts/Editor/Day36ProceduralFloorSetup.cs.meta

Assets/ProjectDelta/Scripts/Presentation/DungeonRoomPrefabBinding.cs
Assets/ProjectDelta/Scripts/Presentation/DungeonRoomPrefabBinding.cs.meta
```

---

## 수정 파일

```text
Assets/ProjectDelta/Prefabs/Dungeon/Day31/Room_Test_CROSS.prefab
Assets/ProjectDelta/Prefabs/Dungeon/Day31/Room_Test_NE.prefab
Assets/ProjectDelta/Prefabs/Dungeon/Day31/Room_Test_NS.prefab
Assets/ProjectDelta/Prefabs/Dungeon/Day31/Room_Test_T.prefab

Assets/ProjectDelta/Scenes/DungeonScene.unity

Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs
Assets/ProjectDelta/Scripts/Presentation/RoomContentMarker.cs
Assets/ProjectDelta/Scripts/Presentation/RoomPassageController.cs
Assets/ProjectDelta/Scripts/Presentation/RoomView.cs
Assets/ProjectDelta/Scripts/Presentation/TestRoomTransitionController.cs
```

---

## 삭제 파일

별도 프로젝트 파일 삭제는 없다.

다만 `DungeonScene` 내부의 기존 테스트 오브젝트:

```text
TestRoom_A
TestRoom_B
```

는 Scene 구성에서 제거했다.

---

## 36일차 문제 수정 기록

### DungeonRunState 이름 충돌

`ProjectDelta.Data`와 `ProjectDelta.Domain`에 같은 이름의 `DungeonRunState`가 있어 CS0104가 발생했다.

다음 alias로 실제 사용 타입을 명시했다.

```csharp
using DungeonRunState = ProjectDelta.Domain.DungeonRunState;
```

### 생성 맵이 플레이어를 따라가는 문제

생성 층 루트가 Player의 자식으로 만들어지는 문제를 수정했다.

```text
기존
Player
└─ GeneratedFloor

수정
Player

GeneratedFloor
```

### 기존 테스트 맵과 생성 맵 중복

절차 생성 시작 시 기존 RoomView를 제거하고, Editor 설정 메뉴에서도 `TestRoom_A/B`를 Scene에서 정리했다.

### 생성 방 그리드 미표시

생성 RoomView에 `GridFloorGuideController`를 자동 추가하도록 수정했다.

### 방 천장 없음

모든 생성 방에 런타임 Ceiling을 추가했다.

### 문과 벽 구분 문제

실제 연결 Door는 회색으로 표시하고, 연결되지 않은 출구는 주변 벽과 같은 고정 벽으로 변환했다.

### 문 위 빈 공간

WallHeight와 DoorHeight 차이인 0.3을 `GeneratedDoorLintel` 고정 벽으로 채웠다.

---

## 현재 확인 사항

36일차 구현은 저장소 기준으로 35일차 이후 하나의 커밋에 정리되어 있다.

최신 커밋:

```text
0d9001262353bf2b0e46f1a3b6e66811d1e26615
```

현재 GitHub에는 해당 커밋의 CI Status가 등록되어 있지 않다.

따라서 Unity Editor의 실제 컴파일 및 Play 동작 확인은 로컬 실행 결과를 기준으로 판단해야 한다.

---

## 36일차 완료 판단

**36일차 목표인 GeneratedDungeon의 실제 RoomView 배치, 문 연결, 계단 배치 및 절차 생성 던전의 기본 플레이 연결은 완료되었다.**

추가로 실제 플레이 과정에서 발견된:

```text
생성 맵 Parent 문제
기존 TestRoom 중복
천장 누락
그리드 누락
문/벽 시각 구분
문 상단 빈 공간
```

까지 같은 일차에서 정리했다.

35일차까지 데이터로만 존재하던 절차 생성 던전이 이제 실제 Unity Scene에 표시되고 플레이어가 생성된 EntryRoom에서 시작할 수 있는 단계까지 연결되었다.

---

## 다음 개발 방향

### 37일차

36일차에서 실제 던전 배치까지 연결됐으므로 다음 단계에서는 생성된 방의 실제 플레이 흐름을 안정화하면 된다.

우선 검토할 항목:

```text
방 진입/이탈 상태 관리
방 방문 상태 기록
현재 RoomId 갱신 검증
문 상호작용과 방 이동 안정화
계단 방 진입/사용 검증
층 이동 후 이전 층 데이터 정리
카메라와 생성 방 표시 범위
런타임 RoomView 관리
```

이후 전투 방, 방 콘텐츠, 방 클리어 상태와 실제 생성 던전 상태를 연결할 수 있다.
