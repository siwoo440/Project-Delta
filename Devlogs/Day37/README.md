# Project Delta - 37일차 개발일지

## 개발 주제

**GeneratedDungeon 그래프 기반 미니맵 및 탐색형 지도 공개 시스템 구현**

36일차까지 절차 생성된 던전을 실제 `RoomView`로 배치하고 플레이어가 방 사이를 이동할 수 있도록 연결했다.

37일차에서는 이 `GeneratedDungeon`의 방 그래프를 이용해 화면 우측 상단에 실제 던전 구조를 표시하는 미니맵을 구현하고, 플레이어가 이동하면서 주변 방을 하나씩 밝혀가는 탐색형 지도 시스템을 추가했다.

---

## 개발 목표

- `GeneratedDungeon.Layout` 기반 방 단위 미니맵 구현
- `RoomNode.MacroCoordinate`를 UI 좌표로 변환
- 방문 / 미방문 / 현재 방 상태 구분
- 플레이어 방향 아이콘 표시
- 현재 방을 중심으로 지도 위치 갱신
- M키 전체 지도 패널 열기/닫기
- Esc키 전체 지도 패널 닫기
- 현재 방 주변 8칸의 방만 최초 공개
- 아직 발견하지 않은 먼 방은 지도에서 숨김
- 한 번 발견한 방은 멀어져도 계속 지도에 유지
- 층이 변경되면 발견 정보 초기화
- 지도 상태 계산과 발견 범위에 대한 EditMode 테스트 추가

---

## 1. GeneratedDungeon 기반 미니맵 전환

기존 미니맵은 현재 방 내부의 5×5 그리드를 중심으로 표시하는 구조였다.

37일차에서는 던전 생성 시스템이 완성된 상태이므로 다음 데이터로 실제 층 구조를 표시하도록 변경했다.

```text
GeneratedDungeon
└─ DungeonLayoutGraph
   └─ RoomNode
      ├─ RoomId
      ├─ MacroCoordinate
      └─ Connections
```

각 `RoomNode` 하나가 지도에서 방 아이콘 하나가 된다.

---

## 2. DungeonMinimapSnapshot 추가

미니맵 표시용 데이터를 게임 로직과 분리하기 위해 `DungeonMinimapSnapshot` 구조를 추가했다.

각 방은 다음 정보를 가진다.

```text
RoomId
MacroCoordinate
State
```

방 상태는 다음 세 가지로 구분한다.

```text
Unvisited
Visited
Current
```

현재 방은 항상 `Current` 상태가 우선 적용된다.

---

## 3. MacroCoordinate 기반 지도 배치

실제 던전 생성에서 사용하는 `MacroCoordinate`를 그대로 지도 좌표로 사용한다.

예:

```text
Macro (0, 0)
→ 지도 중심

Macro (1, 0)
→ 오른쪽

Macro (-1, 0)
→ 왼쪽

Macro (0, 1)
→ 위쪽

Macro (0, -1)
→ 아래쪽
```

현재 방을 기준 좌표로 두고 다른 방의 상대 위치를 계산한다.

따라서 플레이어가 이동하면 현재 방이 미니맵 중앙에 오고 주변 지도 구조가 상대적으로 이동한다.

---

## 4. 방문 상태 표시

기존 `RoomInstance.Visited` 데이터를 이용해 지도 상태를 구분한다.

```text
미방문 방
→ 어두운 회색

방문한 방
→ 밝은 회색

현재 방
→ 노란색 강조
```

현재 방은 방문 여부와 관계없이 항상 현재 방 상태로 표시한다.

---

## 5. 플레이어 방향 표시

현재 방 중심에 플레이어 방향을 나타내는 삼각형 아이콘을 표시한다.

카메라 또는 시점 Transform의 Y 회전을 기준으로:

```text
North
East
South
West
```

방향을 계산하고 지도 아이콘을 회전시킨다.

---

## 6. M키 전체 지도 패널

37일차 작업 과정에서 기존 M키 전체 지도 입력이 그래프 미니맵 전환 과정에서 빠지는 문제가 있었다.

이를 다시 연결했다.

```text
M
→ 전체 지도 열기

M 다시 입력
→ 전체 지도 닫기

Esc
→ 전체 지도 닫기
```

전체 지도에서도 우측 상단 미니맵과 같은 GeneratedDungeon 데이터를 사용한다.

현재 38일차 기능인 연결선, 확대/축소, 탐험률 등은 아직 포함하지 않는다.

---

## 7. 주변 8칸 탐색 공개

모든 생성 방을 처음부터 보여주지 않고 플레이어가 이동하면서 지도를 밝혀가도록 변경했다.

현재 방을 중심으로 다음 3×3 범위를 검사한다.

```text
□ □ □
□ P □
□ □ □
```

`P`는 현재 방이며 주변 8칸에 실제 방이 존재할 경우 해당 방을 발견 상태로 등록한다.

조건:

```text
|Room.X - Current.X| <= 1
|Room.Z - Current.Z| <= 1
```

따라서 대각선 방도 발견 범위에 포함된다.

---

## 8. 미발견 방 숨김

아직 플레이어 주변 탐색 범위에 한 번도 들어오지 않은 방은 지도에서 완전히 숨긴다.

```text
발견 전
→ 지도에 표시하지 않음

발견 후 미방문
→ 미방문 회색 방으로 표시

직접 방문
→ 방문 방으로 표시
```

이 규칙은 우측 상단 미니맵과 M키 전체 지도에 동일하게 적용한다.

전체 지도를 열어도 아직 발견하지 않은 던전 구조를 미리 확인할 수 없다.

---

## 9. 발견한 방 유지

`DungeonMinimapRevealTracker`가 현재 층에서 발견한 `RoomId`를 `HashSet<string>`으로 기억한다.

```text
현재 방 주변에서 발견
↓
revealedRoomIds 등록
↓
플레이어가 다른 방으로 이동
↓
이전 방에서 발견한 방도 계속 지도에 표시
```

따라서 플레이어가 던전을 이동할수록 지도에 보이는 영역이 점점 넓어진다.

---

## 10. 층 변경 시 발견 상태 초기화

`DungeonMinimapRevealTracker`는 현재 `GeneratedDungeon` 인스턴스를 추적한다.

새 층 생성으로 `GeneratedDungeon`이 변경되면:

```text
기존 revealedRoomIds 제거
↓
새 층 EntryRoom 기준 주변 탐색
↓
새 지도 탐색 시작
```

으로 처리한다.

37일차에서는 이 발견 정보를 현재 플레이 세션의 현재 층에서만 유지한다.

게임 종료 후 발견 정보 저장 및 복원은 39일차 저장/복원 단계에서 다룬다.

---

## 11. 숨겨진 방을 지도 배율 계산에서 제외

미발견 방이 화면에는 보이지 않더라도 지도 크기 계산에 포함되면 발견된 방들이 지나치게 작게 표시될 수 있다.

이를 방지하기 위해 지도 배율 계산에서도:

```text
revealedRoomIds에 존재하는 방
```

만 사용하도록 처리했다.

따라서 아직 보이지 않는 먼 방 때문에 현재 미니맵이 축소되지 않는다.

---

## 12. EditMode 테스트 추가

37일차 미니맵 데이터와 탐색 공개 규칙을 확인하기 위한 테스트를 추가했다.

### DungeonMinimapSnapshotTests

검증 항목:

```text
GeneratedDungeon의 모든 방 Snapshot 생성
미등록 방 Unvisited 처리
방문 방 Visited 처리
현재 방 Current 우선 처리
현재 방 기준 상대 좌표 계산
이전 층 RoomInstance 무시
```

### DungeonMinimapRevealTrackerTests

검증 항목:

```text
현재 방 + 주변 8칸 공개
멀어진 뒤에도 기존 발견 방 유지
새 GeneratedDungeon으로 변경 시 발견 정보 초기화
미등록 RoomId는 미발견 처리
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/
├─ DungeonMinimapSnapshot.cs
├─ DungeonMinimapSnapshot.cs.meta
├─ DungeonMinimapRevealTracker.cs
└─ DungeonMinimapRevealTracker.cs.meta

Assets/ProjectDelta/Tests/EditMode/
├─ DungeonMinimapSnapshotTests.cs
├─ DungeonMinimapSnapshotTests.cs.meta
├─ DungeonMinimapRevealTrackerTests.cs
└─ DungeonMinimapRevealTrackerTests.cs.meta
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Presentation/
└─ DungeonMinimapController.cs
```

---

## 저장소 정리

최신 37일차 커밋에는 기능 구현 외에 비어 있던 일부 placeholder 폴더 관련 파일 제거도 포함되어 있다.

```text
Assets/Editor.meta

Assets/ProjectDelta/AddressableAssets.meta
Assets/ProjectDelta/AddressableAssets/.gitkeep

Assets/ThirdParty.meta
Assets/ThirdParty/.gitkeep
```

현재 37일차 지도 기능 자체와 직접 연결된 파일은 아니다.

---

## 현재 지도 동작

```text
게임 시작
↓
EntryRoom 현재 방 표시
↓
현재 방 주변 8칸의 실제 방 공개
↓
멀리 있는 방은 숨김
↓
방 이동
↓
새 주변 방 추가 공개
↓
이전에 발견한 방 유지
↓
직접 방문한 방은 방문 상태로 변경
```

전체 지도:

```text
M
→ 현재까지 발견한 영역을 큰 지도 패널로 표시

M / Esc
→ 전체 지도 닫기
```

---

## 37일차 완료 기준

다음 항목을 만족하면 37일차 목표가 완료된 것으로 본다.

```text
GeneratedDungeon 방 구조가 미니맵에 표시된다.
현재 방이 지도 중앙에 표시된다.
미방문 / 방문 / 현재 방 상태가 구분된다.
플레이어 방향이 표시된다.
현재 방 주변 8칸만 새롭게 공개된다.
멀리 있는 미발견 방은 표시되지 않는다.
한 번 발견한 방은 멀어져도 계속 표시된다.
M키로 전체 지도 패널을 열고 닫을 수 있다.
Esc키로 전체 지도 패널을 닫을 수 있다.
다음 층에서는 발견 상태가 초기화된다.
```

---

## 검증 상태

최신 저장소 커밋:

```text
5ae2256f761519fc15ada331b65922eae63743bc
```

GitHub에는 이 커밋에 연결된 CI Status 또는 Workflow Run이 등록되어 있지 않다.

따라서 Unity Editor 컴파일과 EditMode Test Runner의 실제 통과 여부는 로컬 Unity 실행 결과로 확인해야 한다.

---

## 다음 개발 방향

### 38일차

37일차에서 구축한 `DungeonMinimapSnapshot`과 발견 정보를 기반으로 전체 지도 기능을 확장한다.

주요 작업:

```text
방 사이 연결선 표시
전체 지도 중앙 정렬
확대 / 축소
탐험률 표시
계단까지의 거리 표시
층별 진행도 표시
```

37일차에서 만든 방 좌표와 발견 상태를 그대로 활용하면 전체 지도 UI를 별도의 던전 데이터 구조 없이 확장할 수 있다.
