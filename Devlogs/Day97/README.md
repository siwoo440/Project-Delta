# Project Delta - 97일차 개발일지

## 작업 개요

97일차는 장비 시스템의 기초 구조를 구축했다.

기존 아이템 시스템의 `ItemCategory.Equipment`를 기반으로 장비를 인벤토리와 분리된 런타임 상태로 관리하도록 확장하고, 장착·해제·교체 규칙을 구현했다.

이번 일차에서는 장비 UI나 실제 최종 능력치 계산까지 연결하지 않고, 이후 98~101일차 장비 시스템 확장의 기반이 되는 데이터와 도메인 규칙을 우선 완성했다.

- 기준 커밋: `edd14cfd36433f7a9f75fa14d18c47baf69de50d`
- 이전 기준: 96일차 `0ad11722733eaf408b1844159514e52f66bad6bb`

---

## 1. 장비 슬롯 6종 정의

장비 슬롯을 다음 6종으로 구성했다.

- Weapon : 무기
- Helmet : 투구
- ChestArmor : 갑옷
- Leggings : 레깅스
- Boots : 신발
- Accessory : 장신구

기존의 단일 Armor 개념 대신 방어구를 투구, 갑옷, 레깅스, 신발의 4부위로 분리했다.

이를 통해 이후 장비별 능력치, 등급, 랜덤 옵션, UI 표시를 각 부위 단위로 확장할 수 있는 구조를 마련했다.

---

## 2. ItemDefinition 장비 데이터 확장

`ItemDefinition`에 장비 전용 데이터를 추가했다.

추가된 주요 데이터:

- `EquipmentSlot`
- `EquipmentStatBonuses`

`EquipmentSlot`은 해당 아이템이 어느 장비 슬롯에 들어가는지 정의한다.

예:

```text
철검       -> Weapon
강철 투구  -> Helmet
가죽 갑옷  -> ChestArmor
가죽 레깅스 -> Leggings
가죽 신발  -> Boots
은반지     -> Accessory
```

`EquipmentStatBonuses`는 이후 99일차에서 플레이어 최종 능력치에 장비 보너스를 합산하기 위한 데이터로 준비했다.

97일차에서는 값을 저장할 수 있는 구조만 마련하고 실제 전투 능력치에는 아직 적용하지 않는다.

장비 아이템은 인벤토리에서 중첩되지 않도록 `MaxStackSize`를 항상 1로 처리한다.

---

## 3. EquipmentRunState 구현

장착 중인 아이템을 인벤토리와 분리해 관리하기 위해 `EquipmentRunState`를 추가했다.

각 장비 슬롯은 현재 장착 중인 `EquipmentItemState`를 보관한다.

장착 상태에서 관리되는 주요 정보:

- Item ID
- 표시 이름
- 장비 슬롯 종류
- Max Stack Size

또한 슬롯별 장착 아이템 조회와 특정 아이템 장착 여부 확인 기능을 추가했다.

---

## 4. RunContext에 장비 상태 연결

플레이 중 장비 상태를 하나의 런타임 상태로 유지할 수 있도록 `RunContext`에 다음 상태를 추가했다.

```text
RunContext
├ Player
├ Dungeon
├ Inventory
├ Equipment
├ Skills
├ Characters
├ Events
├ Battle
└ ...
```

새로운 Run이 시작되면 빈 `EquipmentRunState`가 함께 생성된다.

97일차에서는 런타임 상태까지만 연결하며, 장비 저장/복원은 101일차 통합 단계에서 처리할 예정이다.

---

## 5. 장착 기능 구현

`EquipmentService.Equip()`을 통해 인벤토리 아이템을 장비 슬롯으로 이동하도록 구현했다.

기본 흐름:

```text
Inventory
[철검]

↓ 장착

Inventory
[빈 슬롯]

Equipment
Weapon = 철검
```

장착 시 다음 조건을 검사한다.

- Inventory와 Equipment 상태가 유효한가
- 실제 장착 가능한 `Equipment` 카테고리인가
- 인벤토리 슬롯에 유효한 아이템이 존재하는가
- 아이템에 정의된 장비 슬롯과 실제 장착 대상 슬롯이 같은가

장착 성공 시 인벤토리에서 수량 1개를 제거하고 해당 장비 슬롯에 저장한다.

---

## 6. 기존 장비 교체

같은 슬롯에 이미 장비가 있는 상태에서 새로운 장비를 장착하면 기존 장비를 인벤토리로 반환하고 새 장비를 장착한다.

예:

```text
Helmet = 낡은 투구
Inventory = 강철 투구

↓ 교체

Helmet = 강철 투구
Inventory = 낡은 투구
```

기존 장비를 인벤토리에 반환할 수 없는 경우 교체를 실패시키고 아이템 손실이 발생하지 않도록 처리했다.

---

## 7. 장비 해제

`EquipmentService.Unequip()`을 추가해 현재 장착 중인 아이템을 인벤토리로 되돌릴 수 있도록 했다.

기본 흐름:

```text
Equipment
Boots = 가죽 신발

↓ 해제

Equipment
Boots = Empty

Inventory
[가죽 신발]
```

인벤토리에 빈 공간이 없을 경우 해제를 실패시키고 기존 장착 상태를 유지한다.

---

## 8. 잘못된 장비 슬롯 장착 방지

초기 구현에서는 외부에서 전달한 슬롯 값을 그대로 사용했기 때문에 잘못된 호출이 발생하면 투구를 신발 슬롯 등에 장착할 가능성이 있었다.

이를 수정해 장비에 정의된 슬롯과 실제 장착 대상 슬롯을 별도로 전달하도록 변경했다.

```text
definedSlotType = 아이템 자체에 정의된 슬롯
targetSlotType  = 실제 장착하려는 슬롯
```

두 값이 다르면 `WrongEquipmentSlot`로 즉시 실패한다.

예:

```text
강철 투구
Defined Slot = Helmet

↓ Boots 슬롯 장착 시도

WrongEquipmentSlot
장착 실패
인벤토리 변화 없음
```

이 검증은 인벤토리 아이템을 제거하기 전에 실행되므로 잘못된 장착 시도에서도 아이템 상태가 변경되지 않는다.

---

## 9. 테스트 추가

장비 시스템의 기반 규칙을 검증하기 위해 EditMode 테스트를 추가했다.

주요 검증 항목:

- 장비 슬롯 6종이 정확하게 존재하는지 확인
- Run 시작 시 빈 Equipment 상태 생성
- 정상 장착 시 인벤토리에서 장비 슬롯으로 이동
- Equipment가 아닌 아이템 장착 거부
- 기존 장비 교체 후 이전 장비 인벤토리 반환
- 장비 해제 후 인벤토리 복귀
- 인벤토리가 가득 찬 경우 해제 실패 및 장비 유지
- 아이템 정의 슬롯과 대상 슬롯이 다른 경우 장착 거부
- 잘못된 슬롯 장착 실패 시 인벤토리와 장비 상태가 변경되지 않음
- 장비 아이템 Max Stack Size 1 적용
- ItemDefinition 장비 슬롯 및 기본 능력치 데이터 노출

---

## 최종 상태

97일차 종료 기준으로 장비 시스템의 도메인 기반이 구축되었다.

```text
Inventory
   ↕
EquipmentService
   ↕
EquipmentRunState
   ↕
RunContext
```

지원하는 장비 슬롯:

```text
Weapon
Helmet
ChestArmor
Leggings
Boots
Accessory
```

지원하는 기본 동작:

```text
장착
해제
교체
비장비 아이템 거부
잘못된 부위 장착 거부
인벤토리 가득 참 보호
```

다음 98일차에서는 이 기반 시스템을 인벤토리 UI와 연결해 실제 플레이 중 장비를 선택하고 장착·해제할 수 있는 장비 UI를 구현한다.
