# Project Delta - 34일차 개발일지

## 개발 주제

**절차적 던전 생성 — 인접 방 루프 연결, 출구 위치 정렬 및 중복 연결 방지**

33일차에서는 메인 경로를 유지한 상태에서 가지 경로를 생성하고, 가지 끝을 일반 막다른 방 후보 또는 특수 방 후보로 분류할 수 있도록 던전 생성 구조를 확장했다.

34일차에서는 이미 생성된 방들이 던전 격자에서 서로 인접한 경우 추가 연결을 만들 수 있도록 루프 생성 규칙을 추가하고, 모든 연결이 실제 `RoomExit` 좌표와 방향을 기준으로 검증되도록 연결 구조를 보강했다.

---

## 개발 목표

- 인접한 미연결 방 사이의 선택적 루프 생성
- `LoopChance` 설정 추가
- 실제 `RoomExit` 좌표·방향을 이용한 연결 정렬 검증
- 메인 경로와 가지 경로 생성에도 `RoomExit.CanConnectTo()` 적용
- 어떤 출구끼리 연결되었는지 `RoomConnectionEdge`에 보존
- 같은 방향 기존 연결 덮어쓰기 방지
- 자기 자신 연결 방지
- 비인접 방 연결 방지
- 중복 연결 방지
- 루프 생성 후 막다른 방·특수 방 후보 재검증
- 동일 Seed에서 루프까지 포함한 연결 구조 재현

---

## 구현 내용

### 1. LoopChance 설정 추가

`DungeonGenerationSettings`에 다음 설정을 추가했다.

```text
LoopChance
```

기본값은 다음과 같다.

```text
LoopChance = 0
```

기존 생성 코드의 동작을 바꾸지 않도록 기본값에서는 추가 루프가 생성되지 않는다.

루프를 활성화하려면 다음과 같이 직접 지정한다.

```csharp
new DungeonGenerationSettings(
    12,
    5,
    8,
    loopChance: 0.25d
)
```

`LoopChance`는 `0.0 ~ 1.0` 범위만 허용한다.

---

### 2. RoomConnectionEdge에 실제 출구 정보 저장

기존 `RoomConnectionEdge`는 다음 정보만 저장했다.

```text
Neighbor
IsLocked
```

34일차부터 다음 정보가 추가되었다.

```text
LocalExit
NeighborExit
HasExactExitPair
```

예:

```text
Room A
East (2, 0)

↕

Room B
West (-2, 0)
```

처럼 실제로 어떤 출구 쌍이 연결되었는지 그래프 자체가 보존한다.

이 정보는 이후 실제 RoomView 프리팹을 배치하고 문끼리 연결할 때 사용할 수 있다.

---

### 3. 안전한 TryConnect 추가

`DungeonLayoutGraph`에 연결 성공 여부를 반환하는 `TryConnect()`를 추가했다.

기본 방향 기반 연결과 실제 `RoomExit` 쌍 기반 연결을 각각 지원한다.

```text
TryConnect(RoomNode, Direction, RoomNode)

TryConnect(RoomNode, RoomExit, RoomNode, RoomExit)
```

연결 조건을 만족하지 못하면 기존 연결을 변경하지 않고 `false`를 반환한다.

---

### 4. 그래프 연결 안전 규칙 강화

연결 전에 다음 조건을 검증한다.

```text
from / to가 null이 아님
↓
자기 자신 연결이 아님
↓
from의 해당 방향이 비어 있음
↓
to의 반대 방향이 비어 있음
↓
두 방이 실제 MacroCoordinate상 인접함
↓
정확한 출구 쌍 사용 시 RoomExit.CanConnectTo() 통과
↓
연결
```

따라서 다음 연결을 차단한다.

- 자기 자신 연결
- 두 칸 이상 떨어진 방 연결
- 이미 사용 중인 방향 덮어쓰기
- 같은 연결의 중복 등록
- 반대 방향이 아닌 출구 연결
- 실제 문 위치 축이 어긋난 출구 연결

---

### 5. AddRoom의 RoomId 중복 검사 추가

기존에는 동일 MacroCoordinate만 차단했다.

34일차에서는 동일한 `RoomId`가 이미 등록되어 있는 경우도 예외를 발생시키도록 보강했다.

```text
RoomId 중복
→ 등록 거부

MacroCoordinate 중복
→ 등록 거부
```

그래프 내부 식별자 무결성을 함께 보호한다.

---

### 6. 기존 Connect API 호환 유지

이전 일차에서 사용하던:

```csharp
Connect(from, direction, to)
```

API는 삭제하지 않았다.

내부적으로 안전한 `TryConnect()`를 사용하도록 변경하여, 잘못된 연결이 기존 Edge를 조용히 덮어쓰는 대신 예외로 문제를 드러내도록 했다.

정확한 출구 쌍을 전달하는 새 연결 API도 추가했다.

```csharp
Connect(from, fromExit, to, toExit)
```

---

## 7. 메인 경로 생성에 출구 정렬 검사 적용

32일차 메인 경로 생성은 기존까지 필요한 반대 방향 출구의 존재를 중심으로 후보를 선택했다.

34일차에서는 각 후보 `RoomExit`에 대해:

```csharp
outgoingExit.CanConnectTo(entranceExit)
```

를 확인한다.

따라서 방향만 맞고 실제 문 위치 축이 다른 방은 메인 경로 후보에서 제외된다.

---

## 8. 가지 경로 생성에도 출구 정렬 검사 적용

33일차 가지 생성에도 동일한 규칙을 적용했다.

```text
가지 시작 RoomExit
↓
다음 방 반대 방향 출구 수집
↓
CanConnectTo 검사
↓
정렬 가능한 출구만 사용
```

메인 경로와 가지 경로가 서로 다른 연결 규칙을 사용하지 않도록 통일했다.

---

## 9. PlannedRoom에 정확한 출구 쌍 보존

경로를 그래프에 확정하기 전 사용하는 `PlannedRoom`도 다음 정보를 보존하도록 변경했다.

```text
EntranceExit
ExitFromPreviousRoom
```

따라서 임시 경로 계획 단계에서 선택한 정확한 출구 쌍을 실제 `DungeonLayoutGraph` 연결 단계까지 그대로 전달한다.

---

## 10. 인접 방 루프 생성

메인 경로와 가지 생성이 끝난 뒤 `GenerateLoops()`를 실행한다.

전체 방을 순회하면서 던전 격자에서 실제로 붙어 있는 방을 찾는다.

중복 검사를 피하기 위해 각 방에서 다음 두 방향만 검사한다.

```text
North
East
```

이렇게 하면:

```text
A → B

B → A
```

형태로 같은 인접 관계를 두 번 처리하지 않는다.

---

## 11. 루프 연결 조건

인접 방을 찾았다고 바로 연결하지 않는다.

다음 순서로 검사한다.

```text
인접 좌표에 방 존재
↓
현재 방 해당 방향 미사용
↓
이웃 방 반대 방향 미사용
↓
양쪽 RoomTemplate 확인
↓
실제 RoomExit 쌍 검색
↓
RoomExit.CanConnectTo() 통과
↓
LoopChance 판정
↓
TryConnect()
```

모든 조건을 통과한 경우에만 추가 루프가 생성된다.

---

## 12. 루프 생성의 Seed 재현성

루프 검사 전에 방 목록을 `RoomId` 기준으로 정렬한다.

```text
RoomId 정렬
↓
고정된 순서로 인접 관계 검사
↓
Seed 기반 Random 사용
```

따라서 동일한:

```text
Seed
GenerationSettings
RoomTemplate Pool
```

을 사용하면 루프 연결까지 동일한 구조를 재현할 수 있다.

---

## 13. 막다른 방·특수 방 후보 재검증

33일차에서 가지 끝은:

```text
DeadEndCandidate
SpecialCandidate
```

중 하나로 분류된다.

하지만 루프 연결이 생기면 기존 막다른 방이 더 이상 막다른 구조가 아닐 수 있다.

예:

```text
Branch ─ Candidate

        ↓ Loop 추가

Branch ─ Candidate
            │
            └─ OtherRoom
```

따라서 루프 생성이 완료된 뒤:

```text
Connections.Count == 1
```

조건을 다시 확인한다.

연결 수가 1이 아니면 `DeadEndCandidates` 또는 `SpecialRoomCandidates` 목록에서 제거한다.

---

## EditMode 테스트

`LoopConnectionTests.cs`에 **8개의 테스트**를 추가했다.

### 1. LoopChance 범위 검증

`LoopChance`가 `0 ~ 1` 범위를 벗어나면 예외가 발생하는지 확인한다.

### 2. 정확한 RoomExit 쌍 보존 검증

두 방을 실제 출구 쌍으로 연결했을 때 양쪽 `RoomConnectionEdge`에:

```text
LocalExit
NeighborExit
```

이 올바르게 저장되는지 확인한다.

### 3. 어긋난 출구 연결 방지

East/West처럼 방향은 반대이지만 정렬 축 좌표가 다른 경우 연결이 거부되는지 확인한다.

### 4. 중복 연결 보호

같은 방향과 같은 방 사이의 연결을 두 번 시도했을 때 두 번째 연결이 거부되고 기존 Edge가 유지되는지 확인한다.

### 5. 비인접 방 연결 방지

던전 MacroCoordinate에서 서로 붙어 있지 않은 방을 연결할 수 없는지 확인한다.

### 6. LoopChance 0 검증

루프 확률이 0일 때 전체 그래프의 무방향 Edge 수가:

```text
방 수 - 1
```

인 트리 구조를 유지하는지 확인한다.

### 7. 생성된 모든 연결의 실제 출구 검증

설정 기반 생성 결과의 모든 Edge가 정확한 `RoomExit` 쌍을 보유하고, 저장된 출구들이 `CanConnectTo()` 조건을 만족하는지 확인한다.

### 8. Seed 기반 루프 재현 검증

동일한 Seed와 설정으로 두 번 생성했을 때 루프를 포함한 전체 연결 집합이 동일한지 확인한다.

---

## 생성 파일

```text
Assets/ProjectDelta/Tests/EditMode/LoopConnectionTests.cs
Assets/ProjectDelta/Tests/EditMode/LoopConnectionTests.cs.meta
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Domain/DungeonGenerationSettings.cs
Assets/ProjectDelta/Scripts/Domain/DungeonGenerator.cs
Assets/ProjectDelta/Scripts/Domain/DungeonLayoutGraph.cs
```

---

## 삭제 파일

없음.

---

## 현재 남은 한계

### 1. LoopChance 기본값은 0

기존 생성 결과와 테스트에 영향을 주지 않기 위해 기본값에서는 추가 루프를 생성하지 않는다.

실제 던전 생성 설정에서 원하는 확률을 지정해야 루프가 활성화된다.

### 2. 루프는 새 방을 만들지 않음

34일차의 루프 생성은 이미 존재하는 두 방 사이에 추가 Edge만 만든다.

따라서 `TargetRoomCount` 자체에는 영향을 주지 않는다.

### 3. 루프 생성 실패에 대한 재시도 없음

해당 인접 관계의 출구 위치가 맞지 않거나 이미 방향이 사용 중이면 그 연결은 단순히 생략한다.

전체 던전 생성 실패·재시도·Seed 기록은 다음 안정화 단계에서 처리한다.

### 4. 실제 RoomView 연결은 아직 없음

그래프에는 정확한 `RoomExit` 쌍이 저장되지만 실제 프리팹 인스턴스는 아직 생성하지 않는다.

실제 RoomView 배치와 문 연결은 이후 던전 배치 단계에서 진행한다.

---

## 34일차 완료 판단

**34일차 목표인 인접 방 루프 연결, 출구 위치 일치 검증, 좌표·중복 연결 방지 규칙 구현은 완료되었다.**

30일차부터 보존해온 `RoomExit` 좌표 정보가 이제 실제 메인 경로, 가지 경로, 루프 연결 전체에서 사용된다.

또한 `RoomConnectionEdge`가 실제 연결에 사용된 출구 쌍을 보존하게 되어 이후 논리 던전 그래프를 실제 RoomView 프리팹과 연결할 수 있는 기반도 마련됐다.

GitHub 저장소에는 현재 해당 커밋의 CI 상태가 등록되어 있지 않으므로 Unity Test Runner의 실제 통과 여부는 로컬 Unity Editor에서 별도로 확인해야 한다.

---

## 다음 개발 방향

### 35일차

**방 수·거리·연결 수 제약, 생성 실패·재시도·Seed 기록/재현, 레이아웃 저장·복원 및 대량 자동 생성 검증**

34일차까지 던전의 기본 그래프 생성 규칙은 갖춰졌다.

35일차에서는 생성기를 반복 실행해도 안정적으로 유효한 던전을 만들 수 있도록 생성 전체를 검증하고 실패를 관리한다.

주요 작업 방향:

```text
던전 생성 요청
↓
Seed 기록
↓
생성
↓
전체 제약 검증
├─ 방 수
├─ 메인 경로 거리
├─ 연결 무결성
└─ 좌표 중복
↓
실패
→ 재시도

성공
→ 최종 Seed / Layout 보존
↓
동일 Seed 재생성 검증
↓
대량 자동 생성 테스트
```

이후 36일차에서 완성된 논리 `GeneratedDungeon`을 실제 RoomView 프리팹 배치와 연결한다.
