# Project Delta - 41일차 개발일지

## 개발 목표

40일차에서 논리적으로 배정한 Monster Encounter를 실제 던전 방 안에 표시되는 정지형 테스트 몬스터로 연결한다.

이번 일차에서는 몬스터 AI나 전투 진입을 구현하지 않고, Monster Encounter가 지정된 방에서 유효한 GridPosition 하나를 결정하여 테스트용 Capsule 몬스터를 배치하는 단계까지만 구현한다.

- Monster Encounter가 배정된 방만 실제 스폰 대상으로 사용
- 방 내부 유효 GridPosition 후보 계산
- 연결된 문 칸과 문 안쪽 안전 칸 제외
- 기존 RoomContentMarker 점유 칸 제외
- 동일 Seed에서 동일 스폰 위치 재현
- 정지형 Capsule 테스트 몬스터 생성
- 몬스터의 RoomId / MonsterDefinitionId / GridPosition 보관
- EditMode 테스트 추가

---

## 구현 내용

### 1. MonsterSpawnPositionService 추가

Monster Encounter가 지정된 방 안에서 실제 몬스터 스폰 위치를 선택하는 `MonsterSpawnPositionService`를 추가했다.

서비스는 `RoomDefinition`의 방 내부 범위를 기준으로 모든 GridPosition을 후보로 만든 뒤, 사용할 수 없는 칸을 제거한다.

스폰 후보 계산 흐름은 다음과 같다.

```text
RoomDefinition 범위
↓
전체 GridPosition 생성
↓
기존 콘텐츠 점유 칸 제거
↓
연결된 문 칸 제거
↓
문 안쪽 1칸 안전 영역 제거
↓
남은 후보 반환
```

유효한 후보가 하나도 없으면 몬스터를 생성하지 않는다.

### 2. 연결된 문 주변 스폰 방지

몬스터가 방 입구를 막거나 플레이어가 방에 들어오자마자 바로 겹치는 상황을 줄이기 위해 연결된 출구의 다음 두 칸을 예약 영역으로 처리한다.

```text
문 칸
+
문에서 방 안쪽으로 한 칸
```

예를 들어 북쪽 문이 `(0, 2)`에 있다면 `(0, 2)`와 `(0, 1)`은 몬스터 스폰 후보에서 제외된다.

모든 RoomDefinition의 Passage가 아니라 현재 GeneratedDungeon에서 실제 이웃 방과 연결된 `RoomConnectionEdge.LocalExit`만 사용한다.

### 3. 기존 RoomContentMarker 점유 칸 제외

해당 RoomView 아래에 이미 존재하는 `RoomContentMarker`들의 GridPosition을 수집한다.

따라서 다음과 같은 기존 콘텐츠가 사용하는 칸에는 테스트 몬스터를 배치하지 않는다.

- 계단
- 상자
- SecretWall
- NPC Point
- Ambient Prop
- 기타 RoomContentMarker 기반 콘텐츠

이 구조는 이후 다른 탐험 콘텐츠가 추가되어도 같은 점유 칸 제외 규칙을 재사용할 수 있다.

### 4. Seed 기반 스폰 위치 결정

스폰 위치는 일반 Unity Random에 의존하지 않고 40일차에서 만든 결정적 난수 계산을 재사용한다.

사용하는 값은 다음과 같다.

```text
DungeonSeed
+ RoomId
+ MonsterDefinitionId:SPAWN
```

따라서 같은 던전 Seed, 같은 방, 같은 몬스터 정의에서는 동일한 후보 목록이 만들어지는 한 같은 GridPosition이 선택된다.

현재 몬스터는 이동하지 않기 때문에 별도의 현재 위치 저장 데이터를 추가하지 않고 Seed 기반 재현을 사용한다.

### 5. ExplorationMonsterMarker 추가

탐험 화면에 실제 배치된 몬스터의 논리 정보를 보관하기 위해 `ExplorationMonsterMarker`를 추가했다.

보관 정보:

```text
RoomId
MonsterDefinitionId
GridPosition
```

이 컴포넌트는 이후 플레이어와 몬스터의 논리 좌표를 비교하는 접촉 판정과 탐험 몬스터 AI의 기반으로 사용할 수 있다.

### 6. 정지형 테스트 몬스터 실제 생성

`DungeonFloorController`가 40일차의 `CurrentEncounterLayout`을 만든 뒤 `Monster` 타입으로 배정된 방을 순회한다.

각 방에서 유효한 GridPosition 하나를 선택한 후 Unity 기본 Capsule을 생성한다.

생성되는 오브젝트 이름은 다음 형식이다.

```text
Monster_{MonsterDefinitionId}_{RoomId}
```

몬스터는 해당 RoomView의 자식으로 배치된다.

현재 테스트 Visual은 다음 설정을 사용한다.

```text
Primitive: Capsule
Scale: 약 0.65 / 0.75 / 0.65
Collider: Trigger
```

Collider를 Trigger로 설정하여 아직 물리적으로 플레이어의 그리드 이동을 막지 않도록 했다.

### 7. RoomContentType.Monster 연결

생성된 테스트 몬스터에 다음 두 컴포넌트를 런타임으로 추가한다.

```text
ExplorationMonsterMarker
RoomContentMarker(ContentType = Monster)
```

따라서 RoomView의 기존 콘텐츠 마커 구조에서도 몬스터 위치를 조회할 수 있다.

몬스터 생성 후 `RoomView.RefreshMarkers()`를 호출하여 런타임으로 추가된 Monster 마커가 RoomView의 콘텐츠 목록에 반영되도록 했다.

### 8. DungeonFloorController의 몬스터 목록 관리

`DungeonFloorController`에 다음 목록을 추가했다.

```text
SpawnedMonsters
```

RoomId를 키로 사용하여 현재 층에서 생성된 정지형 테스트 몬스터를 조회할 수 있다.

현재 40일차 규칙이 방당 Monster Encounter 최대 1개이므로 RoomId 하나당 테스트 몬스터 하나를 관리한다.

층을 제거하거나 새 층으로 교체할 때는 `spawnedMonsters` 목록도 함께 초기화한다.

### 9. EditMode 테스트 추가

`MonsterSpawnPositionTests`를 추가했다.

테스트 항목은 다음 6가지다.

1. 연결된 문 칸과 문 안쪽 안전 칸 제외
2. 기존 콘텐츠 점유 GridPosition 제외
3. 같은 Seed와 같은 방에서 동일한 GridPosition 선택
4. 선택된 위치가 RoomDefinition 범위 안에 존재
5. 모든 칸이 예약되었을 때 스폰 실패 반환
6. 여러 문이 존재할 때 각 문과 각 안전 칸을 올바르게 제외

---

## 41일차 동작 흐름

```text
40일차 CurrentEncounterLayout
↓
Monster Encounter 방 조회
↓
해당 RoomView / RoomDefinition 조회
↓
연결된 출구 목록 수집
↓
기존 RoomContentMarker 점유 위치 수집
↓
MonsterSpawnPositionService
↓
유효한 GridPosition 후보 생성
↓
Seed 기반으로 한 칸 선택
↓
정지형 Capsule 생성
↓
ExplorationMonsterMarker 추가
↓
RoomContentMarker(Monster) 추가
↓
RoomView.RefreshMarkers()
```

---

## 변경 파일

### 생성

- `Assets/ProjectDelta/Scripts/Application/MonsterSpawnPositionService.cs`
- `Assets/ProjectDelta/Scripts/Application/MonsterSpawnPositionService.cs.meta`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterMarker.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterMarker.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/MonsterSpawnPositionTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/MonsterSpawnPositionTests.cs.meta`

### 수정

- `Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs`

### 삭제

- 없음

---

## 최신 커밋 확인

확인한 최신 커밋:

- SHA: `34fd62208cc7c658cd95be822861596642576e00`
- 현재 커밋 메시지: `a`
- 이전 커밋: `213b48b7cde031c1adbbfa99682d45648624e38d`
- 이전 커밋 메시지: `40일차 : Monster Encounter 데이터 및 방별 확률 배치 규칙 구현`

최신 커밋은 40일차보다 정확히 1개 커밋 앞선 상태이며, 41일차 작업 파일 7개가 포함되어 있다.

변경 내역을 확인한 범위에서는 41일차 목표와 충돌하는 명확한 구조적 문제는 발견되지 않았다.

다만 해당 커밋에는 GitHub CI 상태와 GitHub Actions 실행 기록이 없으므로 실제 Unity 컴파일 성공 및 EditMode Test Runner 통과 여부는 저장소 정보만으로 확인할 수 없다.

---

## 41일차 결과

40일차에서 논리적으로 Monster Encounter가 배정된 방이 실제 탐험 공간의 정지형 테스트 몬스터로 연결되었다.

몬스터는 RoomId, MonsterDefinitionId, GridPosition을 가진 상태로 방 안에 배치되며, 문 주변과 기존 콘텐츠 칸을 피하고 동일 Seed에서 동일 위치를 재현한다.

현재 몬스터는 움직이지 않고 접촉해도 전투가 시작되지 않는다.

다음 42일차에서는 플레이어 이동 완료 후 현재 RoomId와 GridPosition을 몬스터의 논리 좌표와 비교하여, 같은 칸에 도달하면 탐험 입력을 잠그고 Encounter 진입 요청으로 연결하는 단계를 구현한다.
