# Project Delta - 33일차 개발일지

## 개발 주제

**절차적 던전 생성 — 가지 경로 생성, 막다른 방·특수 방 후보 지정 및 분기 확률 구현**

32일차에서는 시작 방부터 계단 방까지 이어지는 메인 경로를 최소·최대 길이 규칙에 맞춰 먼저 생성할 수 있도록 만들었다.

33일차에서는 이 메인 경로를 유지한 상태에서 남은 목표 방 수를 가지 경로로 채우고, 가지 끝 방을 일반 막다른 방 후보 또는 특수 방 후보로 분류할 수 있도록 던전 생성 구조를 확장했다.

---

## 개발 목표

- `TargetRoomCount`를 실제 전체 던전 방 수 목표로 사용
- 메인 경로의 미사용 출구에서 가지 생성
- `BranchChance`를 이용한 분기 확률 적용
- 가지 최소·최대 길이 설정
- 메인 경로와 기존 가지 방의 좌표 충돌 방지
- 메인 경로 및 계단 방 유지
- 가지 끝 방을 `DeadEndCandidate` 또는 `SpecialCandidate`로 분류
- 방 역할을 생성 메타데이터로 관리
- 동일 Seed에서 동일한 가지 구조와 역할 재현
- 기존 생성 API와 이전 일차 생성 결과 생성자 호환 유지

---

## 구현 내용

### 1. DungeonGenerationSettings 확장

기존 설정:

```text
TargetRoomCount
MinMainPathLength
MaxMainPathLength
```

에 다음 값을 추가했다.

```text
BranchChance
MinBranchLength
MaxBranchLength
SpecialCandidateChance
```

기본값은 다음과 같다.

```text
BranchChance = 0.65
MinBranchLength = 1
MaxBranchLength = 3
SpecialCandidateChance = 0.30
```

기존처럼 다음 생성자를 사용하던 코드는 그대로 사용할 수 있다.

```csharp
new DungeonGenerationSettings(12, 5, 8)
```

추가 설정을 지정하고 싶을 때만 선택적으로 값을 전달한다.

---

### 2. 가지 생성 설정 검증

새 설정값에 다음 검증 규칙을 추가했다.

```text
BranchChance
0.0 ~ 1.0

MinBranchLength
1 이상

MaxBranchLength
MinBranchLength 이상

SpecialCandidateChance
0.0 ~ 1.0
```

잘못된 확률이나 길이 범위가 생성기에 들어가지 않도록 설정 생성 시점에서 차단한다.

---

### 3. DungeonRoomRole 추가

방 자체의 콘텐츠 타입과 던전 생성 단계의 역할을 분리하기 위해 새로운 생성 역할 열거형을 추가했다.

```text
DungeonRoomRole
├─ MainPath
├─ Branch
├─ DeadEndCandidate
└─ SpecialCandidate
```

각 역할의 의미는 다음과 같다.

### MainPath

시작 방부터 계단 방까지 이어지는 필수 진행 경로다.

### Branch

메인 경로 밖에 추가된 일반 가지 경로 방이다.

### DeadEndCandidate

가지 경로 마지막에 위치한 일반 막다른 방 후보다.

### SpecialCandidate

상점, 휴식, 이벤트 등의 특수 방으로 사용할 수 있도록 표시된 가지 끝 후보다.

현재 단계에서는 실제 상점·보물·이벤트 타입을 확정하지 않고 생성 위치 후보만 구분한다.

---

## 4. GeneratedDungeon 결과 확장

33일차부터 생성 결과가 다음 정보를 추가로 제공한다.

```text
BranchRooms
DeadEndCandidates
SpecialRoomCandidates
RoomRoles
TargetRoomCount
RoomCountTargetReached
```

기존 메인 경로 관련 정보도 그대로 유지된다.

```text
MainPath
TargetMainPathLength
MainPathCompleted
EntryRoom
StairsRoom
FailureReason
```

이를 통해 이후 시스템에서 방 하나를 조회했을 때 생성 단계에서 어떤 역할을 가진 방인지 확인할 수 있다.

---

## 5. 방 역할 조회 기능

`GeneratedDungeon.TryGetRoomRole()`을 이용해 방의 생성 역할을 조회할 수 있도록 했다.

예:

```text
RoomNode
↓
TryGetRoomRole
↓
MainPath / Branch / DeadEndCandidate / SpecialCandidate
```

`RoomNode` 자체에는 생성 역할 필드를 추가하지 않았다.

따라서 기존 던전 그래프 구조와 생성 메타데이터가 분리되어 유지된다.

---

## 6. 메인 경로 우선 생성 유지

32일차의 메인 경로 생성 방식은 그대로 유지한다.

```text
Seed
↓
목표 MainPath 길이 선택
↓
Backtracking 기반 MainPath 계획
↓
MainPath 생성 성공
↓
실제 DungeonLayoutGraph 확정
```

메인 경로 생성 자체가 실패하면 가지 생성을 진행하지 않고 기존처럼 실패 결과를 반환한다.

---

## 7. TargetRoomCount 기반 남은 방 계산

메인 경로가 완성되면 다음 계산을 사용한다.

```text
남은 방 수
=
TargetRoomCount
-
현재 생성된 방 수
```

예:

```text
TargetRoomCount = 12
MainPath = 7

남은 방 = 5
```

33일차에서는 이 남은 방을 가지 경로로 채운다.

---

## 8. 가지 시작점 수집

계단 방을 제외한 메인 경로 방에서 아직 그래프 연결에 사용되지 않은 출구를 찾는다.

```text
MainPath 방
↓
RoomTemplate.Exits 확인
↓
해당 방향에 이미 Connection이 있는가?
├─ Yes → 사용 중
└─ No  → Branch 시작 후보
```

계단 방은 다음 층으로 이동하는 최종 진행 위치이므로 가지 시작 후보에서 제외했다.

---

## 9. 분기 확률 적용

각 가지 시작 후보마다 `BranchChance`를 적용한다.

```text
미사용 출구
↓
Seed 기반 Random
↓
BranchChance 판정
├─ 성공 → 가지 생성 시도
└─ 실패 → 다음 출구
```

`BranchChance = 0`이면 가지가 생성되지 않는다.

`BranchChance = 1`이면 가능한 모든 후보에서 가지 생성을 시도한다.

---

## 10. 가지 길이 결정

가지 하나의 길이는 다음 범위에서 결정한다.

```text
MinBranchLength
~
MaxBranchLength
```

단, 남아 있는 전체 방 수를 초과할 수 없다.

예:

```text
남은 방 = 2
MaxBranchLength = 3

실제 사용할 수 있는 최대 길이 = 2
```

따라서 가지 생성 때문에 `TargetRoomCount`를 초과하지 않는다.

---

## 11. 가지 경로 계획

가지는 실제 그래프에 바로 추가하지 않고 먼저 임시로 계획한다.

```text
MainPath의 미사용 출구
↓
다음 MacroCoordinate 계산
↓
기존 방 좌표와 충돌 검사
↓
연결 가능한 RoomTemplate 선택
↓
가지 방 임시 추가
↓
다음 출구 탐색
↓
목표 길이 완성
↓
실제 그래프에 확정
```

메인 경로 생성과 마찬가지로 실패한 임시 경로는 실제 그래프에 남지 않는다.

---

## 12. 전체 좌표 충돌 방지

새 가지를 계획할 때 현재 그래프에 존재하는 모든 방의 `MacroCoordinate`를 수집한다.

검사 대상에는 다음이 모두 포함된다.

```text
MainPath
+
이미 확정된 Branch
```

새 방의 후보 좌표가 이미 사용 중이면 해당 후보를 사용하지 않는다.

따라서 33일차 생성 단계에서도 한 좌표에 여러 방이 겹치지 않는다.

---

## 13. 가지 경로 실제 그래프 등록

가지 계획이 목표 길이까지 성공하면 해당 방들을 `DungeonLayoutGraph`에 순서대로 추가한다.

```text
MainPath
   │
   └─ Branch
        │
        └─ Branch
             │
             └─ BranchEnd
```

각 가지 방은 앞 방과 실제 `RoomConnectionEdge`로 연결된다.

따라서 시작 방에서 모든 가지 방까지 실제 그래프를 따라 도달할 수 있다.

---

## 14. 가지 끝 방 후보 분류

가지 하나가 완성되면 마지막 방만 후보 역할을 지정한다.

```text
Branch End
↓
SpecialCandidateChance 판정
├─ 성공 → SpecialCandidate
└─ 실패 → DeadEndCandidate
```

두 후보 목록에 동시에 들어가지 않도록 분리했다.

---

## 15. 메인 경로와 계단 방 유지

가지 경로 생성 후에도 다음 규칙은 변하지 않는다.

```text
MainPath[0]
= EntryRoom

MainPath[last]
= StairsRoom
```

가지 생성으로 인해 계단 방이 다른 막다른 방으로 다시 선정되지 않는다.

따라서 32일차에서 보장한 필수 진행 경로가 그대로 유지된다.

---

## EditMode 테스트

`BranchGenerationTests.cs`에 **8개의 테스트**를 추가했다.

### 1. BranchChance 0 검증

분기 확률이 0일 때 던전이 메인 경로만 유지하는지 확인한다.

### 2. BranchChance 1 및 TargetRoomCount 검증

공간과 출구가 충분한 상태에서 분기 확률이 1일 때 전체 목표 방 수까지 생성되는지 확인한다.

### 3. MainPath·Stairs 유지 검증

가지가 추가되어도 시작 방, 메인 경로, 계단 방이 바뀌지 않는지 확인한다.

### 4. 전체 MacroCoordinate 중복 검증

메인 경로와 가지를 포함한 전체 방이 서로 다른 좌표를 사용하는지 확인한다.

### 5. 전체 방 도달 가능성 검증

BFS 탐색을 통해 모든 가지 방이 시작 방에서 도달 가능한지 확인한다.

### 6. DeadEndCandidate 검증

특수 방 후보 확률을 0으로 설정했을 때 가지 끝 방이:

- 실제 연결 수 1인 막다른 방
- 메인 경로가 아닌 방

인지 확인한다.

### 7. SpecialCandidate 검증

특수 방 후보 확률을 1로 설정했을 때 가지 끝 방이 `SpecialCandidate` 역할로 지정되는지 확인한다.

### 8. Seed 재현성 검증

동일한 Seed와 동일한 설정으로 생성했을 때:

- 전체 방 수
- 방 좌표
- 생성 역할

이 동일하게 재현되는지 확인한다.

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/DungeonRoomRole.cs
Assets/ProjectDelta/Scripts/Domain/DungeonRoomRole.cs.meta
Assets/ProjectDelta/Tests/EditMode/BranchGenerationTests.cs
Assets/ProjectDelta/Tests/EditMode/BranchGenerationTests.cs.meta
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Domain/DungeonGenerationSettings.cs
Assets/ProjectDelta/Scripts/Domain/DungeonGenerator.cs
```

---

## 삭제 파일

없음.

---

## 현재 남은 한계

### 1. Branch에서 추가 Branch가 시작되지는 않음

33일차의 가지 시작 후보는 메인 경로의 미사용 출구를 기준으로 한다.

가지 방은 하나의 선형 가지 안에서 이어질 수 있지만, 가지 중간 방에서 다시 별도의 하위 가지가 갈라지는 구조는 아직 만들지 않는다.

### 2. 모든 Seed가 TargetRoomCount를 반드시 채우는 것은 아님

`BranchChance`가 낮거나 사용 가능한 출구·좌표가 부족하면 전체 목표 방 수보다 적게 생성될 수 있다.

현재는 `RoomCountTargetReached`를 통해 이를 확인할 수 있다.

생성 실패 재시도와 제약 보장은 이후 안정화 단계에서 보강한다.

### 3. 루프 연결 없음

서로 인접한 메인·가지 방이 이미 존재하더라도 별도의 연결을 추가하지 않는다.

현재 연결은 방을 새로 생성하면서 만들어진 트리 구조를 유지한다.

인접 방 루프 연결은 다음 일차에서 구현한다.

### 4. 실제 특수 방 콘텐츠 미지정

`SpecialCandidate`는 위치 후보일 뿐이다.

상점, 휴식, 이벤트, 보물 등의 실제 방 콘텐츠 타입 선정은 이후 콘텐츠 배치 시스템에서 진행한다.

### 5. 실제 RoomView 자동 배치 없음

현재 결과는 여전히 논리적인 `DungeonLayoutGraph`다.

프리팹 실제 배치 및 문 연결은 이후 던전 배치 단계에서 진행한다.

---

## 33일차 완료 판단

**33일차 목표인 메인 경로 기반 가지 생성, 막다른 방·특수 방 후보 지정, 분기 확률 적용은 구현되었다.**

32일차의 필수 진행 경로를 유지하면서 선택 탐험 경로를 추가할 수 있게 되었고, 전체 목표 방 수와 가지 길이를 고려해 던전 형태를 확장할 수 있는 기반이 마련됐다.

또한 각 방의 생성 역할을 별도 메타데이터로 관리하여 이후 실제 방 콘텐츠를 배치하기 위한 구조도 확보했다.

GitHub 저장소에는 현재 해당 커밋의 CI 상태가 등록되어 있지 않으므로 Unity Test Runner의 실제 통과 여부는 로컬 Unity Editor에서 별도로 확인해야 한다.

---

## 다음 개발 방향

### 34일차

**인접 방 루프 연결, 출구 위치 일치·좌표 충돌·중복 연결 방지 규칙 구현**

33일차까지의 던전 연결은 기본적으로 트리 구조다.

34일차에서는 이미 생성된 두 방이 던전 격자에서 서로 인접하고 양쪽에 호환되는 출구가 존재할 경우 추가 연결을 만들어 순환 경로를 생성한다.

```text
기존 구조

A ─ B
    │
    C ─ D

인접하지만 연결되지 않은 A와 D가 존재할 수 있음

        ↓

34일차

A ─ B
│   │
D ─ C
```

다음 일차에서는:

- 인접 RoomNode 탐색
- 양쪽 출구 방향 검사
- `RoomExit.CanConnectTo()`를 이용한 위치 정렬 확인
- 이미 연결된 방향 중복 방지
- 동일 방 중복 연결 방지
- 선택적 Loop 생성
- 루프 추가 후 전체 도달 가능성·연결 무결성 테스트

를 구현한다.
