# Project Delta - 101일차 개발일지

## 작업 개요

101일차는 장비 착용에 요구 조건을 도입했다.

지금까지는 `ItemDefinition.EquipmentSlot`만 맞으면 무조건 장착됐는데, 이번 일차부터는 아이템별로 정의된 공격력·속도·매력·저항 요구치를 만족해야만 장착할 수 있다.

장비 종류 데이터(무기/방어구/가방 템플릿), 저주 장비, 장비 비교 UI는 이번 범위에서 다루지 않는다 - 수정된 개발 일정 기준 102·103일차에서 이어간다.

---

## 1. ItemDefinition - 요구 조건 데이터

`Assets/ProjectDelta/Scripts/Data/ItemDefinition.cs`

`EquipmentRequirements`(StatBlock) 필드를 추가했다. `EquipmentStatBonuses`와 같은 방식으로 `StatBlock`을 재사용해, 공격력/속도/매력/저항 4개 필드만 실제로 검사에 쓰고 나머지는 무시한다. 값이 0인 스탯은 요구 조건이 없는 것으로 취급하므로, 기존 장비 에셋은 아무것도 설정하지 않아도 그대로 동작한다(하위 호환).

---

## 2. StatBlock.Subtract

`Assets/ProjectDelta/Scripts/Domain/PlayerRunState.cs`

기존 `StatBlock.Sum`과 짝을 이루는 `StatBlock.Subtract(minuend, subtrahend)`를 추가했다. 요구 조건 판정에서 "현재 장착 중인 장비의 보너스를 제외한 기준 수치"를 구하는 데 쓰인다.

---

## 3. EquipmentService - 요구 조건 검사

`Assets/ProjectDelta/Scripts/Domain/EquipmentService.cs`

`Equip`에 `requirements`(StatBlock) 선택 인자를 추가하고, 인벤토리를 건드리기 전에 `MeetsRequirements`로 먼저 검사한다.

핵심 규칙은 **"교체 대상 슬롯에 이미 장비가 있으면 그 장비의 보너스를 제외한 기준 수치로 판정한다"**는 것이다. 그렇지 않으면 지금 낀 장비의 힘을 빌려 원래는 착용 불가능한 상위 장비로 계속 갈아탈 수 있게 된다. `MeetsRequirements`는 대상 슬롯의 기존 장비(`EquipmentItemState.EquipmentBonuses`)를 `player.GetFinalStats()`에서 빼서 "맨몸 기준 스탯"을 구한 뒤, 그 값이 요구치 이상인지 확인한다.

검사에 실패하면 새 실패 사유 `EquipmentActionFailureReason.RequirementNotMet`을 반환하고 인벤토리·장비 상태는 전혀 바뀌지 않는다. `player`나 `requirements`가 없으면(테스트 등 UI 밖 호출) 판정할 기준이 없으므로 통과시켜, 97~100일차의 기존 호출부와 테스트는 수정 없이 그대로 동작한다.

---

## 4. 연결 지점

- `EquipmentInteractionService.EquipFromInventory`(`Assets/ProjectDelta/Scripts/Application/EquipmentInteractionService.cs`)가 `definition.EquipmentRequirements`를 `EquipmentService.Equip`에 전달하도록 연결했다.
- `PlayerInventoryHudController`(`Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs`)의 실패 메시지 매핑에 `RequirementNotMet` → "장착 요구 조건을 만족하지 못했습니다." 를 추가했다. Scene/Prefab 변경은 없다.

---

## 5. 테스트

- `EquipmentServiceTests`
  - 요구 조건을 만족하지 못하면 인벤토리·장비 상태 변경 없이 실패하는지.
  - 요구 조건을 만족하면 정상 장착되는지.
  - `player`를 넘기지 않으면 요구 조건 검사를 건너뛰는지(하위 호환).
  - **핵심 케이스**: 약한 무기를 낀 채로는 최종 스탯이 요구치를 넘어 보이지만, 그 무기의 보너스를 제외하면 기준 미달인 상위 무기로는 교체가 막히는지.
- `EquipmentInteractionServiceTests` - `definition.EquipmentRequirements`가 `EquipFromInventory`를 통해 실제로 강제되는지.
- `ItemDefinitionEquipmentTests` - `EquipmentRequirements`가 설정한 값을 그대로 노출하는지, 설정하지 않으면 null이 아닌 빈 `StatBlock`을 반환하는지.
- `PlayerRunStateTests` - `StatBlock.Subtract`가 필드별로 정확히 빼는지.

---

## 6. Unity 에디터에서 확인해야 할 사항

1. Scene/Prefab 변경 사항은 없다 - 로직만 추가됐다.
2. 장비 에셋에서 `Equipment Requirements`(공격력/속도/매력/저항)를 설정해보고, 요구치 미달 시 장착이 거부되며 "장착 요구 조건을 만족하지 못했습니다" 메시지가 뜨는지 확인해달라.
3. 이미 장비를 착용한 상태에서, 그 장비의 보너스 없이는 요구치를 못 넘는 상위 장비로 교체를 시도했을 때 정상적으로 막히는지 플레이 모드에서 확인해달라.
4. 새 EditMode 테스트를 Unity Test Runner에서 실행해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
5. 수정된 개발 일정(101~233일차 수정본)에 따라 다음은 102일차 - 장비 종류 데이터(무기/경갑·중갑·로브/장신구 6역할/가방 4등급 템플릿)이다. 현재 6부위 슬롯 체계이므로 가방은 별도 슬롯이 아닌 인벤토리 확장 아이템으로 조정해서 적용할 예정이다.
