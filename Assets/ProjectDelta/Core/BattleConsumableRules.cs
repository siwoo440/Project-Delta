namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class BattleConsumableRules // 일반 전투의 선택 소비품 사용
    { // 규칙 클래스 시작
        public static bool TryUse(InventoryState inventory, int slotIndex, CombatFighter player, int mana, int maxMana, out int newMana, out int restored) // 실제 전투자와 선택 아이템의 회복 판정
        { // 사용 시작
            newMana = mana; // 실패 시 기존 마나 보존
            restored = 0; // 실패 시 회복량 없음
            if (inventory == null || player == null || !player.Actor.IsPlayer || !player.Actor.IsAlive || player.Health <= 0 || slotIndex < 0 || slotIndex >= inventory.Slots.Count || maxMana <= 0 || mana < 0 || mana > maxMana) // 생존 플레이어와 입력 범위 검사
            { // 잘못된 사용 분기 시작
                return false; // 자원과 아이템 보존
            } // 잘못된 사용 분기 끝
            var item = TrialConsumableRules.Find(inventory.Slots[slotIndex].ItemId); // 선택 슬롯의 소비품 정의 조회
            if (item == null) // 등록된 소비품 검사
            { // 정의 없음 분기 시작
                return false; // 미등록 아이템 보존
            } // 정의 없음 분기 끝
            if (item.Resource == TrialConsumableResource.Health) // 체력 회복약 검사
            { // 체력 회복 분기 시작
                if (!TrialConsumableRules.TryUse(inventory, slotIndex, player.Health, player.MaxHealth, out _)) // 회복 가능 검사와 선택 아이템 소비
                { // 체력 사용 불가 시작
                    return false; // 실제 체력과 아이템 보존
                } // 체력 사용 불가 끝
                return player.TryHeal(item.Recovery, out restored); // 검증된 전투 체력 회복
            } // 체력 회복 분기 끝
            if (!TrialConsumableRules.TryUse(inventory, slotIndex, mana, maxMana, out var recoveredMana)) // 마나 회복 가능 검사와 선택 아이템 소비
            { // 마나 사용 불가 시작
                return false; // 마나와 아이템 보존
            } // 마나 사용 불가 끝
            newMana = recoveredMana; // 실제 회복된 마나 전달
            restored = recoveredMana - mana; // 실제 마나 회복량 전달
            return true; // 마나 회복 성공
        } // 사용 끝
    } // 규칙 클래스 끝
} // 이름 공간 끝
