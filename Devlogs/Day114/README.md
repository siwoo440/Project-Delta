# Project Delta - 114일차 개발일지

## 작업 개요

113일차(제가 직접 본 적 없는, 이전 세션에서 압축된 부분)에서 만든 NPC 기반 위에 실제 서비스를 연결하는 날이다. NPCDefinition·NpcRelationshipState·F키 상호작용까지는 이미 있었지만 "서비스" 버튼을 누르면 "실제 기능은 114일차에서 확장합니다"라는 자리표시자만 나오는 상태였다. 오늘은 그 자리를 상인·치료사·지도사·보물사냥꾼 네 가지 서비스로 채웠다.

작업 전 113일차 코드를 먼저 전부 읽었다 - `NpcDefinition`에는 역할(Role) 필드가 따로 없고 `NpcServiceType` 플래그(Trade/Healing/MapInformation/RelicTrade/RelicResearch/ExplorationInformation)로만 역할이 구분되게 설계돼 있었다. 이 구조를 바꾸지 않고 그대로 활용했다.

---

## Part 1. 테스트 NPC를 4명으로 확장

`Assets/ProjectDelta/Scripts/Presentation/NpcRuntimeBootstrapController.cs`

기존엔 상인 1명만 매 층 고정 스폰됐다. 역할별 설정(`NpcRoleConfig`) 배열로 상인(Trade)·치료사(Healing)·지도사(MapInformation|ExplorationInformation)·보물사냥꾼(RelicTrade|RelicResearch) 4명을 서로 다른 방에 자동 배치하도록 확장했다. 방/칸 선택 로직(`RoomBlockingPlacementService`)은 그대로 재사용하고, NPC마다 시각 구분을 위해 역할별 색상만 추가했다.

---

## Part 2. 서비스 로직 (Domain)

`Assets/ProjectDelta/Scripts/Domain/NpcHealingService.cs`, `NpcRelicService.cs`, `NpcServiceRunState.cs`, `RelicRunState.cs`

- `NpcHealingService.Heal` - 골드를 받고 체력·마나·정력을 전부 회복한다. 이미 가득 찬 경우와 골드 부족을 구분해서 실패 처리한다. `ShopService`와 같은 패턴으로 `GoldService`를 그대로 재사용했다.
- `NpcRelicService` - 저주 유물 제거(`RemoveCursedRelic`, 골드 소비)와 유물 희생(`SacrificeRelic`, 골드 획득) 두 가지를 처리한다. `RelicRunState`에는 유물을 빼는 방법이 아예 없었다(획득 즉시 패시브 적용 전제로 설계되어 해제 개념이 없었다) - `AddRelic`과 같은 이유로 `internal RemoveRelic`을 추가하고, 같은 Domain 어셈블리인 `NpcRelicService`를 통해서만 호출되게 했다.
- `NpcServiceRunState` - NPC 한 명의 서비스 상태를 담는 그릇. 지금은 상인의 재고(`ShopRunState`)만 들어있다. NPC GameObject가 같은 층 안에서는 파괴되지 않는다는 걸 확인했기 때문에, 이 상태를 `NpcContentMarker`에 붙여두는 것만으로 "같은 층 재방문 시 재고·가격 유지" 요구사항이 별도 저장 로직 없이 저절로 충족된다. 층 이동/불러오기 이후의 영속화는 115일차 이후 범위로 남겨뒀다.

---

## Part 3. 상점 재고

`Assets/ProjectDelta/Scripts/Presentation/NpcShopStockBuilder.cs`

`RuntimeItemDefinitionLookup`이 쓰던 것과 같은 방식(`Resources.FindObjectsOfTypeAll<ItemDefinition>()`)으로 실제 존재하는 아이템 자산을 모아 재고를 만든다. 지금 프로젝트에 진짜 아이템 자산이 `ITEM_DAY80_TEST_DROP` 하나뿐이라 재고가 그만큼만 나오지만, 나중에 아이템이 늘어나면 코드를 바꾸지 않아도 자동으로 반영된다.

---

## Part 4. 서비스 화면

`Assets/ProjectDelta/Scripts/Presentation/NpcInteractionController.cs`, `Assets/ProjectDelta/Scripts/Application/NpcInteractionService.cs`

"서비스" 버튼을 누르면 상점/회복/정보/유물 정리 중 해당 NPC가 가진 서비스만 메뉴로 보여주고, 각 화면에서 실제 거래가 이뤄지게 확장했다.

- 상점: 재고 목록에서 구매 버튼 → `ShopService.Buy` 재사용.
- 회복: 현재 체력/마나/정력 표시 후 회복 버튼 → `NpcHealingService.Heal`.
- 정보(지도사): 실시간 미니맵 발견 상태를 직접 조작하는 대신, 지금까지 방문한 방들의 종류(전투/함정/이벤트/일반) 통계만 보여주는 것으로 범위를 좁혔다 - 미니맵 내부 상태를 Presentation 레이어에서 직접 건드리는 건 오늘 범위에서 위험도가 커서 뺐다.
- 유물 정리(보물사냥꾼): 보유 유물 목록에서 저주 유물은 "저주 제거", 나머지는 "희생" 버튼을 보여준다.

`NpcInteractionService.Resolve`의 Service 분기 메시지도 "114일차에서 확장합니다" 자리표시자에서 실제 안내 문구로 바꿨다.

---

## 오류 수정

빌드 중 `DungeonRunState`가 `ProjectDelta.Data`와 `ProjectDelta.Domain` 양쪽에 있어 모호하다는 CS0104 오류가 났다. `DungeonFloorController.cs`에서 이미 쓰던 것과 같은 방식으로 `using DungeonRunState = ProjectDelta.Domain.DungeonRunState;` 별칭을 추가해 해결했다.

---

## 테스트

- `NpcHealingServiceTests` - 정상 회복(골드 소비), 골드 부족, 이미 가득 참, null 방어.
- `NpcRelicServiceTests` - 저주 제거(성공/저주 아님/골드 부족/유물 없음), 희생(성공/유물 없음).

NPC 4명 자동 배치, 상점·회복·정보·유물 정리 화면은 Unity 에디터가 없는 환경이라 실제 플레이로 확인하지 못했다. 사용자가 에디터에서 빌드 오류를 잡아 직접 확인했다.
