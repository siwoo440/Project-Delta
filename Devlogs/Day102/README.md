# Project Delta - 102일차 개발일지

## 작업 개요

102일차는 장비 종류를 구분하는 분류 데이터를 추가했다.

무기, 방어구(경갑·중갑·로브), 장신구(6역할), 가방(4등급)을 각각 데이터로 구분할 수 있게 하고, 특히 가방은 6부위 장비 슬롯 체계와 별개로 인벤토리를 확장하는 소모형 아이템으로 연결했다.

저주 장비와 장비 비교 UI는 이번 범위에서 다루지 않는다 - 수정된 개발 일정 기준 103일차에서 이어간다.

---

## 1. 분류 태그 3종

`Assets/ProjectDelta/Scripts/Domain/ArmorWeightClass.cs`, `AccessoryRole.cs`, `BagTier.cs`

- `ArmorWeightClass` - 경갑/중갑/로브. ChestArmor·Leggings·Boots에만 의미가 있다.
- `AccessoryRole` - 전투형·회피형·탐험형·자원형·매력형·저항형 6종. Accessory에만 의미가 있다.
- `BagTier` - 소형/중형/대형/초대형 4등급. `BagTierRules.GetSlotBonus`가 기획서의 "+2~+8 확장" 범위를 4단계(2/4/6/8)에 고르게 배분한다.

세 태그 모두 지금은 순수 분류 메타데이터이며, 스탯 계산에는 아직 관여하지 않는다. `ItemCategoryRules`·`EquipmentRarityRules`와 같은 패턴(정적 규칙 클래스 + `GetDisplayName`)으로 만들어 기존 코드 스타일과 맞췄다.

**확인이 필요한 가정**: 기획서에는 장신구 6역할이 "전투형·회피형·탐험형 등"이라고만 적혀 있고 나머지 3개 이름이 명시되어 있지 않아, 자원형·매력형·저항형으로 채워뒀다. 정확한 명칭이 정해지면 `AccessoryRole` enum 값과 표시명만 교체하면 된다.

---

## 2. ItemDefinition에 분류 필드 연결

`Assets/ProjectDelta/Scripts/Data/ItemDefinition.cs`

`armorWeightClass`, `accessoryRole`, `bagTier` 세 필드를 추가했다. 기존 에셋은 아무것도 설정하지 않아도 전부 `None`으로 유지되므로 하위 호환에 문제가 없다.

---

## 3. 가방 - 6부위 장비 슬롯과 분리된 인벤토리 확장 아이템

`Assets/ProjectDelta/Scripts/Application/BagExpansionService.cs`

가방은 무기·투구·갑옷·레깅스·신발·장신구 6부위 체계에 낄 자리가 없다. 그래서 "장착"이 아니라 **사용하는 즉시 인벤토리 슬롯을 영구적으로 넓혀주고 소모되는 아이템**으로 설계했다.

- `BagExpansionService.ApplyAndConsume(inventory, slotIndex, definition)` - `definition.BagTier`가 `None`이 아니면 인벤토리에서 아이템 1개를 소모하고, 기존 `InventoryRunState.BagSlotBonus`에 등급별 확장치를 더해 `SetCapacityBonuses`로 반영한다.
- 여러 개의 가방을 얻으면 확장치가 그대로 누적된다.
- 가방이 아닌 아이템, 빈 슬롯, 인벤토리 없음 등은 상태 변경 없이 실패 사유(`BagExpansionFailureReason`)를 반환한다.

이미 있던 97~101일차의 `EquipmentService`/`EquipmentInteractionService`(장착·해제·요구 조건)와는 완전히 분리된 별도 파이프라인이다 - 가방은 장착/해제 개념이 없기 때문이다.

---

## 4. UI 연결

`Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs`

가방 아이템을 선택하고 기존 "사용" 버튼을 누르면, 93일차에 만든 `ItemUseService`(HP/MP/기력 회복 판정) 경로를 타지 않고 `BagExpansionService`로 바로 분기되어 슬롯이 늘어난다. 가방은 `ItemCategoryRules.CanUse`가 이미 허용하는 `ExplorationTool` 분류를 그대로 쓰므로 버튼 자체는 기존 로직으로 노출되며, 사용 가능 여부 미리보기(`ItemUseService.Preview*`)만 가방일 때 건너뛰도록 예외 처리했다. Scene/Prefab 변경은 없다.

---

## 5. 테스트

- `EquipmentClassificationRulesTests` - 세 분류의 표시명이 정확한지, `BagTierRules.GetSlotBonus`가 2→4→6→8로 단조 증가하는지.
- `BagExpansionServiceTests` - 가방 사용 시 슬롯이 늘고 아이템이 소모되는지, 여러 가방을 사용하면 확장치가 누적되는지, 가방이 아닌 아이템/빈 슬롯/인벤토리 없음이 상태 변경 없이 실패하는지.
- `ItemDefinitionEquipmentTests` - 세 분류 필드가 설정한 값을 그대로 노출하는지, 설정하지 않으면 전부 `None`을 유지하는지.

---

## 6. Unity 에디터에서 확인해야 할 사항

1. Scene/Prefab 변경 사항은 없다 - 기존 "사용" 버튼을 그대로 재사용한다.
2. 방어구/장신구 에셋에서 `Armor Weight Class`/`Accessory Role`을 설정해보고 인스펙터에 정상 노출되는지 확인해달라.
3. 가방 아이템(`Bag Tier` 설정)을 인벤토리에서 선택 후 "사용"을 눌러 슬롯이 실제로 늘어나는지, 아이템이 소모되는지 플레이 모드에서 확인해달라. 서로 다른 등급의 가방을 연속으로 사용해 확장치가 누적되는지도 확인해달라.
4. 새 EditMode 테스트를 Unity Test Runner에서 실행해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
5. 장신구 6역할의 정확한 명칭이 기획서에 별도로 있다면 알려달라 - `AccessoryRole.cs`의 자원형·매력형·저항형은 추정치다.
6. 수정된 개발 일정(101~233일차 수정본)에 따라 다음은 103일차 - 저주 장비 + 장비 비교 UI다.
