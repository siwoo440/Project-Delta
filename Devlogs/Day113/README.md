# Project Delta - 113일차 개발일지

## 작업 개요

112일차에 전투방 상자·몬스터 보장과 플레이어 중심 타일 지도를 정리한 뒤, 113일차에는 NPC 시스템의 공통 기반을 구축했다.

이번 일차의 목표는 상점이나 치료 같은 개별 NPC 기능을 완성하는 것이 아니라, 이후 NPC 콘텐츠가 공통 구조 위에서 동작할 수 있도록 데이터·관계 상태·상호작용 결과·런타임 배치 흐름을 연결하는 것이다.

핵심 작업은 다음과 같다.

1. `NpcDefinition`과 NPC 서비스·적대 규칙을 추가했다.
2. `DataRepository`와 `DataValidator`가 NPC 정의를 다룰 수 있도록 확장했다.
3. NPC 호감도·조우 횟수·적대 상태를 정적 Definition과 분리한 런타임 관계 상태를 만들었다.
4. NPC 메뉴 명령과 결과를 공통 구조로 정의하고, 상호작용 종료 후 탐험 복귀 흐름을 연결했다.
5. 테스트 NPC를 절차 생성 방에 자동 배치하고 `F` 키 상호작용과 이동 차단을 연결했다.
6. 관련 EditMode 테스트를 추가했다.

---

## Part 1. NPC 정적 데이터 구조

`Assets/ProjectDelta/Scripts/Data/NpcDefinition.cs`  
`Assets/ProjectDelta/Scripts/Data/NpcServiceType.cs`  
`Assets/ProjectDelta/Scripts/Data/NpcHostilityMode.cs`  
`Assets/ProjectDelta/Scripts/Data/DefinitionBase.cs`

NPC의 변하지 않는 기본 데이터는 `NpcDefinition`에서 관리하도록 했다.

주요 데이터는 다음과 같다.

- 고유 NPC ID
- 표시 이름
- 제공 서비스
- 적대 가능 여부
- 초기 호감도
- 관계 영구 유지 여부
- 적대 전환 시 사용할 기본 전투 능력치

NPC 서비스는 하나의 NPC가 여러 기능을 가질 수 있도록 Flags 방식의 `NpcServiceType`으로 정의했다.

현재 서비스 종류는 다음과 같다.

- `Trade`
- `Healing`
- `MapInformation`
- `RelicTrade`
- `RelicResearch`
- `ExplorationInformation`

NPC 적대 규칙은 `NpcHostilityMode`로 분리했다.

- `Never`
- `CanBecomeHostile`
- `StartsHostile`

`DefinitionBase`에는 런타임 테스트용 Definition도 동일한 ID 규칙을 사용할 수 있도록 런타임 ID 설정 통로를 추가했다.

---

## Part 2. DataRepository와 데이터 검증 연결

`Assets/ProjectDelta/Scripts/Data/DataRepository.cs`  
`Assets/ProjectDelta/Scripts/Data/DataValidator.cs`

기존 `DataRepository`는 Monster와 Item Definition만 관리하고 있었다.

113일차에는 다음 NPC 테이블과 조회 API를 추가했다.

```text
DataRepository
├─ Monsters
├─ Items
└─ Npcs
```

NPC는 `DefinitionTable<NpcDefinition>`을 사용하며 고유 ID로 조회할 수 있다.

`DataValidator`도 NPC Definition의 빈 ID와 중복 ID를 기존 Monster/Item과 동일한 규칙으로 검사하도록 확장했다.

---

## Part 3. NPC 관계 런타임 상태

`Assets/ProjectDelta/Scripts/Domain/NpcRelationshipState.cs`  
`Assets/ProjectDelta/Scripts/Domain/NpcRelationshipStage.cs`  
`Assets/ProjectDelta/Scripts/Domain/NpcRelationshipRules.cs`  
`Assets/ProjectDelta/Scripts/Domain/NpcRelationshipRegistry.cs`

NPC의 현재 관계 값은 `NpcDefinition`에 저장하지 않고 별도의 런타임 상태로 분리했다.

`NpcRelationshipState`는 다음 정보를 관리한다.

- NPC ID
- 호감도
- 조우 횟수
- 현재 적대 여부

호감도는 0~100 범위로 제한하고, 현재 값에 따라 관계 단계를 계산한다.

- `0~33` : Neutral
- `34~66` : Interest
- `67~84` : Trust
- `85~99` : Special
- `100` : EndingAvailable

`NpcRelationshipRegistry`는 동일한 고유 NPC ID가 다시 등장했을 때 같은 런타임 관계 상태를 재사용한다.

현재 단계에서는 플레이 세션 내 공유 상태이며, 영구 세이브 데이터 연결은 이후 관계 시스템 확장 단계에서 진행한다.

---

## Part 4. NPC 상호작용 공통 결과 구조

`Assets/ProjectDelta/Scripts/Domain/NpcInteractionCommand.cs`  
`Assets/ProjectDelta/Scripts/Domain/NpcInteractionResult.cs`  
`Assets/ProjectDelta/Scripts/Domain/NpcInteractionResultType.cs`  
`Assets/ProjectDelta/Scripts/Application/NpcInteractionService.cs`

NPC UI가 직접 게임 상태를 변경하지 않도록 상호작용 명령과 결과를 Domain/Application 계층으로 분리했다.

현재 명령은 다음 세 가지다.

- `Talk`
- `Service`
- `Leave`

결과 종류는 다음과 같다.

- `ContinueInteraction`
- `OpenService`
- `ReturnToExploration`
- `StartBattle`

113일차에서는 대화, 서비스 진입 준비, 떠나기 결과를 처리한다.

`Leave` 결과는 `ReturnToExploration`으로 반환되어 UI를 닫고 탐험 입력을 다시 활성화하는 공통 복귀 흐름으로 이어진다.

`StartBattle` 결과 타입은 이후 적대 NPC 전투 연결을 위한 확장 지점으로 준비했다.

---

## Part 5. 테스트 NPC 런타임 배치

`Assets/ProjectDelta/Scripts/Presentation/NpcRuntimeBootstrapController.cs`  
`Assets/ProjectDelta/Scripts/Presentation/NpcContentMarker.cs`

별도 NPC 프리팹이나 ScriptableObject 에셋을 수동으로 연결하지 않아도 기본 동작을 확인할 수 있도록 테스트 NPC를 런타임에서 생성한다.

현재 테스트 NPC는 다음 정보를 사용한다.

```text
ID : NPC_MERCHANT_TEST
표시명 : 상인
서비스 : Trade
적대 규칙 : CanBecomeHostile
```

NPC는 `RoomContentType.NpcPoint`를 가진 `RoomContentMarker`와 `NpcContentMarker`를 함께 사용한다.

배치 시 기존 `RoomBlockingPlacementService`를 이용해 문 앞과 기존 콘텐츠 칸을 피하고, NPC 한 칸이 방 내부 이동 경로를 끊지 않는 위치를 선택한다.

가능하면 일반적인 탐험 방에 배치하며 시작 방과 계단 방은 피한다.

112일차 지도 시스템은 이미 `NpcPoint`를 `N`으로 표시하므로 별도 지도 구조 변경 없이 발견된 NPC 위치가 미니맵과 전체 맵에 표시된다.

---

## Part 6. NPC 상호작용과 탐험 복귀

`Assets/ProjectDelta/Scripts/Presentation/NpcInteractionController.cs`

플레이어 정면 한 칸에 NPC가 있을 때 `F` 키로 상호작용할 수 있도록 했다.

상호작용이 열리면 플레이어 이동 입력을 잠그고 NPC 메뉴를 표시한다.

현재 메뉴 흐름은 다음과 같다.

```text
NPC 접근
→ F 상호작용
→ 대화 / 서비스 / 떠나기
→ 결과 처리
→ 떠나기 또는 ESC
→ 패널 종료
→ 탐험 입력 복구
```

대화는 현재 관계 단계를 포함한 기본 결과 메시지를 표시한다.

서비스는 NPC가 가진 `NpcServiceType`을 확인하고 서비스 연결 준비 상태를 반환한다. 실제 상점·치료 등 개별 서비스 기능은 다음 개발 단계에서 확장한다.

---

## Part 7. 씬 자동 설치

`Assets/ProjectDelta/Scripts/Presentation/NpcRuntimeInstaller.cs`

기존 DungeonScene을 직접 수정하지 않아도 NPC 기반 기능을 확인할 수 있도록 런타임 설치기를 추가했다.

씬이 로드되고 플레이어 컨트롤러가 존재하면 필요한 NPC 런타임 구성요소를 자동으로 연결한다.

이를 통해 이번 단계에서는 별도의 Inspector Component 연결을 최소화했다.

---

## Part 8. NPC의 그리드 점유

`Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs`

NPC는 상자와 마찬가지로 실제 그리드 한 칸을 점유한다.

활성 `NpcPoint`가 있는 칸은 플레이어 이동 대상에서 제외해 캐릭터가 NPC와 같은 칸을 통과하지 않도록 했다.

이동 차단은 특정 NPC 구현이 아니라 방의 활성 콘텐츠 마커를 기준으로 처리한다.

---

## Part 9. 테스트

113일차에 추가한 EditMode 테스트는 다음과 같다.

### `NpcDefinitionRepositoryTests`

- 런타임 NPC Definition 구성
- NPC ID 등록
- `DataRepository.Npcs` 조회
- 서비스/적대 데이터 확인

### `NpcRelationshipRulesTests`

- 호감도 단계 경계값 확인
- 0~100 범위 처리 확인
- NPC 관계 상태 변경 규칙 확인

### `NpcInteractionServiceTests`

- 대화 결과 확인
- 서비스 보유/미보유 NPC 결과 확인
- 떠나기 → `ReturnToExploration` 결과 확인

---

## 현재 범위와 다음 단계

113일차에서는 NPC 공통 기반까지만 구현했다.

아직 포함하지 않은 기능은 다음 단계에서 확장한다.

- 실제 상점 구매/판매
- 치료 서비스
- 지도/정보 서비스
- NPC 적대 전환
- NPC와 실제 Battle 연결
- 호감도 변화 이벤트
- NPC 관계 영구 저장
- 관계 단계별 특수 이벤트 및 엔딩 연결

113일차의 완료 기준은 NPC가 정적 Definition, 런타임 관계 상태, 방 배치, 지도 표시, 상호작용, 탐험 복귀 흐름 안에서 하나의 공통 구조로 동작할 수 있는 기반을 만드는 것이다.

---

## 검증 참고

113일차 원격 커밋은 112일차 커밋보다 1개 앞선 상태이며, NPC 관련 소스와 테스트 40개 파일의 추가·수정으로 구성되어 있다.

GitHub에는 해당 커밋의 별도 CI 상태가 등록되어 있지 않기 때문에 Unity Editor 컴파일 및 Play Mode 성공 여부는 원격 저장소만으로 확인할 수 없다.

실제 컴파일/실행 오류가 발생할 경우 Unity Console 결과를 기준으로 다음 수정 단계에서 보완한다.
