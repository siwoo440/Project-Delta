# Project Delta - 98일차 개발일지

## 작업 개요

98일차는 97일차에 만든 장비 도메인 규칙(`EquipmentService`, `EquipmentRunState`)을 인벤토리 ↔ 장비 UI에 연결했다.

96일차에 정리된 `PlayerInventoryHudController`의 Scene 기반 uGUI 구조를 그대로 확장해, 기존 사용/이동/버리기 기능을 건드리지 않고 장착/해제 흐름만 추가했다.

이번 일차에서는 실제 장비 능력치 합산(99일차), 등급/랜덤 옵션(100일차), Save/Load 최종 통합(101일차)은 다루지 않는다.

---

## 1. EquipmentInteractionService

`Assets/ProjectDelta/Scripts/Application/EquipmentInteractionService.cs`를 추가했다.

`InventoryInteractionService`와 같은 위치(Application 계층)에서, UI가 `EquipmentService`를 직접 호출할 때 필요한 6개 인자를 매번 조립하지 않도록 감쌌다.

- `EquipFromInventory(inventory, equipment, inventorySlotIndex, definition)`
  - `definition.EquipmentSlot`을 defined/target 슬롯 양쪽에 그대로 사용한다. 98일차 UI는 슬롯을 직접 고르지 않고 아이템 자신의 슬롯에만 장착하기 때문이다.
  - `definition`이 null이거나 장비가 아니면 인벤토리를 건드리지 않고 `ItemNotEquipment`로 실패한다.
- `Unequip(inventory, equipment, slotType)`
  - `EquipmentService.Unequip`을 그대로 위임한다.

실제 슬롯 일치 검사, 인벤토리 가득 참 처리 등 규칙 판단은 여전히 97일차 `EquipmentService`가 전담한다.

---

## 2. PlayerInventoryHudController 확장

기존 인벤토리 슬롯/선택/사용/이동/버리기 로직은 그대로 두고 다음을 추가했다.

**인벤토리 → 장비 장착**

- 선택한 아이템이 `ItemCategory.Equipment`면 `장착` 버튼(`equipButton`)이 나타난다.
- 클릭 시 `EquipmentInteractionService.EquipFromInventory`를 호출해 해당 아이템의 `EquipmentSlot`에 장착한다.
- 이미 같은 슬롯에 장비가 있으면 `EquipmentService`가 기존 장비를 인벤토리로 되돌리고 교체한다.
- 인벤토리가 가득 차 교체 대상을 되돌릴 수 없으면 장착이 실패하고 인벤토리 상태는 그대로 유지된다(97일차 규칙 그대로).

**장비 패널 (해제)**

- `equipmentPanel` 아래 `equipmentSlotButtons` / `equipmentSlotIcons` / `equipmentSlotNameTexts` 6칸을 `EquipmentSlotType` 순서(Weapon, Helmet, ChestArmor, Leggings, Boots, Accessory)로 고정 배치했다.
- 각 슬롯 버튼을 누르면 `OnEquipmentSlotClicked`가 `EquipmentInteractionService.Unequip`을 호출해 인벤토리로 돌려보낸다.
- 인벤토리가 가득 차 있으면 해제가 실패하고 장비는 계속 장착 상태로 남는다.
- 빈 슬롯은 "비어있음"으로 표시하고 버튼을 비활성화한다.

**갱신 신호(96일차 리팩터 유지)**

- `CalculateRefreshSignature`에 6개 장비 슬롯의 장착 아이템 ID를 포함시켜, 장비 상태가 바뀔 때만 `Update()` 기반 자동 갱신이 다시 그리도록 했다. 매 프레임 전체 UI를 다시 그리는 방식으로 되돌아가지 않았다.

---

## 3. 테스트

구현 전에 아래 EditMode 테스트를 먼저 추가했다.

- `EquipmentInteractionServiceTests`
  - 정의된 슬롯으로 장착이 되는지, 장비가 아닌 아이템/`null` definition이 인벤토리를 건드리지 않고 실패하는지, 빈 슬롯 해제가 `EquipmentSlotEmpty`로 실패하는지 검증.
- `PlayerInventoryHudEquipmentUguiTests`
  - `PlayerInventoryHudController`가 `OnGUI`를 쓰지 않고, 장비 관련 필드가 `[SerializeField]`로 노출되어 있으며, 장착/해제 핸들러 메서드가 존재하는지 리플렉션으로 검증(기존 `ChestInteractionUguiTests` 패턴을 따름).

`EquipmentServiceTests`(97일차)는 수정하지 않았다 - 도메인 규칙 자체는 변경하지 않았기 때문이다.

---

## 4. Unity 에디터에서 확인해야 할 사항

코드만으로는 완성되지 않는 부분이 있어 에디터 작업이 필요하다.

1. `PlayerInventoryHudController`가 붙은 Scene 오브젝트에서 새 필드를 인스펙터로 연결해야 한다.
   - `equipButton` - 기존 `useButton`/`moveButton`/`discardButton` 옆에 배치할 "장착" 버튼.
   - `equipmentPanel` - 장비 6부위를 보여줄 패널 GameObject.
   - `equipmentSlotButtons[0..5]`, `equipmentSlotIcons[0..5]`, `equipmentSlotNameTexts[0..5]` - 반드시 `EquipmentSlotType` 순서(Weapon, Helmet, ChestArmor, Leggings, Boots, Accessory)에 맞춰 배열 인덱스를 채워야 한다. 순서가 어긋나면 잘못된 부위가 해제된다.
2. 장비 패널 UI(배경, 레이아웃)는 아직 만들지 않았다 - 인벤토리 슬롯과 비슷한 형태로 6칸을 배치하는 실제 Prefab/Scene 작업이 필요하다.
3. 플레이 모드에서 다음을 직접 확인해달라.
   - 인벤토리에서 장비 아이템 선택 → `장착` 버튼 노출 → 클릭 시 인벤토리에서 사라지고 장비 패널에 표시되는지.
   - 같은 슬롯에 다른 장비를 장착했을 때 기존 장비가 인벤토리로 돌아오는지.
   - 인벤토리를 가득 채운 상태에서 해제를 시도하면 실패 메시지가 뜨고 장비가 유지되는지.
   - 기존 사용/이동/버리기 버튼이 여전히 정상 동작하는지(회귀 확인).
4. 새로 추가된 EditMode 테스트(`EquipmentInteractionServiceTests`, `PlayerInventoryHudEquipmentUguiTests`)를 Unity Test Runner에서 실행해 통과하는지 확인해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
