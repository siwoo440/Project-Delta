namespace ProjectDelta.Core // 순수 규칙 이름 공간
{ // 이름 공간 시작
    public enum PermanentGrowthKind // 영구 강화의 독립 항목
    { // 항목 시작
        MaxHealth, MaxMana, MaxStamina, Attack, Defense, Speed, Charm, Evasion, Resistance, // 능력치 항목
        Inventory, Relic, ShopDiscount, ShopStock, ShopRare, ShopSell, // 가방과 상점 항목
        Map, Trap, Secret, StartingConsumable, StartingGold // 탐험과 시작 보너스 항목
    } // 항목 끝
    public static class PermanentGrowthRules // 기획 수치의 순수 조회
    { // 규칙 시작
        private static readonly int[] ResourceCosts = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 12 }; // 자원 비용
        private static readonly int[] CombatCosts = { 3, 4, 5, 6, 7, 8, 9, 10, 12, 15 }; // 전투 능력 비용
        private static readonly int[] InventoryCosts = { 5, 10, 15, 20, 25 }; // 인벤토리 비용
        private static readonly int[] RelicCosts = { 15, 30 }; // 유물 한도 비용
        private static readonly int[] StockCosts = { 8, 16, 24 }; // 상점 재고 비용
        private static readonly int[] RareCosts = { 8, 12, 16, 20, 25 }; // 희귀 상품 비용
        private static readonly int[] SellCosts = { 6, 12, 18, 24 }; // 판매 비율 비용
        private static readonly int[] MapCosts = { 10, 20, 30 }; // 시작 지도 비용
        private static readonly int[] SecretCosts = { 6, 12, 18, 24, 30 }; // 비밀 방 비용
        private static readonly int[] ConsumableCosts = { 5, 12, 20 }; // 시작 소비품 비용
        public static bool TryNextCost(PermanentGrowthKind kind, int currentLevel, out int cost) // 다음 단계 비용 조회
        { // 조회 시작
            cost = 0; // 실패 결과 초기화
            var costs = Costs(kind); // 항목별 비용 표
            if (costs == null || currentLevel < 0 || currentLevel >= costs.Length) // 종류와 현재 단계 검사
            { // 거절 시작
                return false; // 유효하지 않은 비용 차단
            } // 거절 끝
            cost = costs[currentLevel]; // 다음 단계 비용 선택
            return true; // 정상 조회
        } // 조회 끝
        public static bool TryEffect(PermanentGrowthKind kind, int level, out int effect) // 단계별 누적 효과 조회
        { // 조회 시작
            effect = 0; // 실패 결과 초기화
            var costs = Costs(kind); // 항목별 단계 기준
            if (costs == null || level < 0 || level > costs.Length) // 종류와 단계 검사
            { // 거절 시작
                return false; // 유효하지 않은 효과 차단
            } // 거절 끝
            switch (kind) // 효과 계산 구분
            { // 항목 구분 시작
                case PermanentGrowthKind.MaxHealth: // 최대 체력
                case PermanentGrowthKind.MaxStamina: // 최대 정력
                case PermanentGrowthKind.Trap: // 함정 판정 보정
                case PermanentGrowthKind.Secret: // 비밀 방 확률 보정
                    effect = level * 5; // 단계당 오 증가
                    break; // 항목 계산 끝
                case PermanentGrowthKind.MaxMana: // 최대 마나
                    effect = level * 3; // 단계당 삼 증가
                    break; // 항목 계산 끝
                case PermanentGrowthKind.ShopDiscount: // 상점 구매 할인
                case PermanentGrowthKind.ShopRare: // 희귀 상품 확률
                    effect = level * 2; // 단계당 이 퍼센트 포인트
                    break; // 항목 계산 끝
                case PermanentGrowthKind.ShopSell: // 상점 판매 비율
                    effect = 50 + level * 5; // 기본 오십 퍼센트와 단계 보정
                    break; // 항목 계산 끝
                case PermanentGrowthKind.StartingGold: // 시작 골드
                    effect = level * 50; // 단계당 오십 골드
                    break; // 항목 계산 끝
                default: // 고정 단위 또는 단계형 항목
                    effect = level; // 능력치와 슬롯 증가 또는 지정 단계
                    break; // 항목 계산 끝
            } // 항목 구분 끝
            return true; // 정상 조회
        } // 조회 끝
        private static int[] Costs(PermanentGrowthKind kind) // 항목별 단계 비용 표 선택
        { // 표 선택 시작
            switch (kind) // 종류 구분
            { // 구분 시작
                case PermanentGrowthKind.MaxHealth: // 최대 체력
                case PermanentGrowthKind.MaxMana: // 최대 마나
                case PermanentGrowthKind.MaxStamina: // 최대 정력
                    return ResourceCosts; // 자원 비용 표
                case PermanentGrowthKind.Attack: // 공격력
                case PermanentGrowthKind.Defense: // 방어력
                case PermanentGrowthKind.Speed: // 속도
                case PermanentGrowthKind.Charm: // 매력
                case PermanentGrowthKind.Evasion: // 회피
                case PermanentGrowthKind.Resistance: // 저항
                    return CombatCosts; // 전투 비용 표
                case PermanentGrowthKind.Inventory: // 인벤토리 확장
                case PermanentGrowthKind.ShopDiscount: // 상점 구매 할인
                case PermanentGrowthKind.Trap: // 함정 판정
                case PermanentGrowthKind.StartingGold: // 시작 골드
                    return InventoryCosts; // 공통 단계 비용
                case PermanentGrowthKind.Relic: return RelicCosts; // 유물 한도 비용
                case PermanentGrowthKind.ShopStock: return StockCosts; // 상점 재고 비용
                case PermanentGrowthKind.ShopRare: return RareCosts; // 희귀 상품 비용
                case PermanentGrowthKind.ShopSell: return SellCosts; // 판매 비율 비용
                case PermanentGrowthKind.Map: return MapCosts; // 지도 공개 비용
                case PermanentGrowthKind.Secret: return SecretCosts; // 비밀 방 비용
                case PermanentGrowthKind.StartingConsumable: return ConsumableCosts; // 시작 소비품 비용
                default: return null; // 미등록 종류 거절
            } // 구분 끝
        } // 표 선택 끝
    } // 규칙 끝
} // 이름 공간 끝
