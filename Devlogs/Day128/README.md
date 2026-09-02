# 128일차 : 유물 보유량 확장 강화

## 목표
- 126~127일차와 같은 뼈대(Domain 규칙 클래스 + ApplicationFlow 적용/구매 + Lobby UI 행 추가)를
  재사용해, 기억의 조각으로 유물 최대 보유 수를 영구적으로 늘리는 세 번째 강화 항목 완성

## 구현 내용

### 1. 강화 규칙
- `RelicSlotUpgradeRule`(Domain) 신설 - 최대 10레벨, 레벨당 +1 보유량, 다음 레벨 비용은
  `12 × (현재 레벨 + 1)` (유물은 인벤토리 슬롯(127일차, 기본 8)보다도 희소성이 큰 영구
  효과라 더 비싸게 책정)

### 2. 저장 위치
- `ProfileData.PermanentGrowth.RelicSlotUpgradeLevel` 신설 (인벤토리 슬롯과 같은 단일 int 형태)

### 3. 적용 지점
- `RelicRunState`에 이미 있던 `MaxCapacity`/`SetMaxCapacity()` 자리를 그대로 사용 - 지금까지
  프로젝트 어디에서도 이 메서드를 호출하지 않고 있었음
- `ApplicationFlow.StartNewGame()`/`ContinueGame()` 양쪽에서 `ApplyPermanentRelicGrowth()` 호출
- `DungeonSaveMapper`가 아직 유물을 저장/복원하지 않는다는 걸 먼저 확인 - 127일차 인벤토리
  때와 달리 `ApplyBasics` 이후에 다시 덮어쓸 필요가 없어 호출 순서에 제약이 없었음
- `ApplicationFlow.TryPurchaseRelicSlotUpgrade()`: 조각 차감 + 레벨 증가 + 즉시 저장

### 4. 로비 상점 UI
- `LobbySceneController`의 기존 강화 패널에 "유물 보유량" 행 추가 - 인벤토리 슬롯 행과
  동일한 구매/비활성화 로직 재사용
