# Project Delta - 29일차 개발일지

## 개발 주제

**절차적 던전 생성 — 던전 생성 알고리즘 기초 구현**

28일차에 만든 `DungeonLayoutGraph`를 실제로 채우는 `DungeonGenerator`를 구현했다. 현재 방 콘텐츠 대부분이 경계 출구 1개만 가진다는 제약을 고려하면서도, 이후 다중 출구 방이 추가되면 같은 생성기가 갈림길을 만들 수 있도록 `RoomTemplate`과 프론티어 확장 방식을 도입했다.

---

## 개발 목표

- `DungeonLayoutGraph`를 실제 방 노드와 연결로 채우는 생성기 구현
- 시작 방에서 생성된 모든 방의 도달 가능성 보장
- 생성 종료 후 계단 후보 방 선정
- 출구 개수가 다른 방에도 대응 가능한 구조 마련
- 생성 실패 상황에서 무한 반복하지 않도록 안전장치 추가

---

## 구현 내용

### 1. RoomTemplate 추가

`DungeonGenerator`가 `Data.RoomDefinition`을 직접 참조하지 않도록 Domain 계층에 `RoomTemplate`을 추가했다.

현재 `RoomTemplate`은 다음 정보만 가진다.

```text
RoomTemplate
├─ DefinitionId
└─ ExitDirections
```

`RoomDefinition.ToRoomTemplate()`이 정적 방 데이터를 생성기용 최소 데이터로 변환한다.

### 2. RoomDefinition 경계 출구 계산

`RoomDefinition`에 다음 기능을 추가했다.

```text
MinX / MaxX / MinZ / MaxZ
GetExits()
ToRoomTemplate()
```

`GetExits()`는 `PassageType.Door` 중에서 이웃 칸이 방 범위를 벗어나는 문만 던전 연결용 경계 출구로 판단한다. 따라서 방 내부 이동용 문은 던전 방 연결 출구에서 제외된다.

### 3. DungeonGenerator 추가

`DungeonGenerator.Generate()`는 다음 순서로 동작한다.

```text
시작 방을 (0, 0)에 배치
↓
아직 사용하지 않은 출구를 프론티어로 관리
↓
무작위 출구 방향 선택
↓
반대 방향 출구를 가진 RoomTemplate 검색
↓
좌표 충돌 여부 확인
↓
새 RoomNode 생성 및 연결
↓
목표 방 수 또는 확장 불가 조건에서 종료
```

`maxAttempts`를 두어 콘텐츠 부족이나 연결 불가능 상태에서도 생성기가 무한 반복하지 않도록 했다.

### 4. 계단 방 후보 선정

생성 완료 후 BFS로 시작 방에서 가장 먼 막다른 방을 찾고 `GeneratedDungeon.StairsRoom`으로 지정한다.

현재 단계에서는 **계단 오브젝트를 실제 씬에 배치하는 것이 아니라 논리적인 계단 후보 방만 결정**한다.

### 5. GridPosition 연산 보강

생성기가 현재 방 좌표에서 방향 벡터를 더해 다음 방의 매크로 좌표를 구할 수 있도록 `GridPosition`에 `+` 연산자를 추가했다.

---

## 테스트

`DungeonGeneratorTests.cs`에는 실제로 **5개의 EditMode 테스트**가 작성되어 있다.

1. 다중 출구 방으로 목표 방 수까지 생성 가능한지 확인
2. 생성된 모든 방이 시작 방에서 도달 가능한지 확인
3. 계단 후보 방이 막다른 방인지 확인
4. 출구 1개짜리 방만 있을 때 2개 방에서 안전하게 종료되는지 확인
5. 시작 방에 출구가 없을 때 1개 방으로 종료되는지 확인

개발 과정에서 별도 순수 C# 실행 환경으로 생성 로직을 검사했고, 보고된 로컬 검증에서는 여러 시드와 생성 개수 조합이 통과했다.

단, GitHub 저장소에는 해당 로컬 실행 결과나 Unity Test Runner 결과가 CI 기록으로 남아 있지 않다. 따라서 Unity 에디터에서 `DungeonGeneratorTests`와 기존 `DungeonLayoutGraphTests`가 통과하는지는 별도로 확인해야 한다.

---

## 적용 중 수정한 문제

`IReadOnlyList<T>`에 직접 `Contains()`를 호출하던 코드를 제거하고 `HasExitDirection()`의 직접 순회 방식으로 변경했다.

Domain 계층에 불필요하게 LINQ 의존성을 추가하지 않고 현재 프로젝트 구조를 유지했다.

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/RoomTemplate.cs
Assets/ProjectDelta/Scripts/Domain/RoomTemplate.cs.meta
Assets/ProjectDelta/Scripts/Domain/DungeonGenerator.cs
Assets/ProjectDelta/Scripts/Domain/DungeonGenerator.cs.meta
Assets/ProjectDelta/Tests/EditMode/DungeonGeneratorTests.cs
Assets/ProjectDelta/Tests/EditMode/DungeonGeneratorTests.cs.meta
Devlogs/Day29/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Domain/GridPosition.cs
Assets/ProjectDelta/Scripts/Data/RoomDefinition.cs
```

---

## 커밋에 함께 포함된 삭제 파일

```text
Assets/Scenes.meta
```

29일차 기능 구현과 직접 관계없는 삭제다. `Assets/Scenes` 폴더가 계속 사용 중이라면 Unity에서 새 `.meta`가 생성되기 전에 기존 파일을 복원할 필요가 있는지 확인한다.

---

## 현재 남은 한계

### 1. 출구의 위치 정보가 생성 데이터에서 사라짐

`RoomDefinition.GetExits()` 단계에서는 문마다 `X`, `Z`, `Direction` 정보를 가지고 있지만, 현재 `RoomTemplate`에는 `Direction`만 저장된다.

따라서 같은 북쪽 출구라도 서로 다른 위치에 있는 문을 구분할 수 없다.

```text
(-2, 2) North
(0, 2) North
(2, 2) North
```

이 상태로 실제 방 프리팹을 배치하면 그래프상 연결은 맞지만 실제 문 위치가 서로 어긋날 수 있다. 실제 씬 배치 전에 출구 위치를 보존하는 구조가 필요하다.

### 2. 현재 실사용 방 대부분이 경계 출구 1개

출구 1개 방만으로는 긴 메인 경로나 갈림길을 만들 수 없다. 생성 알고리즘을 실제 던전에서 사용하려면 다중 출구 `RoomDefinition`과 대응 프리팹이 필요하다.

### 3. 실제 씬과 아직 연결되지 않음

29일차까지는 `GeneratedDungeon`이라는 논리 결과만 만든다. 실제 `RoomView` 프리팹 배치, 문 연결, 플레이어 이동, 저장·복원은 이후 일차에서 연결한다.

---

## 29일차 완료 판단

**알고리즘 기초 구현은 완료했지만 절차적 던전 시스템 전체가 완성된 것은 아니다.**

29일차 결과는 이후 던전 생성 기능을 만들기 위한 기반으로 사용한다.

---

## 다음 개발 방향

30~37일차는 다음 순서로 진행한다.

```text
30일차  RoomTemplate에 출구 좌표·방향을 보존하는 RoomExit 구조 추가 및 RoomDefinition 변환 규칙 보강
31일차  다중 출구 방 규격 확정, 테스트용 2·3·4출구 RoomDefinition·프리팹 제작 및 문 정렬 검증
32일차  시작 방→계단 방 메인 경로 최소·최대 길이와 목표 길이 생성 규칙 구현
33일차  메인 경로에서 가지 경로 생성, 막다른 방·특수 방 후보 지정과 분기 확률 구현
34일차  인접 방 루프 연결, 출구 위치 일치·좌표 충돌·중복 연결 방지 규칙 구현
35일차  방 수·거리·연결 수 제약, 생성 실패 감지·재시도·Seed 기록/동일 Seed 재현 규칙 통합
36일차  GeneratedDungeon을 DungeonFloorController와 연결해 실제 RoomView 프리팹·문·계단 배치
37일차  생성 던전 Seed·레이아웃·방 상태 저장/복원과 10,000회 자동 생성 검증·실패 로그 구현
```

38일차부터는 생성된 실제 던전 그래프를 기준으로 미니맵과 전체 맵 UI 작업으로 넘어간다.
