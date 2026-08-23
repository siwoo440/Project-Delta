# Project Delta - 28일차 개발일지

## 개발 주제

**절차적 던전 생성 — 던전 레이아웃 자료구조 설계**

11~27일차 던전 탐험 구간을 마무리하고, 오늘부터 절차적 던전 생성 구간(원래 26~35일차, 24~27일차 보완 작업으로 28~37일차로 조정)을 시작한다. 생성 알고리즘을 바로 짜지 않고, 이 프로젝트가 지금까지 지켜온 패턴("자료구조 먼저, 동작 나중")을 따라 방-방 연결을 나타내는 그래프 자료구조부터 만들었다.

---

## 개발 목표

- 방과 방 사이 연결을 나타내는 일반화된 그래프 자료구조 설계 (격자 기반, B안)
- 여러 형태의 방 모양이 앞으로 생길 것을 염두에 두고 확장 지점을 명확히 남기기
- 15일차 `RoomConnection`이 "정확히 두 방"으로 하드코딩되어 있던 제약 해소
- 자료구조가 실제로 동작한다는 것을 테스트로 검증

---

## 구현 내용

### 1. 사전 조사: 기존 코드가 던전 생성에 얼마나 준비되어 있는가

- 18일차에 만든 미로 방 10종(`RoomDefinition_Maze01~10`)은 전부 남쪽 경계 `(0,-2)`에 문이 **하나뿐**이다. 갈림길 있는 던전을 만들려면 이 리소스도 나중에 손봐야 한다.
- `RoomConnection`/`RoomConnectionEnd`(15일차)는 정확히 두 방(`EndA`/`EndB`)만 연결하도록 하드코딩되어 있어, N개의 방을 잇는 일반화된 구조가 아니다.
- "던전 레이아웃 그래프"에 해당하는 자료구조는 프로젝트 어디에도 없었다.

### 2. DungeonLayoutGraph — 방-방 연결 그래프 (신규)

```text
DungeonLayoutGraph (Domain)
├─ RoomNode — 방 하나 (RoomId, DefinitionId, MacroCoordinate, 4방향 연결)
├─ RoomConnectionEdge — 한 방향 연결 (이웃 방 + 잠김 여부)
├─ AddRoom() — 좌표 중복 시 예외
├─ TryGetRoom() / TryGetRoomAt() — 식별자·좌표 양쪽으로 조회
└─ Connect() — 한쪽만 지정하면 반대 방향도 자동 연결 (RoomGridLayout.GetOpposite 재사용)
```

`RoomGridLayout`(방 하나 내부 칸 단위 통로)과 스케일이 다르다는 점을 주석으로 명확히 했다 — `DungeonLayoutGraph`는 "방 하나 = 노드 하나"인 던전 전체 지도용이고, 방 내부 모양은 계속 `RoomDefinition`이 담당한다.

**여러 방 모양 대비**: `RoomNode.MacroCoordinate`는 지금 "방 하나 = 격자 한 칸"을 가정한다. 나중에 여러 칸을 차지하는 방이 생기면 이 필드 하나를 점유 칸 목록으로 바꾸면 되도록 확장 지점을 주석으로 남겨뒀다.

### 3. GridPosition — 값 동등 비교 추가

`DungeonLayoutGraph`가 `Dictionary<GridPosition, RoomNode>`로 좌표별 조회를 하려면 제대로 된 동등 비교가 필요했다. 지금까지는 좌표 비교를 전부 `.X ==`/`.Z ==` 식으로 손으로 해왔는데(`RoomConnection.Matches()` 등), `GridPosition`에 `IEquatable<GridPosition>`과 `Equals`/`GetHashCode`/`==`/`!=`를 추가해서 Dictionary 키로 바로 쓸 수 있게 했다. 기존 코드의 비교 방식과 결과가 같아서 순수 추가이자 무해한 변경이다.

### 4. DungeonRunState — 층별 레이아웃 그래프 보유

`DungeonRunState`(Domain)에 `Layout`(DungeonLayoutGraph) 프로퍼티를 추가했다. `AdvanceFloor()`(22일차) 호출 시 새 그래프로 초기화된다 — 되돌아가는 방향이 없는 게임(기획서 3.1절)이라 이전 층 연결 정보는 버려도 된다.

### 5. 테스트로 검증

`DungeonLayoutGraphTests.cs`(EditMode)를 15일차 `RoomConnectionTests.cs`와 같은 스타일로 작성했다. 방 추가·조회, 양방향 연결, 잠김 정보 양쪽 전달, 좌표 중복 차단, 연결 없는 방향 조회 실패까지 5개 테스트로 검증했다.

---

## 적용 중 발견된 문제 및 수정

없음.

---

## 현재 28일차 전체 흐름

```text
기존 코드 조사 (미로 방 문 개수, RoomConnection의 2방 제약, 레이아웃 그래프 부재 확인)
↓
GridPosition에 값 동등 비교 추가 (Dictionary 키로 쓰기 위한 선행 작업)
↓
DungeonLayoutGraph(RoomNode/RoomConnectionEdge) 신규 구현
↓
DungeonRunState에 Layout 프로퍼티 연결, 층 전환 시 초기화
↓
EditMode 테스트 5개로 검증
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/DungeonLayoutGraph.cs
Assets/ProjectDelta/Scripts/Domain/DungeonLayoutGraph.cs.meta
Assets/ProjectDelta/Tests/EditMode/DungeonLayoutGraphTests.cs
Assets/ProjectDelta/Tests/EditMode/DungeonLayoutGraphTests.cs.meta
Devlogs/Day28/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Domain/GridPosition.cs (IEquatable 구현 추가)
Assets/ProjectDelta/Scripts/Domain/RunSubStates.cs (DungeonRunState.Layout 추가)
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

28일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- Test Runner EditMode에서 `DungeonLayoutGraphTests`의 5개 테스트 전부 통과
- 기존 씬/플레이 동작에 변화 없음 (아직 아무도 이 그래프를 실제로 채우지 않으므로 당연한 결과)

**참고**: 오늘은 자료구조만 만들었다. 이 그래프를 실제로 채우는 절차적 생성 알고리즘, 계단이 반드시 도달 가능한 위치에 배치되도록 보장하는 로직, 미로 방들의 "문 하나뿐" 제약 해소는 이후 일차로 이어진다.

---

## 다음 개발 방향

29일차부터는 오늘 만든 `DungeonLayoutGraph`를 실제로 채우는 절차적 생성 알고리즘을 만든다. 우선 미로 방 리소스의 문 배치 제약(남쪽 문 하나뿐)을 어떻게 해소할지부터 정해야 한다.
