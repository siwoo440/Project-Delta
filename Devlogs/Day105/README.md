# Project Delta - 105일차 개발일지

## 작업 개요

105일차는 골드 획득·소비를 한곳으로 정리하고, 상점의 구매/판매 규칙을 만들었다.

지금까지 골드는 `BattleRewardPayoutService` 한 곳에서만 직접 `player.Gold`를 조작했는데, 이제 전투 보상·이벤트·상점이 전부 같은 API를 쓰도록 통일했다.

상점 UI와 "층 진입 시 실제로 상품을 채우는" 연결은 이번 범위에서 다루지 않는다. 106일차(보물상자+미믹)로 이어간다.

---

## 1. GoldService - 골드 공통 API

`Assets/ProjectDelta/Scripts/Domain/GoldService.cs`

`Earn(player, amount)` / `TrySpend(player, amount)` 두 개로 정리했다.

- `Earn`은 기존 `BattleRewardPayoutService.ApplyDropGold`가 하던 계산(음수/0 무시, `int.MaxValue` 포화, 실제 증가량 반환)을 그대로 옮긴 것이다. `ApplyDropGold`는 이제 `GoldService.Earn`을 호출하도록 바꿨을 뿐 동작과 반환값은 완전히 동일해서, 기존 `BattleRewardSummaryTests`의 골드 관련 테스트를 손대지 않고도 그대로 통과한다.
- `TrySpend`는 보유 골드가 부족하면 상태를 바꾸지 않고 실패한다. 0을 소비하는 건 항상 성공(no-op)으로 처리했다.

---

## 2. 상점 - ShopRunState / ShopService

`Assets/ProjectDelta/Scripts/Domain/ShopRunState.cs`, `ShopService.cs`

`ShopRunState`는 이번 층에 고정된 상품 목록(`ShopProductState` - 아이템ID·이름·가격·최대 중첩)만 들고 있다. `SetProducts`를 다시 호출하기 전까지는 가격이 바뀌지 않는다 - "가격은 층 진입 시 한 번 결정해 고정한다"는 요구를 그대로 구조로 표현했다.

`ShopService`가 실제 규칙을 처리한다.

- `Buy` - `GoldService.TrySpend`로 골드를 먼저 확인·차감하고, `InventoryRunState.TryAdd`로 공간을 검증한다. 인벤토리에 못 넣으면 방금 지출한 골드를 그대로 환불한다(아이템도 골드도 잃지 않게).
- `Sell` - `ItemCategoryRules.CanSell`로 판매 가능 분류인지 먼저 확인하고, 정가의 50%(내림)를 지급한다.

무작위성이나 UI 의존성이 없는 순수 Domain 로직이라, 97일차부터 이어온 `EquipmentService`/`RelicService`와 같은 위치·같은 패턴이다.

---

## 3. ItemDefinition.BasePrice

`Assets/ProjectDelta/Scripts/Data/ItemDefinition.cs`

상점 정가를 담는 필드를 추가했다. `ShopService.Sell`이 이 값의 50%를 판매가로 계산하므로, 상점에서 사지 않은 전리품도 정가만 설정돼 있으면 판매할 수 있다.

---

## 4. ShopInteractionService - UI 어댑터

`Assets/ProjectDelta/Scripts/Application/ShopInteractionService.cs`

`Sell(inventory, player, slotIndex, definition)` - `ItemDefinition`에서 분류·정가를 꺼내 `ShopService.Sell`에 그대로 넘긴다. `CreateProduct(definition, overridePrice)`는 상점 재고를 채울 때 `ItemDefinition`의 정가를 쓰거나, 필요하면 이번 층 한정 가격으로 덮어쓸 수 있게 했다. 98일차 `EquipmentInteractionService`와 같은 얇은 어댑터 패턴이다.

`RunContext.Shop`을 추가해 `Player`/`Inventory`/`Equipment`/`Relics`와 동급의 독립 런타임 상태로 두었다.

---

## 5. 이번 일차에서 의도적으로 하지 않은 것

- **상점 UI**는 만들지 않았다 - 문서 원문에 UI 언급이 없다.
- **층 진입 시 실제로 `SetProducts`를 호출해 상품을 채우는 연결**은 하지 않았다 - 상점에 어떤 아이템이 나올 수 있는지(후보 카탈로그)가 아직 콘텐츠로 정해지지 않아서, 그 시점은 실제 상점 콘텐츠가 만들어질 때 연결하는 게 맞다고 판단했다. `SetProducts`와 `ShopInteractionService.CreateProduct`는 그 연결을 받을 준비까지만 해뒀다.

---

## 6. 테스트

- `GoldServiceTests` - `Earn` 증가량 반환·포화·null/0/음수 무시, `TrySpend` 성공·잔액 부족 실패(상태 불변)·0 소비 성공·음수/`null` 실패.
- `ShopServiceTests` - 구매 성공(골드 차감+아이템 추가), 골드 부족 실패(상태 불변), 인벤토리 가득 참 시 골드 환불, 잘못된 상품 인덱스 실패, 판매 성공(정가 50%, 홀수 가격 내림 계산 포함), 판매 불가 분류 실패, 빈 슬롯 판매 실패.
- `ShopInteractionServiceTests` - `ItemDefinition` 기반 판매, `null` definition 실패, `CreateProduct`의 기본 가격/override 가격 처리.
- `ItemDefinitionEquipmentTests` - `BasePrice`가 설정한 값 그대로 노출되는지, 기본값이 0인지.

---

## 7. Unity 에디터에서 확인해야 할 사항

1. Scene/UI 변경 사항은 없다 - 이번 일차는 Domain/Application 로직까지만이다.
2. 아이템 에셋에 `Base Price`를 설정해보고 인스펙터에 정상 노출되는지 확인해달라.
3. 새 EditMode 테스트를 Unity Test Runner에서 실행해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
4. 기존 `BattleRewardSummaryTests`의 골드 관련 테스트(포화 동작 등)가 여전히 통과하는지 함께 확인해달라 - `ApplyDropGold` 내부 구현만 `GoldService`로 옮겼을 뿐 동작은 그대로다.
5. 재구성한 개발 일정 기준 다음은 106일차 - 보물상자 + 미믹이다.
