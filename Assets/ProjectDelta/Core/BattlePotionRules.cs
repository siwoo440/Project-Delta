namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class BattlePotionRules // 일반 전투의 시험 회복약 사용
    { // 클래스 시작
        public static bool TryUse(InventoryState inventory, CombatFighter fighter, string potionId, int amount, out int restored) // 회복약 수량과 전투 체력 검사
        { // 사용 시작
            restored = 0; // 기본 회복량 없음
            if (inventory == null || fighter == null || !fighter.Actor.IsPlayer || !fighter.Actor.IsAlive || string.IsNullOrEmpty(potionId)) // 인벤토리와 생존 플레이어 검사
            { // 잘못된 입력 조건 시작
                return false; // 잘못된 회복약 사용 차단
            } // 잘못된 입력 조건 끝
            if (!PotionRecoveryRules.TryRecover(fighter.Health, fighter.MaxHealth, amount, out _)) // 실제 체력 회복 가능 여부 검사
            { // 회복 불가 조건 시작
                return false; // 효과 없는 아이템 소비 차단
            } // 회복 불가 조건 끝
            var index = -1; // 첫 회복약 묶음 슬롯
            for (var slot = 0; slot < inventory.Slots.Count; slot++) // 인벤토리 슬롯 순회
            { // 슬롯 검사 시작
                if (inventory.Slots[slot].ItemId == potionId && inventory.Slots[slot].Quantity > 0) // 회복약 묶음 검사
                { // 회복약 조건 시작
                    index = slot; // 사용 가능한 슬롯 보관
                    break; // 추가 슬롯 검사 종료
                } // 회복약 조건 끝
            } // 슬롯 검사 끝
            if (index < 0 || !inventory.Remove(index, 1, out var removed) || removed != 1) // 실제 회복약 한 개 소비 검사
            { // 소비 불가 조건 시작
                return false; // 회복약 없는 체력 변경 차단
            } // 소비 불가 조건 끝
            return fighter.TryHeal(amount, out restored); // 검증한 체력 회복과 실제 회복량 전달
        } // 사용 끝
    } // 클래스 끝
} // 이름 공간 끝
