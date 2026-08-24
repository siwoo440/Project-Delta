# Project Delta - 42일차 개발일지

## 개발 목표

41일차에서 방 내부 유효 GridPosition에 배치한 정지형 테스트 몬스터와 플레이어가 같은 방의 같은 칸에 도달했을 때 Encounter 진입을 요청하는 탐험 접촉 흐름을 구현한다.

이번 일차에서는 실제 전투 계산이나 정식 Encounter 상태 머신을 만들지 않고, 다음 단계까지만 구현한다.

- 플레이어 이동 완료 시점 감지
- Player / Monster RoomId 비교
- Player / Monster GridPosition 비교
- 동일 방·동일 칸 접촉 시 테스트 Encounter 시작
- Encounter 중 탐험 이동 입력 잠금
- 중복 Encounter 시작 방지
- OnGUI 테스트 Encounter 표시
- 테스트 종료 시 몬스터 비활성화 및 탐험 복귀
- DungeonScene의 Player에 Encounter Controller 연결
- EditMode 테스트 추가

---

## 구현 내용

### 1. ExplorationEncounterSession 추가

탐험 접촉 Encounter의 최소 상태를 관리하는 `ExplorationEncounterSession`을 추가했다.

현재 세션은 다음 정보만 관리한다.

```text
IsActive
MonsterDefinitionId
```

Encounter 시작 조건은 다음과 같다.

```text
Player.RoomId == Monster.RoomId
그리고
Player.GridPosition == Monster.GridPosition
```

둘 중 하나라도 다르면 Encounter를 시작하지 않는다.

이미 Encounter가 진행 중인 경우 `TryBegin()`은 다시 시작하지 않고 false를 반환한다.

### 2. 같은 방·같은 GridPosition 기반 접촉 판정

42일차 접촉 판정은 Collider 충돌을 기준으로 사용하지 않는다.

41일차에서 `ExplorationMonsterMarker`가 보관하는 다음 정보와 플레이어 런타임 상태를 비교한다.

```text
Player
- CurrentRoomId
- CurrentGridPosition

Monster
- RoomId
- GridPosition
- MonsterDefinitionId
```

따라서 서로 다른 방에서 같은 GridPosition 값을 가지고 있어도 Encounter가 발생하지 않는다.

### 3. 플레이어 이동 완료 시점 감지

`ExplorationMonsterEncounterController`는 `PlayerGridMovementController.IsMoving` 상태를 관찰한다.

```text
IsMoving = true
↓
실제 플레이어 이동 보간
↓
IsMoving = false
↓
현재 위치 Encounter 검사
```

논리 GridPosition이 먼저 갱신된 뒤 실제 Transform이 움직이는 기존 구조를 유지하면서, 화면상 이동이 끝난 뒤 Encounter가 시작되도록 구성했다.

### 4. ExplorationMonsterEncounterController 추가

탐험 화면에서 몬스터 접촉을 감지하고 테스트 Encounter를 표시하는 `ExplorationMonsterEncounterController`를 추가했다.

주요 역할은 다음과 같다.

- PlayerGridMovementController 참조
- PlayerLookController 참조
- DungeonFloorController 참조
- 현재 방의 SpawnedMonster 조회
- 플레이어와 몬스터의 RoomId / GridPosition 비교
- Encounter 시작 및 종료 처리
- 테스트 OnGUI 표시

필요한 참조가 비어 있는 경우 런타임에서 자동 검색하도록 구성했다.

### 5. DungeonFloorController.SpawnedMonsters 재사용

41일차에서 추가한 `DungeonFloorController.SpawnedMonsters`를 사용하여 플레이어가 현재 있는 RoomId의 몬스터만 조회한다.

현재 구조는 방당 Monster Encounter 최대 1개이므로 다음 방식으로 조회한다.

```text
Player.CurrentRoomId
↓
SpawnedMonsters.TryGetValue(RoomId)
↓
현재 방 테스트 몬스터
```

비활성화된 몬스터는 Encounter 대상으로 취급하지 않는다.

### 6. Encounter 시작 시 탐험 입력 잠금

Encounter 시작에 성공하면 다음 처리를 수행한다.

```text
movementController.IsInputLocked = true
```

이를 통해 Encounter가 열린 동안 WASD 탐험 이동을 차단한다.

또한 `PlayerLookController.SetCursorFreeForUi(true)`를 호출하여 OnGUI 버튼을 클릭할 수 있도록 마우스 커서를 UI 상태로 전환한다.

### 7. 테스트 Encounter OnGUI

42일차에서는 정식 전투 UI 대신 간단한 OnGUI 테스트 창을 사용한다.

표시 내용:

```text
ENCOUNTER

Monster : MON_TEST

전투 인카운터가 시작되었습니다.

[테스트 종료]
```

이 UI는 이후 정식 Encounter 화면과 상태 머신이 구현되면 교체하기 위한 임시 확인용 화면이다.

### 8. 테스트 Encounter 종료 처리

`[테스트 종료]` 버튼을 누르면 현재 Encounter를 종료한다.

처리 순서:

```text
현재 몬스터 비활성화
↓
activeMonster 해제
↓
ExplorationEncounterSession.Complete()
↓
Player 이동 잠금 해제
↓
마우스 커서 탐험 상태 복원
↓
탐험 재개
```

테스트 몬스터를 비활성화하는 이유는 플레이어와 몬스터가 같은 칸에 남은 상태에서 동일 Encounter가 반복 발생하는 것을 막기 위함이다.

실제 전투 결과 시스템이 구현되면 승리·회피·패배 결과에 맞는 처리로 교체한다.

### 9. DungeonScene Player 연결

`DungeonScene`의 Player 오브젝트에 `ExplorationMonsterEncounterController`를 추가했다.

씬에 연결된 참조:

```text
Movement Controller
Look Controller
Floor Controller
```

따라서 Play 시 별도 런타임 오브젝트 생성 없이 Player가 직접 몬스터 접촉 Encounter를 감지한다.

### 10. EditMode 테스트 추가

`ExplorationEncounterSessionTests`를 추가했다.

테스트 항목은 다음 6가지다.

1. 같은 RoomId + 같은 GridPosition이면 Encounter 시작
2. 같은 방이지만 다른 GridPosition이면 시작하지 않음
3. 다른 방이지만 같은 GridPosition이면 시작하지 않음
4. 이미 Active 상태이면 중복 Encounter 시작 차단
5. Complete 이후 상태 초기화 및 다음 Encounter 허용
6. RoomId 또는 MonsterDefinitionId가 없으면 Encounter 시작 차단

---

## 42일차 동작 흐름

```text
플레이어 한 칸 이동
↓
이동 보간 완료
↓
현재 RoomId 확인
↓
현재 방 SpawnedMonster 조회
↓
Player GridPosition 확인
↓
Monster GridPosition 확인
↓
같은 방 + 같은 칸
↓
ExplorationEncounterSession 시작
↓
Player 이동 입력 잠금
↓
OnGUI ENCOUNTER 표시
↓
[테스트 종료]
↓
몬스터 비활성화
↓
세션 종료
↓
탐험 입력 복구
```

---

## 변경 파일

### 생성

- `Assets/ProjectDelta/Scripts/Application/ExplorationEncounterSession.cs`
- `Assets/ProjectDelta/Scripts/Application/ExplorationEncounterSession.cs.meta`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/ExplorationEncounterSessionTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/ExplorationEncounterSessionTests.cs.meta`

### 수정

- `Assets/ProjectDelta/Scenes/DungeonScene.unity`

### 삭제

- 없음

---

## 최신 커밋 확인

확인한 최신 커밋:

- SHA: `344a1a32a9bcea0a6356411e38d38a8ba0cbbab3`
- 현재 커밋 메시지: `a`
- 이전 커밋: `a753710bc14b02af27314d488e5c8b7e7766f00d`
- 이전 커밋 메시지: `41일차 : 유효 GridPosition 기반 정지형 테스트 몬스터 배치 구현`

최신 커밋은 41일차보다 정확히 1개 커밋 앞선 상태이며, 42일차 작업 파일 7개가 변경되었다.

GitHub 변경 내역을 확인한 범위에서는 42일차 목표와 충돌하는 명확한 구조적 문제는 확인되지 않았다.

다만 해당 커밋에는 GitHub CI 상태와 GitHub Actions 실행 기록이 없기 때문에 실제 Unity 컴파일 성공 및 EditMode Test Runner 통과 여부는 저장소 정보만으로 확인할 수 없다.

---

## 42일차 결과

41일차의 정지형 테스트 몬스터와 플레이어가 같은 방·같은 GridPosition에서 만났을 때 테스트 Encounter로 진입하는 탐험 접촉 흐름이 연결되었다.

현재 Encounter는 임시 OnGUI 화면과 최소 세션 상태만 사용하며 실제 전투 계산은 포함하지 않는다.

다음 43일차에서는 현재의 단순 Active 상태를 정식 Encounter 상태 머신으로 확장하여 `Idle → Starting → Active → Resolving → Finished` 흐름과 인카운터 시작·종료 책임을 분리하는 단계로 진행한다.
