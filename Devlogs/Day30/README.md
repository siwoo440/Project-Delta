# Project Delta - 30일차 개발일지

## 개발 주제

**절차적 던전 생성 — RoomExit 도입과 출구 좌표·방향 보존**

29일차에는 `RoomTemplate`이 방의 경계 출구를 `CardinalDirection`만으로 보관하고 있어, 같은 방향에 서로 다른 위치의 문이 존재할 경우 이를 구분할 수 없었다.

30일차에서는 `RoomExit` 구조를 추가해 각 출구의 방 내부 좌표와 방향을 함께 보존하도록 변경했다. 또한 `RoomDefinition → RoomTemplate` 변환 과정과 `DungeonGenerator`의 프론티어를 `RoomExit` 기반으로 수정해 이후 다중 출구 방과 실제 문 정렬 규칙을 구현할 수 있는 기반을 마련했다.

---

## 개발 목표

- 방 경계 출구의 `X/Z` 좌표와 방향을 함께 보존
- `RoomDefinition`의 출구 정보가 `RoomTemplate` 변환 과정에서 손실되지 않도록 수정
- `DungeonGenerator`가 단순 방향 목록이 아닌 실제 출구 목록을 사용하도록 변경
- 기존 방향 기반 코드와 테스트가 즉시 깨지지 않도록 호환 구조 유지
- 출구 간 방향·정렬 가능 여부를 판정할 수 있는 기초 규칙 추가
- 출구 데이터 보존 관련 EditMode 테스트 추가

---

## 구현 내용

### 1. RoomExit 구조 추가

Domain 계층에 `RoomExit`을 새로 추가했다.

```text
RoomExit
├─ LocalPosition
│  ├─ X
│  └─ Z
└─ Direction
```

기존에는 다음처럼 방향만 구분했다.

```text
North
East
South
West
```

이제는 다음처럼 동일 방향의 서로 다른 위치도 별개의 출구로 구분할 수 있다.

```text
(0, 2) / North
(1, 2) / North
(-1, 2) / North
```

`RoomExit`은 좌표와 방향을 기준으로 값 비교가 가능하며, 디버깅 시 좌표와 방향을 함께 출력할 수 있다.

### 2. 출구 연결 가능성 기초 규칙 추가

`RoomExit.CanConnectTo()`를 추가했다.

현재 규칙은 다음과 같다.

- 서로 반대 방향이어야 한다.
- North/South 출구는 X 위치가 같아야 한다.
- East/West 출구는 Z 위치가 같아야 한다.

```text
North X=1
    ↕
South X=1
→ 연결 가능

North X=1
    ↕
South X=-1
→ 연결 불가
```

이 규칙은 이후 다중 출구 방의 문 정렬과 실제 방 연결 검증에 사용할 예정이다.

### 3. RoomTemplate 구조 보강

기존 `RoomTemplate`은 다음 정보만 보관했다.

```text
RoomTemplate
├─ DefinitionId
└─ ExitDirections
```

30일차부터는 다음 구조를 사용한다.

```text
RoomTemplate
├─ DefinitionId
├─ Exits
│  └─ RoomExit 목록
└─ ExitDirections
   └─ 기존 코드 호환용 방향 목록
```

신규 던전 생성 코드는 `Exits`를 사용한다.

기존 코드가 즉시 깨지는 것을 막기 위해 `ExitDirections`와 방향 목록을 받는 기존 생성자도 호환용으로 유지했다.

### 4. RoomDefinition 변환 규칙 수정

`RoomDefinition.GetExits()`가 찾아낸 `PassageEntry`의 다음 정보를 모두 보존하도록 `ToRoomTemplate()`을 수정했다.

```text
PassageEntry
├─ X
├─ Z
└─ Direction
        ↓
RoomExit
├─ LocalPosition
└─ Direction
```

29일차까지 발생하던 출구 좌표 손실 문제가 해결되었다.

Domain 계층이 `RoomDefinition`을 직접 참조하지 않고 Data 계층에서 Domain용 데이터로 변환하는 기존 구조는 그대로 유지했다.

### 5. DungeonGenerator 프론티어 수정

29일차 생성기는 프론티어에 남은 출구 방향만 저장했다.

```text
List<CardinalDirection>
```

30일차부터는 실제 출구를 저장한다.

```text
List<RoomExit>
```

따라서 생성기가 방을 확장하는 동안 어떤 방향의 출구를 사용했는지뿐 아니라 해당 출구의 실제 로컬 좌표도 유지할 수 있다.

새 방을 연결할 때 사용한 `RoomExit` 하나만 정확하게 제거하며, 남은 출구가 있는 방만 계속 프론티어에 남도록 정리했다.

현재 생성 자체는 기존 방향 기반 연결 규칙을 유지한다. 실제 출구 위치를 이용한 프리팹 문 정렬과 생성 제약은 이후 던전 생성 단계에서 확장한다.

---

## 테스트

`RoomExitTests.cs`에 **3개의 EditMode 테스트**를 추가했다.

1. `RoomExit`이 LocalPosition과 Direction을 그대로 보존하는지 확인
2. 반대 방향과 정렬 축을 이용해 `CanConnectTo()`가 연결 가능 여부를 판정하는지 확인
3. `RoomDefinition.ToRoomTemplate()` 변환 시 내부 문은 제외하고 경계 출구의 좌표와 방향이 유지되는지 확인

기존 `DungeonGeneratorTests`는 기존 방향 전용 `RoomTemplate` 생성자를 사용할 수 있도록 호환 구조를 유지했다.

GitHub 저장소에는 현재 이 커밋에 대한 CI 상태 기록이 없다. 따라서 Unity Test Runner의 실제 통과 결과는 Unity 에디터에서 별도로 확인해야 한다.

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/RoomExit.cs
Assets/ProjectDelta/Scripts/Domain/RoomExit.cs.meta
Assets/ProjectDelta/Tests/EditMode/RoomExitTests.cs
Assets/ProjectDelta/Tests/EditMode/RoomExitTests.cs.meta
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Data/RoomDefinition.cs
Assets/ProjectDelta/Scripts/Domain/RoomTemplate.cs
Assets/ProjectDelta/Scripts/Domain/DungeonGenerator.cs
```

---

## 삭제 파일

없음.

---

## 현재 남은 한계

### 1. 실제 다중 출구 방 콘텐츠가 아직 없음

`RoomExit` 구조는 다중 출구를 구분할 수 있지만 실제 2·3·4출구 `RoomDefinition`과 대응 프리팹 제작은 아직 진행하지 않았다.

### 2. DungeonGenerator가 아직 출구 정렬을 생성 조건으로 강제하지 않음

생성기는 `RoomExit`을 프론티어에서 보존하지만 현재 방 후보 선택은 반대 방향 출구 존재 여부를 기준으로 한다.

`RoomExit.CanConnectTo()`로 위치 정렬을 판정할 기반은 마련되어 있으며, 실제 문 위치 일치·충돌·중복 연결 방지 규칙은 이후 던전 생성 단계에서 확장한다.

### 3. 실제 RoomView 배치와 아직 연결되지 않음

현재 결과는 여전히 논리적인 `GeneratedDungeon` 그래프다.

실제 `RoomView` 프리팹 배치와 문·계단 연결은 이후 일차에서 진행한다.

---

## 30일차 완료 판단

**30일차 목표인 출구 좌표·방향 보존 구조와 RoomDefinition 변환 규칙 보강은 완료되었다.**

29일차에 남아 있던 출구 위치 정보 손실 문제를 해결했으며, 다중 출구 방과 실제 문 정렬을 구현할 수 있는 데이터 기반을 확보했다.

---

## 다음 개발 방향

### 31일차

**다중 출구 방 규격 확정 및 테스트용 2·3·4출구 방 제작**

다음 일차에서는 실제 `RoomExit` 데이터를 사용하는 테스트 방을 제작한다.

```text
2출구 RoomDefinition / RoomView
3출구 RoomDefinition / RoomView
4출구 RoomDefinition / RoomView
↓
각 출구 LocalPosition 검증
↓
방향별 문 위치 정렬 검증
↓
DungeonGenerator 다중 출구 생성 테스트
```

31일차 결과를 기준으로 이후 메인 경로 생성, 가지 경로, 루프 연결 규칙을 확장한다.
