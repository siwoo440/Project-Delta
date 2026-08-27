# Project Delta - 99일차 개발일지

## 작업 개요

99일차는 97~98일차에 만든 장비 장착/해제 흐름에 실제 능력치 계산을 연결했다.

그동안 `ItemDefinition.EquipmentStatBonuses`는 저장만 되고 플레이어 최종 스탯에는 반영되지 않았는데, 이번 일차에서 장착 중인 6부위 보너스를 합산해 `PlayerRunState.GetFinalStats()`에 더하도록 만들었다.

등급/랜덤 옵션(100일차)과 Save/Load 최종 통합(101일차)은 이번 범위에서 다루지 않는다.

---

## 1. EquipmentItemState에 스탯 스냅샷 저장

`Assets/ProjectDelta/Scripts/Domain/EquipmentRunState.cs`

`EquipmentItemState`가 장착 시점의 `StatBlock` 보너스를 함께 들고 있도록 확장했다.

Domain 계층이 Data 계층(`ItemDefinition`)을 직접 참조하지 않는 기존 원칙을 지키기 위해, `ItemDefinition` 자체를 참조하는 대신 값만 복사해서 저장한다.

`EquipmentRunState`에는 `GetTotalBonuses()`를 추가해 6부위에 장착된 아이템의 보너스를 모두 더한 `StatBlock`을 반환한다.

---

## 2. PlayerRunState 최종 스탯에 장비 보너스 합산

`Assets/ProjectDelta/Scripts/Domain/PlayerRunState.cs`

`EquipmentBonuses` 필드를 추가하고 `GetFinalStats()`가 `BaseStats + AllocatedStats + TemporaryStats + EquipmentBonuses`를 합산하도록 바꿨다.

이 지점 하나만 고쳤기 때문에 전투 보상, 아이템 사용, 탐험 상태이상, HUD 등 기존에 `GetFinalStats()`를 쓰던 모든 코드가 별도 수정 없이 장비 보너스를 자동으로 반영한다.

장비 해제 등으로 최대치가 줄었을 때 현재 체력/마나/기력이 최대치를 넘지 않도록 `ClampCurrentResourcesToFinalStats()`도 추가했다. 최대치가 늘어난다고 현재 자원을 자동으로 채워주지는 않는다(기존 `CreateDefault()`만 완전 회복 처리).

---

## 3. EquipmentService가 장착/해제 시점에 스탯을 동기화

`Assets/ProjectDelta/Scripts/Domain/EquipmentService.cs`

`Equip`/`Unequip`에 `equipmentBonuses`, `player`(둘 다 선택 인자, 기본값 `null`)를 추가했다.

- 장착/해제가 성공하면 `player.EquipmentBonuses`를 `equipment.GetTotalBonuses()`로 갱신하고 `player.ClampCurrentResourcesToFinalStats()`를 호출한다.
- 두 인자 모두 선택값이라 97일차에 작성된 기존 호출부와 테스트는 수정 없이 그대로 컴파일된다.

`Assets/ProjectDelta/Scripts/Application/EquipmentInteractionService.cs`와 `Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs`는 `definition.EquipmentStatBonuses`와 `RunContext.Current.Player`를 실제로 전달하도록 갱신했다.

---

## 4. 테스트

- `EquipmentServiceTests`
  - 보너스와 `player`를 함께 넘겼을 때 `EquipmentRunState.GetTotalBonuses()`와 `PlayerRunState.GetFinalStats()`가 갱신되는지 확인.
  - `player` 인자 없이 호출한 기존 케이스가 여전히 성공하는지(하위 호환) 확인.
- `EquipmentInteractionServiceTests`
  - 장착 시 최종 스탯 증가, 해제 시 원복, 최대 체력 감소 시 현재 체력이 clamp되는지 확인.
- `PlayerRunStateTests`
  - `EquipmentBonuses`가 `GetFinalStats()`에 반영되는지.
  - `ClampCurrentResourcesToFinalStats()`가 최대치 초과분만 줄이고, 최대치 이하인 현재 자원은 그대로 두는지(자동 회복 아님) 확인.

---

## 5. Unity 에디터에서 확인해야 할 사항

1. Scene/Prefab 변경 사항은 없다 - 98일차에 연결한 UI 그대로 사용하며, 도메인 계산 로직만 바뀌었다.
2. 플레이 모드에서 다음을 확인해달라.
   - 장비를 장착하면 인벤토리 HUD에 표시되는 최종 스탯(공격력/방어력 등)이 즉시 올라가는지.
   - 같은 슬롯에 장비를 교체했을 때 이전 장비 보너스는 빠지고 새 장비 보너스만 남는지.
   - 최대 체력을 늘려주는 장비를 벗었을 때 현재 체력이 새 최대치를 넘지 않게 줄어드는지(초과분만 줄고, 최대치 이하였다면 그대로인지).
   - 인벤토리가 가득 차 해제가 실패하는 경우 스탯도 변경되지 않고 그대로인지.
3. 새 EditMode 테스트(`EquipmentServiceTests`, `EquipmentInteractionServiceTests`, `PlayerRunStateTests`에 추가된 케이스)를 Unity Test Runner에서 실행해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
4. 세이브/로드 시 `PlayerRunState.EquipmentBonuses`를 다시 계산해 주는 연결은 이번 일차에서 다루지 않았다 - 저장 데이터 구조 통합은 101일차 범위다. 로드 직후 장비 화면을 열고 닫거나 장착 상태를 갱신하는 흐름이 있다면 스탯이 비어 있을 수 있으니 유의해달라.
