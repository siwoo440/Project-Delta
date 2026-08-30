# Project Delta - 115일차 개발일지

## 작업 개요

113~114일차에서 만든 NPC 기반·서비스 위에 관계 영속화와 새 상호작용(선물·구조·공격), 그리고 완전히 다른 화면 레이아웃을 얹는 날이다.

핵심은 세 가지다.

1. NPC 관계(호감도·조우 횟수·적대 여부)가 층 이동·불러오기 후에도 유지되게 저장 구조를 연결한다.
2. 선물·구조·공격 세 가지 상호작용을 추가하고, 공격은 실제 적대 전환 + 전투 진입까지 이어지게 한다.
3. NPC 대화 화면을 "상단 캐릭터 일러스트 + 하단 좌 대화창 + 하단 우 선택지 버튼" 구성으로 다시 짠다.

---

## Part 1. NPC 관계 영구 저장

`Assets/ProjectDelta/Scripts/Domain/NpcRelationshipRegistry.cs`, `NpcRelationshipState.cs`, `RunContext.cs`, `Assets/ProjectDelta/Scripts/Data/RunData.cs`, `DungeonSaveMapper.cs`

`NpcRelationshipRegistry`는 113일차 코드 주석에 이미 "115일차에서 영구 저장으로 옮긴다"고 적혀 있던 대로, 세션 메모리(static Dictionary)에만 있어서 저장/불러오기하면 관계가 전부 사라졌다.

- `RunData`에 `List<NpcRunState> NpcStates`를 추가했다 - 기존 `EventFlags`(109일차)와 같은 자리, 같은 패턴이다. `DungeonRunState.Rooms`처럼 방마다 지연 복원하는 구조가 아니라, `EventRunState.RestoreFrom`처럼 저장 시점에 레지스트리 전체를 한 번에 캡처하고 불러오기 시점(`ApplyBasics`)에 한 번에 복원한다 - NPC 관계는 방이 아니라 세션 전체에 걸친 상태라 이 편이 더 맞는 패턴이었다.
- `NpcRelationshipState`에 조우 횟수·구조 여부까지 그대로 복원하는 생성자 오버로드를 추가했다(기존 3-인자 생성자는 그대로 두고 위임하도록 해서 기존 호출부는 안 건드렸다).
- `RunContext` 생성자에서 레지스트리를 먼저 비우게 했다 - static 저장소라 새 회차를 시작할 때 이전 회차의 NPC 관계가 새어 들어갈 수 있기 때문이다. 이어하기라면 바로 뒤에 `ApplyBasics`가 저장된 값으로 다시 채운다.

---

## Part 2. 선물·구조·공격

`Assets/ProjectDelta/Scripts/Domain/NpcInteractionCommand.cs`, `Assets/ProjectDelta/Scripts/Application/NpcInteractionService.cs`, `Assets/ProjectDelta/Scripts/Presentation/NpcInteractionController.cs`, `DungeonFloorController.cs`

- **선물** - 인벤토리에서 아이템 하나를 골라 건네면 소모되고 호감도 +10.
- **구조** - NPC 한 명당 한 번만 가능, 호감도 +20. "위험에 처한 NPC"라는 전제 조건 자체가 프로젝트에 아직 없어서, 조건 없이 1회만 가능한 호의 행동으로 단순화했다.
- **공격** - `SetHostile(true)` 후 실제 조우/전투 파이프라인으로 들어간다. 몬스터가 방에 접촉하면 자동으로 전투가 시작되는 기존 구조(`ExplorationMonsterEncounterController.TryBeginEncounterAtCurrentPosition`)를 그대로 재사용했다 - NPC의 GameObject에 `ExplorationMonsterMarker`를 붙이고 `DungeonFloorController`에 새로 추가한 `RegisterRuntimeMonsterMarker`로 "이 방의 조우 대상"으로 등록한 뒤, 기존 진입 메서드를 그대로 호출한다.

  **알려진 한계**: 전투에 실제로 쓰이는 적 능력치는 `NpcDefinition`의 고유 스탯(MaxHp/Attack 등)이 아니라 기존 몬스터 조회 실패 시 fallback으로 쓰는 테스트 몬스터 스탯이다. `NpcDefinition`을 진짜 `MonsterDefinition`처럼 전투 시스템(1800줄 규모의 `ExplorationMonsterEncounterController`, 보상·승패 판정까지 얽혀 있음)에 직접 등록하는 건 오늘 범위에서 위험도가 너무 커서 뺐다 - NPC 전용 능력치를 전투에 실제로 반영하는 건 다음 단계 과제로 남긴다.

`NpcInteractionResultType.StartBattle`은 113일차부터 이미 정의만 되어 있고 아무도 쓰지 않던 값이었는데, 오늘 처음 실제로 연결했다.

---

## Part 3. 화면 레이아웃 교체

`Assets/ProjectDelta/Scripts/Presentation/NpcInteractionController.cs`

기존엔 화면 중앙에 정보+버튼이 전부 세로로 쌓인 단일 패널이었다. 요청받은 구성대로 다시 짰다.

- **상단 중앙**: 캐릭터 일러스트 자리. 실제 일러스트 자산이 없어서 역할별 색상 박스 + NPC 이름으로 대신했다 - 나중에 진짜 일러스트가 생기면 `DrawCharacterIllustration` 메서드만 바꾸면 된다.
- **하단 좌측**: 직사각형 대화창. 이름·호감도·관계 단계·적대 여부 요약과 상태 메시지를 보여준다.
- **하단 우측**: 직사각형 선택지 버튼 목록. 메인 메뉴(대화/서비스/선물/구조/공격/떠나기)와 서비스 하위 화면(상점/회복/정보/유물 정리/선물) 모두 같은 자리를 재사용한다.

---

## 테스트

- `NpcRelationshipRegistryPersistenceTests` - 복원(Restore)이 저장된 값을 그대로 되살리는지, `All`이 등록된 상태를 반영하는지, `Clear`가 실제로 비우는지.
- `NpcInteractionServiceRelationshipTests` - 선물이 호감도를 올리는지, 구조가 1회만 적용되는지(두 번째 시도는 무시), 공격이 전투 불가 NPC에서는 적대 전환되지 않는지.

전투 진입, 새 화면 레이아웃은 Unity 에디터가 없는 환경이라 실제 플레이로 확인하지 못했다. 사용자가 에디터에서 직접 확인했다.
