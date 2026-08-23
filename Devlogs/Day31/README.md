# Project Delta - 31일차 개발일지

## 개발 주제

**절차적 던전 생성 — 다중 출구 방 규격 확정 및 테스트 자산 제작**

30일차에 `RoomExit`을 도입해 방 경계 출구의 좌표와 방향을 보존할 수 있게 만들었다.

31일차에서는 이 구조를 실제 테스트 콘텐츠에 적용해 2·3·4출구 `RoomDefinition`과 대응 `RoomView` 프리팹을 제작하고, 데이터상의 출구 위치와 프리팹상의 실제 문 위치가 동일한 규격을 따르는지 검증할 수 있는 기반을 구축했다.

---

## 개발 목표

- 5×5 방의 중앙 출구 규격 확정
- 2출구 직선형·꺾임형 테스트 방 제작
- 3출구 T자형 테스트 방 제작
- 4출구 십자형 테스트 방 제작
- `RoomDefinition`과 프리팹 출구 위치 대응
- `RoomExitMarker`를 이용한 프리팹 출구 좌표·방향 표현
- 테스트 자산 자동 생성·재검증 Editor 도구 추가
- 다중 출구 구조 관련 EditMode 테스트 추가

---

## 출구 규격

31일차 테스트 방은 5×5 그리드를 기준으로 각 벽의 중앙을 기본 출구 위치로 사용한다.

```text
North : ( 0,  2)
East  : ( 2,  0)
South : ( 0, -2)
West  : (-2,  0)
```

월드에서 한 칸의 크기는 기존 프로젝트 규격에 맞춰 `2`를 사용한다.

따라서 5×5 방 하나의 월드 크기는 10×10이며, 인접 방을 X 또는 Z 방향으로 10만큼 떨어뜨려 배치하면 서로 마주 보는 중앙 문이 같은 위치에서 맞닿는 구조다.

---

## 구현 내용

### 1. 다중 출구 RoomDefinition 제작

다음 네 종류의 테스트 방 데이터를 생성했다.

```text
ROOM_TEST_NS
North + South
```

```text
ROOM_TEST_NE
North + East
```

```text
ROOM_TEST_T
North + East + West
```

```text
ROOM_TEST_CROSS
North + East + South + West
```

모든 테스트 방은 5×5 크기를 사용하며 각 출구는 `PassageType.Door`로 정의되어 있다.

---

### 2. RoomExitMarker 추가

`RoomView` 프리팹에서 각 실제 문 위치가 어떤 논리 출구에 대응하는지 표현하기 위해 `RoomExitMarker`를 추가했다.

각 마커는 다음 정보를 가진다.

```text
RoomExitMarker
├─ LocalPosition
└─ Direction
```

필요할 때 이를 바로 Domain의 `RoomExit` 값으로 변환할 수 있다.

이를 통해 이후 실제 던전 방 배치 단계에서 프리팹의 문 위치와 생성 데이터의 출구를 연결할 수 있는 기반이 마련됐다.

---

### 3. 테스트용 RoomView 프리팹 제작

다음 테스트 프리팹을 생성했다.

```text
Room_Test_NS
Room_Test_NE
Room_Test_T
Room_Test_CROSS
```

각 프리팹에는 다음 요소가 포함된다.

- 5×5 크기의 임시 바닥
- 출구가 없는 방향의 외곽 벽
- 출구가 있는 방향의 문 공간
- 실제 문 위치 확인용 `DoorVisual`
- 각 문에 대응하는 `RoomExitMarker`
- 기존 `RoomView`
- 기존 `RoomPassageController`

현재 프리팹은 게임용 최종 아트가 아니라 출구 규격과 정렬을 확인하기 위한 테스트 자산이다.

---

### 4. Day31MultiExitRoomGenerator 추가

Unity Editor에서 31일차 테스트 자산을 다시 생성하거나 검증할 수 있도록 Editor 도구를 추가했다.

메뉴:

```text
Project Delta
└─ Day31
   ├─ Generate Multi-Exit Test Rooms
   └─ Validate Multi-Exit Test Rooms
```

생성 메뉴는 다음 작업을 자동으로 처리한다.

```text
테스트 RoomDefinition 생성/갱신
↓
대응 테스트 RoomView 프리팹 생성/갱신
↓
RoomExitMarker 배치
↓
출구 개수·좌표·방향 검증
↓
프리팹 문 위치 검증
↓
North/South 및 East/West 인접 정렬 검증
```

---

### 5. Editor 어셈블리 참조 보강

31일차 Editor 도구가 Domain과 Presentation 계층의 타입을 사용할 수 있도록 `ProjectDelta.Editor.asmdef`의 참조를 보강했다.

추가된 주요 참조:

```text
ProjectDelta.Domain
ProjectDelta.Presentation
```

---

## 테스트

`MultiExitRoomTests.cs`에 **4개의 EditMode 테스트**가 추가되었다.

1. 4방향 `RoomExit`이 `RoomTemplate`에 모두 보존되는지 확인
2. 같은 방향이더라도 좌표가 다른 출구를 별개의 출구로 구분하는지 확인
3. North/South와 East/West의 정렬 축 및 반대 방향 규칙을 확인
4. 실제 좌표를 가진 4출구 템플릿으로 `DungeonGenerator`가 목표 방 수까지 생성 가능한지 확인

GitHub 저장소에는 현재 이 커밋에 대한 CI 상태가 등록되어 있지 않다.

따라서 Unity Editor에서 전체 EditMode 테스트가 실제로 통과하는지는 별도로 확인해야 한다.

---

## 생성 데이터 자산

```text
Assets/ProjectDelta/Data/Rooms/Day31/RoomDefinition_Test_NS.asset
Assets/ProjectDelta/Data/Rooms/Day31/RoomDefinition_Test_NE.asset
Assets/ProjectDelta/Data/Rooms/Day31/RoomDefinition_Test_T.asset
Assets/ProjectDelta/Data/Rooms/Day31/RoomDefinition_Test_CROSS.asset
```

각 자산의 `.meta` 파일과 `Day31` 폴더 메타 파일도 함께 생성되었다.

---

## 생성 프리팹

```text
Assets/ProjectDelta/Prefabs/Dungeon/Day31/Room_Test_NS.prefab
Assets/ProjectDelta/Prefabs/Dungeon/Day31/Room_Test_NE.prefab
Assets/ProjectDelta/Prefabs/Dungeon/Day31/Room_Test_T.prefab
Assets/ProjectDelta/Prefabs/Dungeon/Day31/Room_Test_CROSS.prefab
```

각 프리팹의 `.meta` 파일과 `Day31` 폴더 메타 파일도 함께 생성되었다.

---

## 생성 코드

```text
Assets/ProjectDelta/Scripts/Editor/Day31MultiExitRoomGenerator.cs
Assets/ProjectDelta/Scripts/Presentation/RoomExitMarker.cs
Assets/ProjectDelta/Tests/EditMode/MultiExitRoomTests.cs
```

각 스크립트의 `.meta` 파일도 함께 생성되었다.

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Editor/ProjectDelta.Editor.asmdef
```

---

## 삭제 파일

없음.

---

## 현재 남은 한계

### 1. 테스트 프리팹은 최종 게임 방이 아님

31일차 프리팹은 출구 위치와 정렬 규칙을 검증하기 위한 테스트 콘텐츠다.

실제 지역 테마, 환경 오브젝트, 게임 플레이용 최종 방 구성은 이후 콘텐츠 제작 단계에서 별도로 제작한다.

### 2. DungeonGenerator의 방 선택은 아직 출구 위치 정렬을 강제하지 않음

현재 `DungeonGenerator`는 `RoomExit` 정보를 보존하지만 새 방 후보 선택 자체는 필요한 반대 방향 출구의 존재 여부를 중심으로 동작한다.

출구 위치 일치와 충돌·중복 연결 방지는 이후 던전 생성 단계에서 확장한다.

### 3. 생성된 논리 던전을 실제 프리팹으로 자동 배치하지 않음

테스트 프리팹 자체는 제작됐지만 `GeneratedDungeon`의 방 노드를 읽어 씬에 자동 배치하는 기능은 아직 연결하지 않았다.

해당 작업은 이후 실제 던전 배치 단계에서 진행한다.

---

## 31일차 완료 판단

**31일차 목표인 다중 출구 방 규격 확정, 2·3·4출구 테스트 데이터·프리팹 제작, 출구 위치 정렬 검증 기반 구축은 완료되었다.**

30일차에 만든 `RoomExit` 데이터 구조가 실제 다중 출구 콘텐츠에도 적용될 수 있는 것을 확인할 수 있는 형태가 마련됐다.

---

## 다음 개발 방향

### 32일차

**시작 방 → 계단 방 메인 경로의 최소·최대 길이와 목표 길이 생성 규칙 구현**

31일차까지는 어떤 형태의 방을 서로 연결할 수 있는지를 준비했다.

32일차부터는 던전의 전체 진행 구조를 제어한다.

```text
시작 방
↓
메인 경로 목표 길이 결정
↓
연결 가능한 다중 출구 방 선택
↓
최소 길이 보장
↓
최대 길이 제한
↓
목표 지점에 계단 방 배치
```

이후 33일차에서 메인 경로 바깥으로 가지 경로와 막다른 방을 확장한다.
