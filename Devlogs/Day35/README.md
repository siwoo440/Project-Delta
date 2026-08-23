# Project Delta - 35일차 개발일지

## 개발 주제

**절차적 던전 생성 안정화 — 전체 제약 검증, 생성 재시도, Seed 기록·재현, 레이아웃 저장·복원 및 대량 자동 검증**

34일차까지 메인 경로, 가지 경로, 루프 연결과 정확한 `RoomExit` 쌍을 보존하는 논리 던전 그래프를 완성했다.

35일차에서는 기존 생성 알고리즘을 직접 확장하기보다, 완성된 `GeneratedDungeon`을 다시 검증하고 실패 시 다른 Seed로 재시도하며, 성공한 결과를 저장 가능한 Snapshot으로 변환하고 다시 복원할 수 있도록 안정화 계층을 추가했다.

---

## 개발 목표

- 생성 완료 던전의 전체 제약 검증
- 전체 방 수 불일치 검출
- 메인 경로 길이 및 Entry/Stairs 끝점 검증
- 실제 Entry→Stairs 최단 거리 검증
- 좌표 중복 및 연결 수 검증
- 자기 연결 및 비인접 연결 검출
- 양방향 Edge 무결성 검사
- 정확한 `RoomExit` 쌍 저장 여부 및 호환성 검사
- Entry에서 모든 방의 도달 가능성 검사
- 생성 실패 시 Seed를 변경하며 자동 재시도
- 요청 Seed·성공 Seed·시도 횟수·실패 원인 기록
- 생성 결과를 `DungeonLayoutSnapshot`으로 저장
- Snapshot에서 동일한 논리 던전 복원
- 동일 Seed의 전체 레이아웃 재현 검증
- 요청 Seed 0~9999 대상 10,000회 자동 생성 검증

---

## 구현 내용

### 1. DungeonGenerationValidation 추가

생성된 던전이 실제 사용 가능한 상태인지 최종 검사하는 검증 계층을 추가했다.

주요 구성:

```text
DungeonValidationCode
DungeonValidationIssue
DungeonValidationResult
DungeonGenerationValidator
```

생성기 내부에서 이미 방지하는 조건이라도 최종 결과를 다시 검사하여 생성 완료 이후의 마지막 안전망으로 사용한다.

---

## 2. 검증 실패 종류 정의

`DungeonValidationCode`를 통해 실패 원인을 코드 단위로 구분한다.

```text
GeneratorReportedFailure
RoomCountMismatch
MainPathIncomplete
MainPathLengthOutOfRange
MainPathEndpointMismatch
EntryOrStairsMissing
EntryToStairsDistanceOutOfRange
DuplicateCoordinate
TooManyConnections
SelfConnection
NonAdjacentConnection
MissingReciprocalConnection
MissingExactExitPair
InvalidExitPair
DisconnectedRoom
```

따라서 단순히 생성 실패 여부만 확인하는 것이 아니라 어떤 제약이 실패했는지 기록할 수 있다.

---

## 3. 전체 방 수 검증

설정의:

```text
TargetRoomCount
```

와 실제:

```text
DungeonLayoutGraph.AllRooms.Count
```

를 비교한다.

두 값이 다르면:

```text
RoomCountMismatch
```

를 반환한다.

33일차에서 `RoomCountTargetReached`를 제공했지만, 35일차부터는 최종 검증기가 다시 독립적으로 확인한다.

---

## 4. 메인 경로 검증

다음 항목을 검사한다.

```text
MainPathCompleted
MainPath.Count
MinMainPathLength
MaxMainPathLength
MainPath[0] == EntryRoom
MainPath[last] == StairsRoom
```

메인 경로 생성 자체는 성공했더라도 최종 데이터가 잘못 변경된 경우를 검출할 수 있다.

---

## 5. Entry→Stairs 실제 최단 거리 검증

메인 경로에 저장된 순서뿐 아니라 실제 완성된 그래프를 BFS로 탐색한다.

```text
EntryRoom
↓
전체 연결 탐색
↓
StairsRoom
↓
최단 Edge 수 계산
```

루프가 존재하면 메인 경로보다 더 짧은 우회로가 생길 수 있기 때문에 실제 그래프 기준 거리를 다시 검사한다.

메인 경로 방 수 기준 설정을 Edge 거리로 변환하여 비교한다.

```text
최소 거리 = MinMainPathLength - 1
최대 거리 = MaxMainPathLength - 1
```

---

## 6. MacroCoordinate 중복 검사

전체 `RoomNode`의:

```text
MacroCoordinate
```

를 `HashSet<GridPosition>`으로 검사한다.

동일 좌표가 두 번 발견되면:

```text
DuplicateCoordinate
```

를 기록한다.

---

## 7. 방 연결 수 검사

현재 던전 구조는:

```text
North
East
South
West
```

네 방향을 사용하므로 방 하나의 연결 수가 4를 초과하면 잘못된 그래프로 판단한다.

```text
Connections.Count > 4
→ TooManyConnections
```

---

## 8. Edge 무결성 검사

모든 연결을 순회하며 다음을 확인한다.

```text
자기 자신 연결 여부
인접 MacroCoordinate 여부
반대편 Edge 존재 여부
반대편 Edge가 다시 현재 방을 가리키는지
```

문제가 있으면 각각:

```text
SelfConnection
NonAdjacentConnection
MissingReciprocalConnection
```

으로 기록한다.

---

## 9. RoomExit 쌍 검증

34일차부터 모든 설정 기반 연결에는 정확한 출구 쌍이 저장된다.

35일차 검증기에서는:

```text
LocalExit
NeighborExit
HasExactExitPair
```

를 확인한다.

그리고:

```csharp
LocalExit.CanConnectTo(NeighborExit)
```

를 다시 검사한다.

잘못된 경우:

```text
MissingExactExitPair
InvalidExitPair
```

로 기록한다.

---

## 10. 전체 방 도달 가능성 검사

EntryRoom에서 BFS를 실행해 모든 방을 방문한다.

```text
EntryRoom
↓
Connections
↓
전체 RoomNode 방문
```

그래프에는 존재하지만 Entry에서 갈 수 없는 방이 있으면:

```text
DisconnectedRoom
```

으로 기록한다.

---

## 11. DungeonGenerationService 추가

생성·검증·재시도를 하나의 상위 서비스로 묶었다.

주요 구성:

```text
DungeonGenerationAttemptLog
DungeonGenerationRunResult
DungeonGenerationService
```

사용 흐름:

```text
Requested Seed
↓
DungeonGenerator
↓
DungeonGenerationValidator
↓
Valid?
├─ Yes → 성공
└─ No  → 다음 Seed
```

---

## 12. Seed 기반 자동 재시도

`GenerateWithRetry()`는 최초 Seed부터 순차적으로 증가시키며 다시 생성한다.

예:

```text
RequestedSeed = 100

Attempt 1 → Seed 100
Attempt 2 → Seed 101
Attempt 3 → Seed 102
```

기본 최대 시도 횟수는:

```text
10
```

이다.

유효한 던전이 만들어지면 즉시 종료한다.

---

## 13. 생성 시도 로그

각 시도마다 다음 정보를 저장한다.

```text
AttemptNumber
Seed
GeneratedRoomCount
ValidationIssues
IsValid
```

따라서 특정 Seed에서 실패한 원인을 그대로 추적할 수 있다.

---

## 14. 최종 생성 결과

`DungeonGenerationRunResult`는 다음 정보를 제공한다.

```text
Success
Dungeon
RequestedSeed
SuccessfulSeed
AttemptCount
Attempts
Validation
```

성공한 경우 실제 사용해야 할 Seed를 `SuccessfulSeed`로 확인할 수 있다.

최대 횟수까지 모두 실패한 경우 마지막으로 사용한 Seed가 기록된다.

---

## 15. 생성 중 예외 처리

개별 Seed 생성 도중 예상하지 못한 예외가 발생해도 전체 재시도 또는 Stress Test가 즉시 중단되지 않도록 처리했다.

예외 정보는:

```text
GeneratorReportedFailure
```

검증 문제로 변환하고 다음 Seed를 시도한다.

로그에는 예외 타입과 메시지가 함께 남는다.

---

## 16. DungeonLayoutSnapshot 추가

논리 던전 전체를 저장 가능한 순수 데이터로 변환하는 구조를 추가했다.

구성:

```text
DungeonRoomSnapshot
DungeonConnectionSnapshot
DungeonLayoutSnapshot
```

현재 실제 파일 저장 포맷까지 구현한 것은 아니며, 이후 JSON이나 다른 Save 시스템에 전달할 수 있는 데이터 계층이다.

---

## 17. 방 Snapshot 저장 항목

방마다 다음 정보를 저장한다.

```text
RoomId
DefinitionId
MacroX
MacroZ
DungeonRoomRole
```

따라서 복원 시 동일한 방 ID, 원본 정의, 던전 좌표와 생성 역할을 다시 구성할 수 있다.

---

## 18. 연결 Snapshot 저장 항목

각 연결은 중복 없이 한 번만 저장한다.

```text
FromRoomId
ToRoomId
IsLocked

FromExitX
FromExitZ
FromExitDirection

ToExitX
ToExitZ
ToExitDirection
```

34일차에서 추가한 실제 `RoomExit` 연결 정보가 그대로 저장된다.

---

## 19. 던전 메타데이터 저장

Snapshot에는 다음 정보도 함께 저장한다.

```text
Seed
TargetMainPathLength
TargetRoomCount
EntryRoomId
StairsRoomId
FailureReason

MainPathRoomIds
BranchRoomIds
DeadEndCandidateRoomIds
SpecialCandidateRoomIds
```

특히 `MainPathRoomIds`는 순서를 유지한다.

---

## 20. Snapshot Capture

다음 방식으로 생성 결과를 저장 데이터로 변환한다.

```csharp
DungeonLayoutSnapshot.Capture(
    generatedDungeon,
    successfulSeed
)
```

방 목록과 연결 목록은 재현성과 비교가 쉽도록 일정한 순서로 정리한다.

---

## 21. Snapshot Restore

Snapshot에서 새로운 `DungeonLayoutGraph`를 만든다.

복원 순서:

```text
RoomSnapshot
↓
RoomNode 복원
↓
ConnectionSnapshot
↓
RoomExit 쌍 복원
↓
DungeonLayoutGraph.Connect
↓
Entry/Stairs 복원
↓
MainPath/Branch/후보 목록 복원
↓
GeneratedDungeon 생성
```

복원된 그래프 역시 `DungeonGenerationValidator`로 다시 검사할 수 있다.

---

## 22. 현재 저장 범위

35일차 Snapshot은 현재 `GeneratedDungeon`이 가지고 있는 논리 상태를 저장한다.

```text
방 배치
방 정의
연결
출구
생성 역할
Entry
Stairs
MainPath
Seed
```

아직 존재하지 않는 실제 플레이 상태:

```text
적 처치 여부
방 클리어 여부
드랍 아이템
열린 상자
플레이어가 방문했는지
```

등은 저장하지 않는다.

이 정보는 실제 런타임 던전 상태 시스템이 만들어진 뒤 추가할 수 있도록 분리했다.

---

## EditMode 테스트

`DungeonGenerationStabilityTests.cs`에 **7개의 테스트**를 추가했다.

### 1. 정상 생성 결과 전체 검증

정상 생성된 던전이 모든 Validator 제약을 통과하는지 확인한다.

### 2. 방 수 불일치 검출

분기를 비활성화하여 목표 방 수에 도달하지 못하게 만든 뒤 `RoomCountMismatch`가 검출되는지 확인한다.

### 3. 실패 재시도 Seed 기록

의도적으로 모든 시도가 실패하도록 설정하고:

```text
Seed 100
Seed 101
Seed 102
```

순서로 정확히 기록되는지 확인한다.

### 4. 성공 Seed 기록

재시도 도중 성공한 경우:

```text
RequestedSeed
SuccessfulSeed
AttemptCount
Validation
```

이 올바르게 기록되는지 확인한다.

### 5. Snapshot 저장·복원

생성 결과를 Capture 후 Restore하여 다음이 유지되는지 확인한다.

```text
전체 방 수
EntryRoom
StairsRoom
MainPath
Connections
RoomRole
```

복원 결과도 Validator를 다시 통과해야 한다.

### 6. 동일 Seed 전체 Snapshot 재현

동일 Seed로 생성한 두 던전의 Snapshot 전체 서명을 비교한다.

검사 항목:

```text
Seed
Entry
Stairs
목표 값
MainPath
Branch
후보 목록
Room 데이터
Connection 데이터
RoomExit 쌍
```

### 7. 10,000 Seed Stress Test

테스트 카테고리:

```text
Stress
```

로 분리했다.

다음 Requested Seed를 모두 실행한다.

```text
0 ~ 9999
```

각 Requested Seed마다:

```text
최대 10회 생성
↓
Validator 검사
↓
유효한 던전 확보
```

를 확인한다.

실패가 발생하면:

```text
RequestedSeed
마지막 Seed
시도 횟수
각 시도의 방 수
ValidationIssue
```

가 테스트 실패 메시지에 출력된다.

---

## Stress Test 설정

대량 검증에서는 복잡성보다는 생성기 자체의 안정성 검증에 집중하도록 다음 설정을 사용한다.

```text
TargetRoomCount = 8
MinMainPathLength = 6
MaxMainPathLength = 6

BranchChance = 1
MinBranchLength = 1
MaxBranchLength = 1

SpecialCandidateChance = 0.30
LoopChance = 0
```

루프까지 포함한 Seed 재현 검증은 34일차 테스트에서 별도로 다루고 있으므로, 35일차 10,000회 Stress Test는 기본 생성·검증·재시도 안정성에 집중한다.

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/DungeonGenerationValidation.cs
Assets/ProjectDelta/Scripts/Domain/DungeonGenerationValidation.cs.meta

Assets/ProjectDelta/Scripts/Domain/DungeonGenerationService.cs
Assets/ProjectDelta/Scripts/Domain/DungeonGenerationService.cs.meta

Assets/ProjectDelta/Scripts/Domain/DungeonLayoutSnapshot.cs
Assets/ProjectDelta/Scripts/Domain/DungeonLayoutSnapshot.cs.meta

Assets/ProjectDelta/Tests/EditMode/DungeonGenerationStabilityTests.cs
Assets/ProjectDelta/Tests/EditMode/DungeonGenerationStabilityTests.cs.meta
```

---

## 자동 변경 파일

```text
Project-Delta.slnx
```

35일차 커밋에서는 `ProjectDelta.Domain.csproj` 항목의 순서가 변경되었다.

프로젝트 추가·삭제가 아니라 기존 항목의 위치만 바뀐 것으로 기능상 영향은 없다.

---

## 수정·삭제 파일

기존 34일차 Domain 생성 로직 파일의 직접 수정은 없다.

삭제 파일도 없다.

---

## 현재 남은 한계

### 1. 실제 저장 파일 입출력 없음

`DungeonLayoutSnapshot`은 저장 가능한 순수 데이터 구조다.

실제 JSON 파일 작성이나 세이브 슬롯 시스템은 아직 구현하지 않는다.

### 2. 플레이 중 방 상태 저장 없음

현재 Snapshot은 논리 레이아웃과 생성 메타데이터까지만 저장한다.

실제 방 클리어 상태와 적·아이템 상태는 이후 런타임 시스템과 함께 추가해야 한다.

### 3. 재시도 Seed 규칙은 단순 증가 방식

현재는:

```text
RequestedSeed + AttemptIndex
```

방식으로 재시도한다.

복잡한 Seed 파생 규칙은 필요해질 때 확장할 수 있다.

### 4. 최대 시도 횟수 초과 시 최종 실패 가능

기본 10회 안에 유효한 던전이 만들어지지 않으면 `Success = false`로 반환한다.

이를 상위 게임 흐름에서 어떻게 처리할지는 실제 Floor 생성 Controller와 연결할 때 결정한다.

### 5. 실제 RoomView 배치 없음

35일차까지는 여전히 논리 던전 계층이다.

실제 GameObject와 RoomView 프리팹 생성은 다음 일차에서 진행한다.

---

## 35일차 완료 판단

**35일차 목표인 던전 생성 전체 제약 검증, 생성 실패 재시도, Seed 기록·재현, 논리 레이아웃 Snapshot 저장·복원 및 10,000 Seed 자동 검증 기반 구현은 완료되었다.**

29~34일차에서 구축한 절차적 생성기의 결과를 별도의 Validator가 검증하고, 실패 Seed를 기록하며 다시 생성할 수 있는 상위 서비스가 추가됐다.

또한 완성된 논리 던전을 `DungeonLayoutSnapshot`으로 변환하고 다시 `GeneratedDungeon`으로 복원할 수 있어, 이후 실제 세이브 시스템과 RoomView 배치 시스템이 사용할 수 있는 기반도 갖춰졌다.

GitHub 저장소에는 현재 해당 커밋의 CI 상태가 등록되어 있지 않으므로 Unity Test Runner에서 실제 테스트 통과 여부는 로컬 Editor에서 별도로 확인해야 한다.

---

## 다음 개발 방향

### 36일차

**GeneratedDungeon을 DungeonFloorController와 연결해 RoomView 프리팹 실제 배치·문 연결·계단 배치 구현**

35일차까지의 결과:

```text
DungeonGenerationService
↓
Validated GeneratedDungeon
↓
DungeonLayoutSnapshot
```

을 실제 Unity 씬으로 변환한다.

다음 일차 주요 작업:

```text
DungeonFloorController
↓
GeneratedDungeon 생성 또는 복원
↓
RoomNode별 RoomDefinition / Prefab 조회
↓
MacroCoordinate를 World Position으로 변환
↓
RoomView Instantiate
↓
RoomId와 RoomView 연결
↓
RoomConnectionEdge의 LocalExit / NeighborExit 확인
↓
실제 문 연결 상태 적용
↓
StairsRoom에 계단 배치
```

36일차가 끝나면 지금까지 데이터로만 존재하던 절차적 던전이 실제 Unity 씬에 배치되기 시작한다.
