# Project Delta - 32일차 개발일지

## 개발 주제

**절차적 던전 생성 — 시작 방부터 계단 방까지의 메인 경로 길이 제어**

31일차까지는 2·3·4출구 테스트 방과 문 정렬 규격을 준비했다.

32일차에서는 던전 생성 시 시작 방에서 계단 방까지 이어지는 **메인 경로를 먼저 계획하고**, 설정한 최소·최대 길이 범위 안에서 정확한 목표 길이를 생성할 수 있도록 `DungeonGenerator`를 확장했다.

기존 프론티어 기반 생성 방식은 이전 코드 호환을 위해 그대로 유지하고, `DungeonGenerationSettings`를 사용하는 새로운 생성 오버로드를 추가했다.

---

## 개발 목표

- 전체 목표 방 수와 메인 경로 최소·최대 길이 규칙 분리
- Seed를 이용해 이번 층의 메인 경로 목표 길이 결정
- 시작 방부터 계단 방까지 연속된 메인 경로 생성
- 메인 경로 MacroCoordinate 중복 방지
- 생성 중 막히면 다른 출구·방 조합으로 되돌아가 재탐색
- 성공한 경로만 `DungeonLayoutGraph`에 확정
- 메인 경로 마지막 방을 `StairsRoom`으로 지정
- 동일 Seed에서 동일한 메인 경로 재현
- 목표 길이를 만들 수 없을 때 명시적인 실패 결과 반환
- 기존 `Generate(..., targetRoomCount)` 방식 호환 유지

---

## 구현 내용

### 1. DungeonGenerationSettings 추가

던전 생성 규칙을 별도 클래스로 분리했다.

```text
DungeonGenerationSettings
├─ TargetRoomCount
├─ MinMainPathLength
└─ MaxMainPathLength
```

`TargetRoomCount`는 이후 가지 경로까지 포함한 전체 방 수를 위한 값이고, 32일차에서는 메인 경로 길이 범위를 검증하는 기준으로 사용한다.

생성 시 다음 잘못된 설정을 차단한다.

- 전체 목표 방 수가 1 미만
- 메인 경로 최소 길이가 1 미만
- 최대 길이가 최소 길이보다 작음
- 메인 경로 최대 길이가 전체 목표 방 수보다 큼

---

### 2. 설정 기반 Generate 오버로드 추가

기존 방식:

```csharp
Generate(entryTemplate, roomPool, targetRoomCount)
```

은 그대로 유지했다.

32일차부터 다음 방식도 사용할 수 있다.

```csharp
Generate(entryTemplate, roomPool, settings)
```

새 방식에서는 던전 전체를 무작위로 먼저 확장하지 않고, **시작 방 → 계단 방 메인 경로를 우선 생성**한다.

---

### 3. Seed 기반 목표 메인 경로 길이 결정

생성 시작 시 다음 범위에서 이번 층의 목표 길이를 선택한다.

```text
MinMainPathLength
        ↓
   Seed 기반 Random
        ↓
MaxMainPathLength
```

예를 들어 설정이 다음과 같다면:

```text
TargetRoomCount = 12
MinMainPathLength = 5
MaxMainPathLength = 8
```

이번 Seed에 따라 시작 방과 계단 방을 포함한 5~8개의 방이 메인 경로 목표가 된다.

---

### 4. 임시 메인 경로 계획 방식 구현

현재 `DungeonLayoutGraph`는 이미 추가한 방을 되돌리는 삭제 기능을 제공하지 않는다.

따라서 32일차에서는 생성 도중 바로 그래프에 방을 추가하지 않고 다음과 같이 처리한다.

```text
PlannedRoom 목록
↓
임시 좌표 점유
↓
다음 출구·방 후보 탐색
↓
막힘
↓
이전 상태로 Backtracking
↓
다른 후보 탐색
↓
목표 길이 완성
↓
실제 DungeonLayoutGraph 생성
```

이를 통해 실패한 생성 시도의 방이 실제 그래프에 남지 않는다.

---

### 5. 좌표 중복 방지

임시 메인 경로가 사용하는 MacroCoordinate를 `HashSet<GridPosition>`으로 관리한다.

다음 방 후보 좌표가 이미 사용 중이면 해당 출구는 사용하지 않고 다른 출구를 탐색한다.

```text
현재 방
↓
출구 방향으로 다음 MacroCoordinate 계산
↓
이미 사용 중?
├─ Yes → 다른 후보
└─ No  → 임시 경로에 추가
```

따라서 하나의 메인 경로에서 같은 던전 좌표에 두 개의 방이 생성되지 않는다.

---

### 6. Backtracking 기반 경로 탐색

메인 경로 생성 중 막히면 생성 전체를 즉시 실패시키지 않는다.

현재 방에서:

1. 사용 가능한 출구 순서를 Seed 기반으로 섞음
2. 다음 방에 필요한 반대 방향 입구 계산
3. 조건을 만족하는 RoomTemplate 후보 수집
4. 후보 순서를 Seed 기반으로 섞음
5. 임시 경로에 추가
6. 다음 단계 재귀 탐색
7. 실패 시 추가했던 방과 좌표를 제거
8. 다음 후보 시도

방식으로 목표 길이까지 탐색한다.

---

### 7. 성공한 경로만 DungeonLayoutGraph에 확정

목표 길이까지 `PlannedRoom` 생성에 성공하면 그때 실제 그래프를 만든다.

```text
MainPath[0]
= EntryRoom

MainPath[1]
↓
MainPath[2]
↓
...

MainPath[last]
= StairsRoom
```

메인 경로의 각 방은 순서대로 실제 `RoomNode`로 등록되고 앞뒤 방이 연결된다.

---

### 8. GeneratedDungeon 메인 경로 정보 확장

`GeneratedDungeon`에 다음 정보가 추가되었다.

```text
MainPath
TargetMainPathLength
UsesControlledMainPath
MainPathCompleted
FailureReason
```

이를 통해 호출부에서 다음을 구분할 수 있다.

```text
생성 성공
MainPathCompleted = true

생성 실패
MainPathCompleted = false
FailureReason = 실패 원인
```

기존 생성자로 만든 결과는 호환을 위해 기존 방식대로 정상 결과로 취급한다.

---

### 9. 생성 실패 결과 처리

요구한 메인 경로 길이를 만들 수 없으면 잘못된 중간 그래프를 반환하지 않는다.

실패 시:

```text
EntryRoom 하나만 존재
MainPathCompleted = false
FailureReason 기록
```

상태로 반환한다.

이 구조는 이후 생성 실패 자동 재시도를 구현할 때 사용할 수 있다.

---

## EditMode 테스트

`MainPathGenerationTests.cs`에 **7개의 테스트**를 추가했다.

### 1. 설정값 검증

메인 경로 최대 길이가 전체 목표 방 수를 초과하면 예외가 발생하는지 확인한다.

### 2. 최소·최대 범위 검증

Seed로 선택된 `TargetMainPathLength`가 지정한 최소·최대 범위 안에 있는지 확인한다.

### 3. 고정 길이 및 계단 방 검증

최소·최대를 같은 값으로 설정했을 때 정확한 길이가 생성되고 메인 경로 마지막 방이 `StairsRoom`인지 확인한다.

### 4. 경로 연속성 검증

`MainPath`의 모든 앞뒤 방이 실제 `DungeonLayoutGraph`에서도 서로 연결되어 있는지 확인한다.

### 5. MacroCoordinate 중복 검증

메인 경로의 모든 방이 서로 다른 MacroCoordinate를 사용하는지 확인한다.

### 6. Seed 재현성 검증

동일한 Seed와 동일한 설정을 사용했을 때:

- 목표 메인 경로 길이
- 방 종류 순서
- MacroCoordinate

가 동일하게 생성되는지 확인한다.

### 7. 생성 실패 상태 검증

출구가 부족한 방 데이터로 요구 길이를 만들 수 없을 때:

- `MainPathCompleted == false`
- `FailureReason` 존재
- 실패한 임시 방이 그래프에 남지 않음

을 확인한다.

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/DungeonGenerationSettings.cs
Assets/ProjectDelta/Scripts/Domain/DungeonGenerationSettings.cs.meta
Assets/ProjectDelta/Tests/EditMode/MainPathGenerationTests.cs
Assets/ProjectDelta/Tests/EditMode/MainPathGenerationTests.cs.meta
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Domain/DungeonGenerator.cs
```

---

## 삭제 파일

없음.

---

## 현재 남은 한계

### 1. 전체 목표 방 수까지 확장하지 않음

`TargetRoomCount`는 생성 설정에 포함되어 있지만 32일차 설정 기반 생성은 메인 경로만 실제 그래프에 만든다.

남은 방을 메인 경로 주변에 붙이는 기능은 33일차 가지 경로 생성에서 구현한다.

### 2. 가지 경로와 막다른 방이 없음

현재 생성 결과는 시작 방부터 계단 방까지 이어지는 하나의 메인 경로가 중심이다.

탐험용 분기, 막다른 방, 특수 방 후보는 아직 생성하지 않는다.

### 3. 루프 연결이 없음

현재 메인 경로는 순차 연결 구조다.

서로 인접한 별도 경로를 다시 연결해 순환 구조를 만드는 기능은 이후 루프 연결 단계에서 구현한다.

### 4. 생성 실패 자동 재시도는 아직 없음

현재는 메인 경로를 만들 수 없으면 `FailureReason`이 포함된 실패 결과를 반환한다.

Seed 변경 또는 재시도를 자동 수행하는 기능은 이후 생성 안정화 단계에서 추가한다.

### 5. RoomView 자동 배치는 아직 없음

32일차 결과는 여전히 논리적인 던전 그래프다.

실제 RoomView 프리팹을 씬에 자동 배치하는 기능은 이후 던전 배치 단계에서 진행한다.

---

## 32일차 완료 판단

**32일차 목표인 시작 방→계단 방 메인 경로의 최소·최대 길이 및 목표 길이 생성 규칙 구현은 완료되었다.**

이제 던전 생성기는 단순히 방을 무작위로 늘리는 것뿐 아니라, 설정한 길이 범위 안에서 플레이어가 반드시 지나가야 하는 메인 진행 경로를 먼저 생성할 수 있다.

또한 실패한 임시 경로를 실제 그래프에 남기지 않고, 동일 Seed에서 동일한 경로를 재현할 수 있는 기반도 확보했다.

GitHub 저장소에는 현재 해당 커밋의 Actions/CI 실행 기록이 없으므로 Unity Test Runner의 실제 통과 여부는 로컬 Unity Editor에서 별도로 확인해야 한다.

---

## 다음 개발 방향

### 33일차

**메인 경로에서 가지 경로 생성, 막다른 방·특수 방 후보 지정과 분기 확률 구현**

32일차에서 완성한 메인 경로를 중심으로 남은 전체 목표 방 수를 채운다.

```text
Entry
  ↓
Main
  ↓
Main ── Branch ── Dead End
  ↓
Main ── Branch
  ↓
Main
  ↓
Stairs
```

다음 일차에서는:

- 메인 경로의 남은 출구 수집
- 분기 시작 방 선택
- 가지 경로 길이 결정
- `TargetRoomCount`까지 방 추가
- 막다른 방 후보 지정
- 특수 방 후보 지정
- 분기 확률 적용

순서로 확장할 예정이다.
