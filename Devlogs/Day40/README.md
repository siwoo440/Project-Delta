# Project Delta - 40일차 개발일지

## 개발 목표

39일차까지 완성한 절차 생성 던전·지도·저장 구조 위에 인카운터 배치의 기초 구조를 추가한다.

이번 일차에서는 실제 몬스터 GameObject를 생성하지 않고, 생성된 각 방에 Monster Encounter가 존재하는지 결정하는 논리 배치 단계까지만 구현한다.

- `RoomContentType.Monster` 추가
- `EncounterDefinition` 데이터 구조 추가
- 일반 방별 Monster Encounter 출현 확률 판정
- 방당 0~1개 Encounter 제한
- Entry / Stairs / Special Candidate 방 제외
- 동일 Seed에서 동일 배치가 재현되는 결정적 난수 규칙
- 새 던전 생성과 이어하기 복원 흐름에 Encounter 배치 연결
- 테스트용 Monster / Encounter 데이터 에셋 구성
- EditMode 테스트 추가

---

## 구현 내용

### 1. RoomContentType을 Domain으로 이동

기존 `RoomContentMarker.cs` 내부에 있던 `RoomContentType`을 Domain 계층의 독립 파일로 분리했다.

기존 Unity 직렬화 값이 바뀌지 않도록 기존 순서를 명시적으로 유지했다.

```text
Stairs = 0
Chest = 1
SecretWall = 2
NpcPoint = 3
AmbientProp = 4
Monster = 5
```

이를 통해 기존 계단·상자·NPC 마커의 저장된 enum 값은 유지하면서 새로운 `Monster` 콘텐츠 타입을 추가했다.

### 2. EncounterDefinition 추가

새로운 `EncounterDefinition` ScriptableObject를 추가했다.

현재 정의하는 데이터는 다음과 같다.

- 연결할 `MonsterDefinition`
- 일반 방 출현 확률 `RoomSpawnChance`
- 사용 여부 `Enabled`

배치 가능한 Encounter는 Encounter ID와 Monster ID가 존재하고, MonsterDefinition이 연결되어 있으며 Enabled 상태여야 한다.

### 3. DungeonEncounterLayout 추가

한 층에서 어떤 방에 어떤 Encounter가 배정되었는지 기록하는 논리 결과 구조를 추가했다.

`RoomEncounterAssignment`은 다음 정보를 보관한다.

- RoomId
- RoomContentType
- EncounterDefinitionId
- MonsterDefinitionId

RoomId를 Dictionary 키로 관리하여 같은 방에 Encounter가 중복 배정되지 않도록 했다.

### 4. RoomEncounterPlacementService 구현

절차 생성이 끝난 `GeneratedDungeon`을 기준으로 일반 방에 Monster Encounter를 배정한다.

배치 대상에서 다음 방은 제외한다.

- Entry Room
- Stairs Room
- Special Room Candidate
- 호출 시 명시적으로 전달한 제외 RoomId

남은 일반 방은 EncounterDefinition의 `RoomSpawnChance`를 이용해 각각 독립적으로 배치 여부를 판정한다.

### 5. 동일 Seed 배치 재현

`string.GetHashCode()` 대신 직접 만든 결정적 해시 계산을 사용한다.

배치 난수는 다음 세 값을 기반으로 계산한다.

```text
Dungeon Seed
+ RoomId
+ EncounterDefinitionId
```

따라서 같은 Seed, 같은 RoomId, 같은 EncounterDefinition 조합에서는 다시 계산해도 동일한 Monster Encounter 배치 결과를 얻도록 구성했다.

이 구조는 39일차의 Seed·레이아웃 저장/복원 시스템과 연결되어 이어하기에서도 동일한 Encounter 배치 결과를 다시 만들 수 있다.

### 6. DungeonFloorController 연결

`DungeonFloorController`에 40일차 Encounter 배치 흐름을 연결했다.

새 던전을 생성하면 다음 순서로 처리한다.

```text
GeneratedDungeon 생성
↓
RoomView 실제 배치
↓
문 연결
↓
계단 배치
↓
GeneratedDungeon / Seed를 DungeonRunState에 등록
↓
RoomEncounterPlacementService 실행
↓
DungeonEncounterLayout 보관
```

이어하기로 저장된 던전을 복원할 때도 저장된 Seed와 동일한 GeneratedDungeon을 사용해 Encounter 배치를 다시 계산한다.

`CurrentEncounterLayout`을 공개하여 41일차의 실제 몬스터 GridPosition 선정과 GameObject 스폰 단계에서 그대로 사용할 수 있도록 했다.

층이 제거되거나 새 층으로 교체될 때는 기존 Encounter Layout도 함께 초기화한다.

### 7. 테스트 데이터 에셋 생성

테스트용 데이터 에셋을 추가했다.

#### MonsterDefinition

```text
Id: MON_TEST
Display Name: Test Monster
```

#### EncounterDefinition

```text
Id: ENC_TEST_MONSTER
Monster: MON_TEST
Room Spawn Chance: 0.35
Enabled: true
```

DungeonScene의 `DungeonFloorController`에도 해당 EncounterDefinition을 연결했다.

### 8. EditMode 테스트 추가

`RoomEncounterPlacementTests`를 추가했다.

테스트 항목은 다음 6가지다.

1. Entry / Stairs 제외 및 방당 Encounter 1개 제한
2. Special Room Candidate 제외
3. 명시적으로 제외한 RoomId 제외
4. 같은 Seed와 같은 방 구조에서 동일 배치 재현
5. 출현 확률 0 또는 Disabled Encounter의 배치 차단
6. null / 잘못된 EncounterDefinition의 배치 차단

GitHub에는 해당 커밋에 연결된 CI 또는 GitHub Actions 실행 기록이 없기 때문에 Unity 컴파일과 EditMode Test Runner의 실제 통과 여부는 저장소 기록만으로 확인할 수 없다.

---

## 변경 파일

40일차 커밋은 39일차 커밋보다 1개 앞선 상태이며 총 20개 파일이 변경되었다.

### 생성

- `Assets/ProjectDelta/Data/Monster.meta`
- `Assets/ProjectDelta/Data/Monster/Encounter Definition.meta`
- `Assets/ProjectDelta/Data/Monster/Encounter Definition/EncounterDefinition.asset`
- `Assets/ProjectDelta/Data/Monster/Encounter Definition/EncounterDefinition.asset.meta`
- `Assets/ProjectDelta/Data/Monster/Monster Definition.meta`
- `Assets/ProjectDelta/Data/Monster/Monster Definition/MonsterDefinition.asset`
- `Assets/ProjectDelta/Data/Monster/Monster Definition/MonsterDefinition.asset.meta`
- `Assets/ProjectDelta/Scripts/Application/RoomEncounterPlacementService.cs`
- `Assets/ProjectDelta/Scripts/Application/RoomEncounterPlacementService.cs.meta`
- `Assets/ProjectDelta/Scripts/Data/EncounterDefinition.cs`
- `Assets/ProjectDelta/Scripts/Data/EncounterDefinition.cs.meta`
- `Assets/ProjectDelta/Scripts/Domain/DungeonEncounterLayout.cs`
- `Assets/ProjectDelta/Scripts/Domain/DungeonEncounterLayout.cs.meta`
- `Assets/ProjectDelta/Scripts/Domain/RoomContentType.cs`
- `Assets/ProjectDelta/Scripts/Domain/RoomContentType.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/RoomEncounterPlacementTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/RoomEncounterPlacementTests.cs.meta`

### 수정

- `Assets/ProjectDelta/Scenes/DungeonScene.unity`
- `Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/RoomContentMarker.cs`

### 삭제

- 없음

---

## 최신 커밋 확인

확인한 최신 커밋:

- SHA: `b5a83f08b0395c384b8409db8839dd3d34ccaa76`
- 현재 커밋 메시지: `40`
- 이전 커밋: `a0bdc98ca039553a191bd0b7427911c55b45e27b`
- 이전 커밋 메시지: `39일차 : 지도 방문 정보·Seed·레이아웃 저장/복원 및 이어하기 통합`

최신 커밋은 39일차보다 정확히 1개 커밋 앞선 상태이며, 40일차 구현 코드와 테스트 데이터 에셋, DungeonScene의 EncounterDefinition 연결이 포함되어 있다.

검토한 변경 내역에서는 40일차 목표와 충돌하는 명확한 문제는 확인되지 않았다.

다만 GitHub CI 및 Workflow 실행 기록이 없으므로 실제 Unity 컴파일 성공과 EditMode 테스트 통과는 로컬 Unity Test Runner에서 별도로 확인해야 한다.

---

## 40일차 결과

던전 생성 이후 각 일반 방에 Monster Encounter가 존재하는지를 결정하는 논리 배치 기반이 마련되었다.

현재 단계에서는 Monster GameObject를 실제로 생성하지 않는다.

다음 41일차에서는 `DungeonFloorController.CurrentEncounterLayout`을 사용하여 Monster Encounter가 배정된 방을 찾고, 방 내부의 유효한 GridPosition을 선정하여 정지형 테스트 몬스터를 실제로 배치하는 단계로 확장한다.
