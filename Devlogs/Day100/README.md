# Project Delta - 100일차 개발일지

## 작업 개요

100일차는 장비에 등급(Rarity)과 랜덤 옵션을 도입했다.

지금까지 `ItemDefinition.EquipmentStatBonuses`는 아이템마다 고정된 값이었는데, 이번 일차부터는 같은 아이템이라도 장착할 때마다 등급이 판정되고 그 등급에 맞춰 실제 스탯이 배율 + 랜덤 변동폭으로 계산된다.

세이브/로드 최종 통합(101일차)은 이번 범위에서 다루지 않는다.

---

## 1. EquipmentRarity와 등급 규칙

`Assets/ProjectDelta/Scripts/Domain/EquipmentRarity.cs`

일반/고급/희귀/영웅/전설 5등급을 정의하고, `EquipmentRarityRules`에 등급별 규칙을 모았다.

- `GetDisplayName` - UI에 표시할 한글 등급명.
- `GetStatMultiplier` - 등급이 높을수록 커지는 스탯 배율(1.0 ~ 2.0).
- `GetDropWeight` - 등급 판정에 쓰이는 가중치(100 ~ 3). 일반이 가장 잘 나오고 전설이 가장 희귀하다.

`ItemCategoryRules`와 같은 위치·같은 패턴(정적 규칙 클래스)으로 만들어 기존 코드 스타일과 맞췄다.

---

## 2. EquipmentRollService - 등급 판정과 랜덤 옵션

`Assets/ProjectDelta/Scripts/Application/EquipmentRollService.cs`

장착 시점에 다음을 계산하는 서비스를 추가했다.

1. `RollRarity` - 가중치 누적합 방식으로 등급 하나를 뽑는다.
2. `Roll` - 뽑힌 등급의 배율을 기본 스탯에 곱하고, 스탯마다 ±10% 랜덤 옵션 변동폭을 추가로 적용해 최종 `StatBlock`을 만든다. 원래 값이 있던 스탯은 배율·변동폭 때문에 0으로 깎이지 않도록 최소 1을 보장한다.

무작위성은 이 서비스에만 존재한다. `EquipmentService`(Domain)는 97~99일차와 마찬가지로 순수하게 결정적으로 남겨뒀다 - 이미 굴려진 등급/보너스 값을 받아 저장만 한다. 테스트에서 `System.Random`을 주입해 시드 고정 검증이 가능한 것도 이 분리 덕분이다.

---

## 3. 장착 시점에 등급이 저장되도록 연결

- `EquipmentItemState`(`Assets/ProjectDelta/Scripts/Domain/EquipmentRunState.cs`)에 `Rarity` 필드를 추가했다. 99일차에 만든 "장착 시점 스탯 스냅샷" 패턴을 그대로 확장한 것이다.
- `EquipmentService.Equip`에 `rarity` 선택 인자를 추가했다. 인자를 생략하면 `Common`으로 처리되어 97~99일차의 기존 호출부와 테스트는 수정 없이 그대로 동작한다.
- `EquipmentInteractionService.EquipFromInventory`가 `EquipmentRollService.Roll()`을 호출해 등급과 실제 보너스를 굴린 뒤 `EquipmentService.Equip`에 전달하도록 연결했다. 인벤토리 UI에서 장착 버튼을 누르는 순간 등급이 정해진다.

### 설계상 트레이드오프

일반적인 RPG는 "드랍 시점"에 등급/랜덤 옵션을 확정하지만, 이번 일차는 "장착 시점"에 굴리는 방식을 선택했다.

인벤토리 슬롯(`InventorySlotState`)이 현재 `ItemId`/수량만 들고 있고 개체별 데이터를 저장하는 구조가 아니어서, 드랍 시점에 확정하려면 `TryAdd`/`TryMoveOrSwap`/`RestoreSlot` 등 인벤토리 핵심 API 전체에 개체별 페이로드를 추가하는 큰 작업이 필요했다. 대신 이미 있는 "장착 시점 스냅샷" 구조를 재사용해 안전하게 구현했다.

그 결과 같은 아이템이라도 벗었다가 다시 장착하면 등급이 새로 굴려진다. 지금은 전투 보상(`BattleRewardState`)이 아직 장비를 드랍하지 않으므로 문제가 되지 않지만, 이후 "드랍 시 등급 확정"이 필요해지면 인벤토리 슬롯에 개체별 데이터를 얹는 작업이 별도로 필요하다.

---

## 4. UI - 장비 패널에 등급 표시

`Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs`

장비 패널의 슬롯 이름 텍스트를 `[희귀] 강철 투구`처럼 등급을 앞에 붙여 표시하도록 바꿨다. Scene/Prefab 변경은 없다 - 98일차에 만든 UI 구조를 그대로 사용한다.

---

## 5. 테스트

- `EquipmentRarityRulesTests` - 등급별 표시명, 배율/가중치가 등급이 높을수록 커지는(또는 작아지는) 단조 관계인지 확인.
- `EquipmentRollServiceTests` - `System.Random`의 내부 구현에 의존하지 않도록 정확한 값 대신 통계적/구조적 불변식을 확인.
  - null definition은 항상 Common + 빈 보너스.
  - 다회 시행에서 항상 정의된 등급만 나오는지, 가중치 차이가 큰 만큼 일반이 전설보다 확실히 많이 나오는지.
  - 다회 시행에서 스탯이 등급 배율 ± 10% 범위 안에 머무르는지, 기본값이 0인 스탯은 항상 0을 유지하는지.
- `EquipmentServiceTests` - `rarity` 인자를 생략하면 `Common`으로 저장되는지(하위 호환), 전달한 등급이 그대로 저장되는지.
- `EquipmentInteractionServiceTests` - 시드를 고정한 뒤 `EquipmentRollService.Roll`로 직접 구한 기대값과 `EquipFromInventory`가 실제로 저장한 등급/보너스가 일치하는지. 99일차에 작성했던 "정확한 값 비교" 테스트 1건(`EquipFromInventory_WithPlayer_AddsBonusesToFinalStats`)이 랜덤화와 충돌해 시드 기반 비교로 함께 수정했다.

---

## 6. Unity 에디터에서 확인해야 할 사항

1. Scene/Prefab 변경 사항은 없다 - 98일차 UI를 그대로 사용하며, 로직만 추가됐다.
2. 플레이 모드에서 다음을 확인해달라.
   - 같은 아이템을 여러 번 장착/해제하면서 등급 표시(`[일반]`, `[희귀]` 등)와 스탯 증가폭이 매번 달라지는지.
   - 등급이 높을수록(희귀/영웅/전설) 최종 스탯이 확실히 더 크게 붙는지.
   - 기존 사용/이동/버리기/장착/해제 흐름이 여전히 정상 동작하는지(회귀 확인).
3. 새 EditMode 테스트(`EquipmentRarityRulesTests`, `EquipmentRollServiceTests`) 및 갱신된 `EquipmentServiceTests`/`EquipmentInteractionServiceTests`를 Unity Test Runner에서 실행해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
4. 세이브/로드 통합(101일차)은 이번에 다루지 않았다 - 로드 후 재장착 흐름에서 등급/보너스가 어떻게 유지되어야 하는지는 101일차에 함께 설계해야 한다.
