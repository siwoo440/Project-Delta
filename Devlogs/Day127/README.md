# 127일차 : 인벤토리 슬롯 확장 강화

## 목표
- 126일차와 같은 뼈대(Domain 규칙 클래스 + ApplicationFlow 적용/구매 + Lobby UI 행 추가)를
  재사용해, 기억의 조각으로 인벤토리 슬롯을 영구적으로 늘리는 두 번째 강화 항목 완성

## 구현 내용

### 1. 강화 규칙
- `InventorySlotUpgradeRule`(Domain) 신설 - 최대 10레벨, 레벨당 +1슬롯, 다음 레벨 비용은
  `8 × (현재 레벨 + 1)` (가방 아이템보다 강한 영구 효과라 126일차 스탯 강화(기본 5)보다
  조금 더 비싸게 책정)

### 2. 저장 위치
- `ProfileData.PermanentGrowth.InventorySlotUpgradeLevel` 신설 (스탯 강화와 달리 항목이
  하나뿐이라 dict 대신 단일 int)

### 3. 적용 지점
- `InventoryRunState`에 이미 있던 `PermanentSlotBonus`/`SetCapacityBonuses()` 자리를 그대로 사용
- `ApplicationFlow.StartNewGame()`: `RunContext.Begin()` 직후 `ApplyPermanentInventoryGrowth()`로
  즉시 적용
- `ApplicationFlow.ContinueGame()`: `DungeonSaveMapper.ApplyBasics()`가 저장 시점의 예전 슬롯
  보너스로 인벤토리를 복원하므로, **그 다음에** 다시 한번 `ApplyPermanentInventoryGrowth()`를
  호출해 프로필 기준의 최신 값으로 덮어씀 - 호출 순서가 중요했던 부분
- 기존 가방 아이템 보너스(102일차 `BagExpansionService`의 `BagSlotBonus`)는 그대로 보존하며
  영구 보너스만 갱신
- `ApplicationFlow.TryPurchaseInventorySlotUpgrade()`: 조각 차감 + 레벨 증가 + 즉시 저장

### 4. 로비 상점 UI
- `LobbySceneController`의 기존 강화 패널에 "인벤토리 슬롯" 행 추가 - 126일차와 같은
  구매/비활성화 로직 재사용
