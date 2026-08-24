# Project Delta - 39일차 개발일지

## 개발 목표

38일차까지 구현한 던전 지도 기능을 실제 런 저장 시스템과 연결한다.

- 지도 방문·발견 정보 저장 및 복원
- 현재 던전 Seed 저장 및 복원
- 생성된 던전 레이아웃 저장 및 복원
- 현재 층·현재 방·방 내부 위치 복원
- 이어하기 시 기존 레이아웃을 그대로 재배치
- 저장/복원 관련 EditMode 테스트 추가

---

## 구현 내용

### 1. 던전 저장 데이터 확장

`RunData`의 던전 저장 데이터에 현재 층의 생성 결과를 보존할 수 있도록 다음 데이터를 추가했다.

- `DungeonSeed`
- `DungeonLayoutSnapshot`
- 발견된 방 RoomId 목록
- 방별 좌표
- 방별 연결 방향
- 방문 여부
- 발견 여부
- 계단 방 여부 및 계단 발견 여부

이를 통해 단순히 같은 Seed로 다시 생성하는 방식이 아니라 저장 당시 확정된 방 좌표와 연결 구조 자체를 복원할 수 있도록 했다.

### 2. 런타임 던전 상태와 저장 데이터 연결

`DungeonRunState`가 현재 층의 다음 정보를 직접 보관하도록 확장했다.

- 현재 `GeneratedDungeon`
- 현재 Seed
- 현재 `DungeonLayoutSnapshot`
- 발견된 RoomId 목록

새 층으로 이동하면 이전 층의 방 레지스트리, 발견 정보, 생성 레이아웃과 Seed를 초기화하도록 구성했다.

### 3. DungeonSaveMapper 저장·복원 확장

기존 `RunContext → RunData` 변환 흐름을 유지하면서 지도와 생성 던전 정보를 함께 저장하도록 수정했다.

저장 시 다음 순서로 처리한다.

1. 현재 방 기준 지도 발견 상태 동기화
2. 현재 층과 현재 방·그리드 위치 저장
3. 현재 던전 Seed 저장
4. 레이아웃 Snapshot 저장
5. 방별 방문·발견·완료 상태 저장
6. 방 좌표와 연결 방향 저장

이어하기 시에는 저장된 레이아웃 Snapshot을 복원하고 발견된 방 정보를 다시 `DungeonRunState`에 적용한다.

### 4. 저장된 던전 실제 재배치

`DungeonFloorController`에 새 던전 생성 경로와 별도로 저장된 던전을 복원하는 경로를 추가했다.

이어하기에서 저장된 `GeneratedDungeon`이 존재하면 새 Seed로 던전을 생성하지 않고 저장 데이터에서 복원한 그래프를 이용해 RoomView를 다시 배치한다.

복원 대상에는 다음 정보가 포함된다.

- Entry Room
- Stairs Room
- 방별 Macro 좌표
- 방 연결 관계
- 정확한 출구 연결 쌍
- 저장 당시 Seed

플레이어는 저장 당시 RoomId와 방 내부 GridPosition을 기준으로 복원된다.

### 5. 지도 발견 상태 복원

`DungeonMinimapRevealTracker`에 저장된 RoomId 목록을 다시 주입할 수 있는 복원 기능을 추가했다.

`DungeonMinimapController`는 이어하기로 복원된 던전을 처음 확인할 때 저장된 발견 정보를 Tracker에 적용하고, 이후 새로 발견된 방을 다시 런 상태에 병합한다.

따라서 M 전체 지도와 일반 미니맵 모두 이어하기 전의 탐험 상태를 이어서 표시할 수 있는 구조가 되었다.

### 6. 저장·복원 테스트 추가

39일차 전용 `DungeonMapPersistenceTests`를 추가했다.

검증 대상으로 구성한 항목은 다음과 같다.

- Seed 저장
- 레이아웃 Snapshot 저장
- 방 좌표·연결 저장
- 방문·발견 상태 저장
- `ApplyBasics` 후 동일한 그래프 복원
- 현재 방과 그리드 위치 복원
- pending RoomRunState 복원
- `SaveService.WriteRun → ReadRun` 실제 직렬화 왕복

기존 `DungeonMinimapRevealTrackerTests`에도 저장된 발견 RoomId를 복원하는 회귀 테스트를 추가했다.

---

## 변경 파일

### 수정

- `Assets/ProjectDelta/Scripts/Data/DungeonSaveMapper.cs`
- `Assets/ProjectDelta/Scripts/Data/RunData.cs`
- `Assets/ProjectDelta/Scripts/Domain/DungeonMinimapRevealTracker.cs`
- `Assets/ProjectDelta/Scripts/Domain/RunSubStates.cs`
- `Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/DungeonMinimapController.cs`
- `Assets/ProjectDelta/Tests/EditMode/DungeonMinimapRevealTrackerTests.cs`

### 생성

- `Assets/ProjectDelta/Tests/EditMode/DungeonMapPersistenceTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/DungeonMapPersistenceTests.cs.meta`

### 삭제

- 없음

---

## 동작 흐름

```text
방 진입 / 진행 상태 변경
        ↓
SaveDungeonProgress
        ↓
DungeonSaveMapper.BuildFromRunContext
        ↓
Seed + LayoutSnapshot + 방문/발견 정보 저장
        ↓
SaveService.WriteRun

이어하기
        ↓
SaveService.ReadRun
        ↓
DungeonSaveMapper.ApplyBasics
        ↓
DungeonLayoutSnapshot.Restore
        ↓
저장된 RoomView 구조 재배치
        ↓
방별 방문 상태 적용
        ↓
현재 RoomId / GridPosition 복원
        ↓
미니맵 발견 상태 복원
```

---

## 최신 커밋 확인

확인한 최신 커밋:

- SHA: `b2ed36592b46dcdd806500c53ca2ecf53e558f38`
- 현재 커밋 메시지: `a`
- 이전 38일차 커밋보다 1개 커밋 앞선 상태
- 39일차 작업 대상 9개 파일이 해당 커밋에 포함됨

GitHub에는 이 커밋에 연결된 CI 상태 또는 GitHub Actions 실행 기록이 없으므로 Unity 컴파일 및 EditMode Test Runner 통과 여부는 로컬 Unity에서 최종 확인해야 한다.

---

## 39일차 결과

지도 시스템이 단순 표시 기능에서 런 저장 데이터와 연결되는 단계까지 확장되었다.

이제 저장 데이터는 현재 던전의 Seed와 실제 레이아웃, 방 방문·발견 정보, 현재 위치를 함께 보존하며 이어하기 시 저장 당시 탐험 상태를 다시 구성할 수 있는 기반을 갖는다.
