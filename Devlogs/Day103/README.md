# Project Delta - 103일차 개발일지

## 작업 개요

103일차는 저주 장비와 장비 비교 UI를 추가해 장비 시스템(97~103일차)을 마무리했다.

기존 장비는 항상 이로운 스탯만 붙었는데, 이번 일차부터 불리한 옵션을 가진 저주 장비를 표현할 수 있고, 인벤토리에서 장비를 선택하면 현재 장착 중인 장비 대비 스탯이 어떻게 바뀌는지 미리 볼 수 있다.

104일차부터는 유물·경제·상자 단계로 넘어간다.

---

## 1. 저주 장비

`Assets/ProjectDelta/Scripts/Data/ItemDefinition.cs`

`IsCursed`(bool) 필드를 추가했다. 저주 효과 자체는 별도 로직이 필요 없었다 - `EquipmentStatBonuses`가 이미 `int` 필드라 음수 값(약점)을 그대로 담을 수 있고, 99~100일차에 만든 최종 스탯 합산·등급 배율 로직도 부호를 신경 쓰지 않고 그대로 동작한다. `IsCursed`는 순수하게 UI가 "이 장비는 불리한 옵션이 있다"고 경고하기 위한 표시용 플래그다.

이 방식을 선택한 이유: `ItemCategory.Cursed`라는 별도 분류가 이미 있지만, 그쪽은 `ItemCategoryRules.GetEquipAvailability`가 `Conditional`이라 지금의 `EquipmentService.Equip`(단순히 `ItemCategory.Equipment`만 허용) 흐름을 못 탄다. 저주 장비도 "장비"로 장착·해제되어야 하므로, 분류는 그대로 `Equipment`를 쓰고 `IsCursed`만 덧붙이는 쪽이 기존 파이프라인을 건드리지 않아 더 간단했다.

---

## 2. 장비 비교 - EquipmentComparisonService

`Assets/ProjectDelta/Scripts/Application/EquipmentComparisonService.cs`

`ComputeBonusDelta(equipment, slotType, candidateBonuses)`가 "후보 장비 보너스 - 현재 장착 중인 장비 보너스"를 계산한다. 101일차에 만든 `StatBlock.Subtract`를 그대로 재사용했다. 해당 슬롯이 비어 있으면 후보 보너스가 그대로 delta가 되고, 음수 옵션(저주)도 가감 없이 그대로 드러난다.

---

## 3. 미리보기와 실제 장착의 굴림 결과 일치 - EquipmentInteractionService

`Assets/ProjectDelta/Scripts/Application/EquipmentInteractionService.cs`

100일차부터 장비는 장착하는 순간 등급·랜덤 옵션이 굴려진다. 그런데 비교 UI는 "장착하기 전" 후보 장비의 스탯을 보여줘야 하므로, 그 미리보기 시점에 한 번 굴려야 한다. 문제는 미리보기용으로 굴린 값과, 실제 장착 버튼을 눌렀을 때 다시 굴린 값이 서로 다르면 화면에 보여준 수치와 실제 결과가 어긋난다는 점이다.

그래서 `EquipFromInventory`에 `EquipmentRollResult`를 직접 받는 오버로드를 추가했다. UI가 선택 시점에 한 번 굴린 결과를 캐시해뒀다가, 장착 확정 시 그 값을 그대로 넘기면 다시 굴리지 않고 그 결과를 그대로 저장한다. 기존 `(definition, player, random)` 시그니처는 내부적으로 새 오버로드를 호출하도록 리팩터링했을 뿐, 동작은 그대로다.

---

## 4. UI 연결

`Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs`

새 패널을 만들지 않고 기존 아이템 설명 텍스트 영역을 확장했다.

- 인벤토리에서 장비 아이템을 선택하면 `UpdatePendingEquipRoll`이 (슬롯 인덱스 + 아이템 ID) 키로 한 번만 굴리고 캐시한다. 같은 슬롯을 다시 눌러도 재굴림하지 않는다.
- 설명 텍스트 아래에 `[등급] 장착 시 스탯 변화`와 함께 0이 아닌 스탯만 `+12`/`-3` 형태로 나열한다. 변화가 없으면 "변화 없음"으로 표시한다.
- `IsCursed`가 true면 "⚠ 저주 장비 — 위 수치에 불리한 옵션이 포함되어 있습니다" 경고를 덧붙인다 - "모든 효과를 공개한다"는 요구를 그대로 반영했다.
- 장착 버튼을 누르면 캐시해둔 굴림 결과를 그대로 `EquipmentInteractionService.EquipFromInventory`에 넘기고, 사용 후 캐시를 비운다. 선택이 바뀌거나(`ClearSelection`) 다른 아이템을 고르면 캐시도 함께 초기화된다.

Scene/Prefab 변경은 없다.

---

## 5. 테스트

- `EquipmentComparisonServiceTests` - 빈 슬롯/기존 장비가 있는 슬롯에서 delta 계산, 저주 장비의 음수 옵션이 그대로 드러나는지, `null` equipment를 안전하게 처리하는지.
- `EquipmentInteractionServiceTests` - 미리 굴린 `EquipmentRollResult`를 넘기면 재굴림 없이 그 값 그대로 저장되는지.
- `ItemDefinitionEquipmentTests` - `IsCursed`가 설정한 값을 노출하고 기본값은 `false`인지.
- `PlayerInventoryHudEquipmentUguiTests` - 굴림 캐시 필드와 비교 텍스트 빌더 메서드가 존재하는지(리플렉션).

---

## 6. Unity 에디터에서 확인해야 할 사항

1. Scene/Prefab 변경 사항은 없다 - 기존 아이템 설명 텍스트 영역을 그대로 확장했다.
2. 저주 장비 에셋(`Is Cursed` 체크 + `Equipment Stat Bonuses`에 음수 값 포함)을 만들어, 인벤토리에서 선택했을 때 스탯 변화와 저주 경고 문구가 함께 표시되는지 확인해달라.
3. 같은 슬롯에 이미 장비가 있는 상태에서 다른 후보 장비를 선택했을 때, 비교 수치(차이값)가 올바른지 확인해달라.
4. 비교 화면에 표시된 등급·수치와, 실제로 장착 버튼을 눌렀을 때 반영되는 수치가 정확히 일치하는지 플레이 모드에서 확인해달라.
5. 새 EditMode 테스트를 Unity Test Runner에서 실행해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
6. 이번 일차로 장비 시스템(97~103일차)이 마무리됐다. 재구성한 개발 일정 기준 다음은 104일차 - 유물·경제·상자 단계다.
