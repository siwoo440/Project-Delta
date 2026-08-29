# Project Delta - 112일차 개발일지

## 작업 개요

111일차에 Combat/Event 방 타입을 실제 플레이 흐름에 연결한 뒤, 이번 일차에는 던전 탐색 쪽 완성도를 크게 높였다.

핵심 작업은 세 가지다.

1. Combat 방을 포함한 일반 방에 상자가 생성되도록 하고, 상자가 문 앞이나 이동 경로를 막지 않도록 배치 규칙을 보강했다.
2. Combat 방에는 최소 한 마리의 몬스터가 남도록 보장 로직을 강화했다.
3. 기존 방 단위 미니맵을 플레이어 중심의 타일 단위 탐색 지도로 확장하고, `M` 전체 맵과 구성요소 표시까지 같은 규칙으로 통합했다.

---

## Part 1. 방 종류와 무관한 상자 생성

`Assets/ProjectDelta/Scripts/Application/RoomChestPlacementService.cs`

상자 생성 대상은 RoomType에 제한하지 않고 Normal, Combat, Trap, Event 방 모두가 후보가 되도록 했다.

시작 방과 계단 방은 제외하며, 동일한 Dungeon Seed와 RoomId에서는 항상 같은 결과가 나오도록 기존 안정 해시 롤을 사용한다.

현재 상자 생성 확률은 방당 30%다.

---

## Part 2. 상자가 길을 막지 않는 배치 규칙

`Assets/ProjectDelta/Scripts/Application/RoomBlockingPlacementService.cs`

기존에는 상자가 문 타일이나 문 바로 앞 칸만 피하면 됐지만, 작은 방이나 통로형 구조에서는 한 칸짜리 상자 하나가 방 내부 이동 경로를 끊을 수 있었다.

이를 막기 위해 상자 후보 타일을 실제 장애물로 가정한 뒤 BFS 방식으로 방 내부 도달 가능 영역을 다시 계산한다.

상자를 놓기 전의 이동 가능 영역과 비교했을 때 단순히 상자 한 칸만 제외되고 나머지 칸의 연결성이 유지되는 후보만 허용한다.

이 과정에서도 기존 몬스터 배치 서비스가 제공하는 문 위치, 문 안쪽 안전 칸, 이미 점유된 콘텐츠 칸 제외 규칙을 함께 사용한다.

---

## Part 3. 런타임 상자와 상호작용 상태 정리

`Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs`  
`Assets/ProjectDelta/Scripts/Presentation/ChestContentMarker.cs`  
`Assets/ProjectDelta/Scripts/Presentation/ChestInteractionController.cs`  
`Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs`

절차 생성된 방에 런타임 상자를 만들고 `RoomContentMarker`와 `ChestContentMarker`를 함께 연결했다.

상자는 실제 GridPosition 한 칸을 점유하기 때문에 활성 상태에서는 플레이어가 그 칸으로 이동할 수 없다.

상자 안의 아이템을 모두 가져가면 `ChestContentMarker`가 GameObject를 비활성화한다. 이에 따라 상호작용 대상에서 사라지고, 플레이어도 해당 칸을 다시 통과할 수 있다.

저장 데이터에서 이미 빈 상자로 복원된 경우에도 같은 방식으로 자동으로 감춰진다.

---

## Part 4. Combat 방 최소 몬스터 보장 강화

`Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs`

일반 Encounter 배치 결과 Combat 방이 비어 있는 경우를 다시 검사하고, 기본 또는 현재 로드된 EncounterDefinition을 후보로 사용해 보장 배치를 시도하도록 확장했다.

일반 점유 규칙 때문에 빈 위치를 찾지 못하면 한 번 더 완화된 후보를 검사하고, 그래도 위치가 없으면 방 내부의 비상 위치를 선택해 최소 몬스터 배치를 우선하도록 했다.

목표는 RoomType.Combat 방이 조우 없이 비어 있는 상태로 남는 경우를 최대한 제거하는 것이다.

---

## Part 5. RoomType 지도 표기

`Assets/ProjectDelta/Scripts/Domain/RoomType.cs`  
`Assets/ProjectDelta/Scripts/Domain/DungeonMinimapSnapshot.cs`

미니맵 스냅샷에서 RoomType을 직접 사용할 수 있도록 방 데이터를 확장했다.

지도에서는 방 종류를 짧은 문자로 표현한다.

- `N` : Normal
- `C` : Combat
- `E` : Event
- `T` : Treasure

현재 방의 종류는 미니맵과 전체 맵 패널에서 별도 라벨로 확인할 수 있다.

---

## Part 6. 플레이어 중심 타일 미니맵

`Assets/ProjectDelta/Scripts/Domain/DungeonMinimapTileRevealService.cs`  
`Assets/ProjectDelta/Scripts/Presentation/DungeonMinimapController.cs`  
`Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs`

기존에는 미니맵 자체가 고정되어 있고 플레이어 아이콘이 지도 위를 이동했다.

이번에는 플레이어 아이콘을 미니맵 중앙에 고정하고, 플레이어가 이동한 반대 방향으로 지도 타일이 스크롤되는 방식으로 변경했다.

GridPosition만 순간적으로 따라가는 것이 아니라 실제 플레이어 Transform 위치를 기준으로 그려 이동 코루틴 중에도 미니맵이 함께 움직일 수 있도록 했다.

플레이어의 현재 칸과 주변 8칸, 즉 기본 3×3 범위를 공개한다.

한 번 공개된 타일은 같은 층을 탐험하는 동안 계속 유지되며, 아직 발견하지 않은 타일은 그리지 않는다.

---

## Part 7. `M` 전체 맵도 동일한 타일 지도 사용

`Assets/ProjectDelta/Scripts/Presentation/DungeonMinimapController.cs`

`M`으로 여는 전체 맵 패널도 우측 상단 미니맵과 같은 발견 타일 데이터를 사용하도록 변경했다.

전체 맵에서도 플레이어는 중앙에 고정되고 타일 지도가 이동한다.

기존 전체 맵의 층 진행도, 탐험률, 계단 거리 정보와 마우스 휠 확대/축소 기능은 유지했다.

상세 타일 지도를 구성할 수 없는 예외 상황에서는 기존 방 단위 그래프 지도로 되돌아가는 fallback도 남겨두었다.

---

## Part 8. 미니맵·전체 맵 구성요소 문자 표시

`Assets/ProjectDelta/Scripts/Domain/DungeonMinimapContentGlyphRules.cs`  
`Assets/ProjectDelta/Scripts/Presentation/DungeonMinimapController.cs`

발견한 타일에 존재하는 활성 `RoomContentMarker`를 미니맵과 전체 맵 양쪽에서 같은 문자 규칙으로 표시한다.

- `S` : Stairs
- `C` : Chest
- `M` : Monster
- `W` : SecretWall
- `N` : NPC
- `A` : AmbientProp

미발견 타일의 콘텐츠는 표시하지 않는다.

상자나 몬스터처럼 GameObject가 비활성화된 콘텐츠도 지도에서 자동으로 사라진다.

각 구성요소는 서로 다른 색상을 사용해 작은 타일 지도에서도 구분하기 쉽게 했다.

---

## Part 9. 테스트

추가 및 보강한 EditMode 테스트:

- `RoomChestPlacementServiceTests`
  - 시작 방/계단 방 제외
  - Combat을 포함한 일반 방 상자 후보 처리
  - 같은 Seed에 대한 결정론적 결과 확인

- `RoomBlockingPlacementServiceTests`
  - 경로를 끊는 한 칸 장애물 배치 거부
  - 이동 가능 영역을 유지하는 후보 허용

- `ChestContentMarkerConfigureTests`
  - 런타임 상자 내용물 설정 확인

- `DungeonMinimapTileRevealServiceTests`
  - 현재 칸 주변 3×3 공개
  - 방 경계를 넘는 좌표 제외
  - 음수 공개 반경 방어

- `DungeonMinimapContentGlyphRulesTests`
  - Stairs/Chest/SecretWall/NpcPoint/AmbientProp/Monster 문자 매핑 확인

- `RoomTypeRollServiceTests`
  - RoomType 관련 변경 이후 기존 분포 규칙 회귀 확인

---

## 검증 및 참고

이번 커밋에는 상자 배치, Combat 몬스터 보장, 플레이어 중심 타일 미니맵, 전체 맵 타일 표시, 지도 구성요소 문자 표시까지 함께 포함되어 있다.

GitHub 커밋 상태에는 별도의 CI 체크가 등록되어 있지 않아 저장소 원격 상태만으로 Unity 컴파일 성공 여부를 확인할 수는 없다.

실제 Unity Editor 컴파일과 Play Mode 확인은 로컬 프로젝트에서 최종 검증한다.

또한 작업 중 사용했던 루트의 BAT/PowerShell/patch 보조 파일은 최종 커밋에 남길 필요가 없으므로 개발일지 amend 시 함께 제거한다.
