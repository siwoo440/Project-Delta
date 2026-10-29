using System; // 장비 식별자와 정수 범위
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public enum EquipmentSlot // 장비의 네 부위
    { // 부위 목록 시작
        Weapon, // 무기 부위
        Armor, // 방어구 부위
        Accessory, // 장신구 부위
        Bag // 가방 부위
    } // 부위 목록 끝
    public sealed class EquipmentDefinition // 시험 장비의 고정된 능력치
    { // 클래스 시작
        public string Id { get; } // 장비 식별자
        public string Name { get; } // 장비 표시 이름
        public EquipmentSlot Slot { get; } // 장착 가능한 부위
        public int AttackBonus { get; } // 장비 공격력 보정
        public int DefenseBonus { get; } // 장비 방어력 보정
        public int SpeedBonus { get; } // 장비 속도 보정
        public int CriticalChance { get; } // 무기 치명타 확률
        public int CriticalMultiplierPercent { get; } // 무기 치명타 배율 백분율
        public int CapacityBonus { get; } // 가방의 추가 인벤토리 칸
        internal EquipmentDefinition(string id, string name, EquipmentSlot slot, int attack, int defense, int speed, int criticalChance, int criticalMultiplierPercent, int capacity) // 고정 장비 정의 생성
        { // 생성 시작
            Id = id; // 실제 장비 식별자 보관
            Name = name; // 장비 표시 이름 보관
            Slot = slot; // 장착 부위 보관
            AttackBonus = attack; // 공격력 보정 보관
            DefenseBonus = defense; // 방어력 보정 보관
            SpeedBonus = speed; // 속도 보정 보관
            CriticalChance = criticalChance; // 치명타 확률 보관
            CriticalMultiplierPercent = criticalMultiplierPercent; // 치명타 배율 보관
            CapacityBonus = capacity; // 추가 칸 보관
        } // 생성 끝
    } // 클래스 끝
    public static class EquipmentCatalog // 확인된 시험 장비 정의
    { // 클래스 시작
        public const string BasicSwordId = "equipment-basic-sword"; // 기본 검 식별자
        public const string BasicAxeId = "equipment-basic-axe"; // 기본 도끼 식별자
        public const string LightArmorId = "equipment-light-armor"; // 경갑 식별자
        public const string CombatAccessoryId = "equipment-combat-accessory"; // 전투 장신구 식별자
        public const string OldBagId = "equipment-old-bag"; // 낡은 주머니 식별자
        private static readonly EquipmentDefinition[] Definitions = // 고정 시험 장비 목록
        { // 목록 시작
            new EquipmentDefinition(BasicSwordId, "기본 검", EquipmentSlot.Weapon, 5, 0, 0, 40, 150, 0), // 공격력 5와 치명타 40% 및 1.5배
            new EquipmentDefinition(BasicAxeId, "기본 도끼", EquipmentSlot.Weapon, 10, 0, 0, 30, 200, 0), // 공격력 10과 치명타 30% 및 2배
            new EquipmentDefinition(LightArmorId, "경갑", EquipmentSlot.Armor, 0, 8, 2, 0, 0, 0), // 방어력 8과 속도 2
            new EquipmentDefinition(CombatAccessoryId, "전투 장신구", EquipmentSlot.Accessory, 3, 2, 0, 0, 0, 0), // 공격력 3과 방어력 2
            new EquipmentDefinition(OldBagId, "낡은 주머니", EquipmentSlot.Bag, 0, 0, 0, 0, 0, 2) // 추가 인벤토리 두 칸
        }; // 목록 끝
        public static EquipmentDefinition Find(string id) // 등록된 장비 식별자 조회
        { // 조회 시작
            foreach (var definition in Definitions) // 고정 장비 순회
            { // 장비 검사 시작
                if (string.Equals(definition.Id, id, StringComparison.Ordinal)) // 정확한 식별자 비교
                { // 일치 시작
                    return definition; // 검증된 장비 정의 전달
                } // 일치 끝
            } // 장비 검사 끝
            return null; // 미등록 장비의 조회 실패
        } // 조회 끝
    } // 클래스 끝
    public sealed class EquipmentStats // 장비가 반영된 능력치 결과
    { // 클래스 시작
        public int Attack { get; } // 최종 공격력
        public int Defense { get; } // 최종 방어력
        public int Speed { get; } // 최종 속도
        public int CriticalChance { get; } // 최종 치명타 확률
        public int CriticalMultiplierPercent { get; } // 최종 치명타 배율 백분율
        public int InventoryCapacity { get; } // 최종 인벤토리 칸 수
        internal EquipmentStats(int attack, int defense, int speed, int criticalChance, int criticalMultiplierPercent, int inventoryCapacity) // 검증된 능력치 결과 생성
        { // 생성 시작
            Attack = attack; // 최종 공격력 보관
            Defense = defense; // 최종 방어력 보관
            Speed = speed; // 최종 속도 보관
            CriticalChance = criticalChance; // 최종 치명타 확률 보관
            CriticalMultiplierPercent = criticalMultiplierPercent; // 최종 치명타 배율 보관
            InventoryCapacity = inventoryCapacity; // 최종 인벤토리 칸 보관
        } // 생성 끝
    } // 클래스 끝
    public sealed class EquipmentState // 부위별 불변 장착 상태
    { // 클래스 시작
        private readonly EquipmentDefinition weapon; // 장착한 무기
        private readonly EquipmentDefinition armor; // 장착한 방어구
        private readonly EquipmentDefinition accessory; // 장착한 장신구
        private readonly EquipmentDefinition bag; // 장착한 가방
        private EquipmentVariantData weaponVariant; // 무기의 개별 특성
        private EquipmentVariantData armorVariant; // 방어구의 개별 특성
        private EquipmentVariantData accessoryVariant; // 장신구의 개별 특성
        private EquipmentVariantData bagVariant; // 가방의 개별 특성
        public static EquipmentState Empty => new EquipmentState(null, null, null, null); // 장비가 없는 새 회차 상태
        private EquipmentState(EquipmentDefinition weapon, EquipmentDefinition armor, EquipmentDefinition accessory, EquipmentDefinition bag) // 검증된 부위별 장비 구성
        { // 구성 시작
            this.weapon = weapon; // 무기 상태 보관
            this.armor = armor; // 방어구 상태 보관
            this.accessory = accessory; // 장신구 상태 보관
            this.bag = bag; // 가방 상태 보관
        } // 구성 끝
        public EquipmentDefinition Get(EquipmentSlot slot) // 지정 부위의 장비 조회
        { // 조회 시작
            switch (slot) // 장착 부위 구분
            { // 부위 선택 시작
                case EquipmentSlot.Weapon: return weapon; // 무기 조회
                case EquipmentSlot.Armor: return armor; // 방어구 조회
                case EquipmentSlot.Accessory: return accessory; // 장신구 조회
                case EquipmentSlot.Bag: return bag; // 가방 조회
                default: return null; // 알 수 없는 부위 차단
            } // 부위 선택 끝
        } // 조회 끝
        public EquipmentVariantData GetVariant(EquipmentSlot slot) // 해당 부위의 독립 개별 장비 조회
        { // 조회 시작
            var item = slot == EquipmentSlot.Weapon ? weaponVariant : slot == EquipmentSlot.Armor ? armorVariant : slot == EquipmentSlot.Accessory ? accessoryVariant : slot == EquipmentSlot.Bag ? bagVariant : null; // 선택 부위의 개별 장비
            return EquipmentVariantRules.Copy(item); // 외부 변경을 차단한 독립 복사
        } // 조회 끝
        public static bool TryRestore(string weaponId, string armorId, string accessoryId, string bagId, EquipmentVariantData weaponData, EquipmentVariantData armorData, EquipmentVariantData accessoryData, EquipmentVariantData bagData, out EquipmentState state) // 개별 특성을 포함한 부위별 복원
        { // 복원 시작
            state = null; // 기본 복원 결과 없음
            if (!TryRestore(weaponId, armorId, accessoryId, bagId, out var restored) || !Matches(weaponId, weaponData) || !Matches(armorId, armorData) || !Matches(accessoryId, accessoryData) || !Matches(bagId, bagData)) // 기본 장비와 개별 장비 일치 검사
            { // 복원 오류 시작
                return false; // 다른 종류와 손상된 특성 차단
            } // 복원 오류 끝
            restored.weaponVariant = EquipmentVariantRules.Copy(weaponData); // 무기 특성 독립 복원
            restored.armorVariant = EquipmentVariantRules.Copy(armorData); // 방어구 특성 독립 복원
            restored.accessoryVariant = EquipmentVariantRules.Copy(accessoryData); // 장신구 특성 독립 복원
            restored.bagVariant = EquipmentVariantRules.Copy(bagData); // 가방 특성 독립 복원
            state = restored; // 검증된 장비 상태 전달
            return true; // 독립 장비 복원 완료
        } // 복원 끝
        public static bool TryRestore(string weaponId, string armorId, string accessoryId, string bagId, out EquipmentState state) // 부위별 식별자의 저장 복원
        { // 복원 시작
            state = null; // 기본 복원 결과 없음
            if (!TryDefinition(weaponId, EquipmentSlot.Weapon, out var weapon) || !TryDefinition(armorId, EquipmentSlot.Armor, out var armor) || !TryDefinition(accessoryId, EquipmentSlot.Accessory, out var accessory) || !TryDefinition(bagId, EquipmentSlot.Bag, out var bag)) // 등록 여부와 부위 검사
            { // 복원 거절 시작
                return false; // 알 수 없는 장비와 잘못된 부위 차단
            } // 복원 거절 끝
            state = new EquipmentState(weapon, armor, accessory, bag); // 검증된 독립 장비 상태
            return true; // 정상 장착 상태 복원
        } // 복원 끝
        public bool TryEquip(EquipmentSlot slot, string id, out EquipmentState state) // 기존 장비의 지정 부위 교체
        { // 기존 장착 시작
            return TryEquip(slot, id, null, out state); // 개별 특성 없는 기존 장비 호환
        } // 기존 장착 끝
        public bool TryEquip(EquipmentSlot slot, string id, EquipmentVariantData variant, out EquipmentState state) // 개별 특성을 포함한 장비 교체
        { // 장착 시작
            state = null; // 기본 장착 결과 없음
            var definition = EquipmentCatalog.Find(id); // 등록 장비 조회
            if (!IsValidSlot(slot) || definition == null || definition.Slot != slot || !Matches(id, variant)) // 부위와 개별 특성 일치 검사
            { // 장착 거절 시작
                return false; // 잘못된 장비의 상태 변경 차단
            } // 장착 거절 끝
            state = Replace(slot, definition, variant); // 변경된 독립 장비 상태
            return true; // 유효한 장비 장착 성공
        } // 장착 끝
        public bool TryUnequip(EquipmentSlot slot, out EquipmentState state) // 지정 부위의 장비 해제
        { // 해제 시작
            state = null; // 기본 해제 결과 없음
            if (!IsValidSlot(slot) || Get(slot) == null) // 유효한 부위와 실제 장비 검사
            { // 해제 거절 시작
                return false; // 빈 부위와 잘못된 부위의 해제 차단
            } // 해제 거절 끝
            state = Replace(slot, null, null); // 선택 부위만 비운 독립 상태
            return true; // 유효한 장비 해제 성공
        } // 해제 끝
        public bool TryCalculate(int baseAttack, int baseDefense, int baseSpeed, int bareCriticalChance, int bareCriticalMultiplierPercent, int baseCapacity, out EquipmentStats result) // 장비와 맨손 기준의 능력치 합산
        { // 계산 시작
            result = null; // 기본 계산 결과 없음
            if (baseAttack < 0 || baseDefense < 0 || baseSpeed < 0 || bareCriticalChance < 0 || bareCriticalChance > 100 || bareCriticalMultiplierPercent < 100 || baseCapacity < 0) // 기본 능력치와 맨손 기준 검사
            { // 기본값 오류 시작
                return false; // 범위 밖 기본 능력치 차단
            } // 기본값 오류 끝
            long attack = baseAttack; // 정수 범위 보호의 공격력
            long defense = baseDefense; // 정수 범위 보호의 방어력
            long speed = baseSpeed; // 정수 범위 보호의 속도
            long capacity = baseCapacity; // 정수 범위 보호의 인벤토리 칸
            foreach (var definition in new[] { weapon, armor, accessory, bag }) // 네 부위의 장비 순회
            { // 보정 합산 시작
                if (definition == null) // 빈 장비 부위 검사
                { // 빈 부위 시작
                    continue; // 보정 없이 다음 부위 검사
                } // 빈 부위 끝
                attack += definition.AttackBonus; // 장비 공격력 합산
                defense += definition.DefenseBonus; // 장비 방어력 합산
                speed += definition.SpeedBonus; // 장비 속도 합산
                capacity += definition.CapacityBonus; // 가방의 추가 칸 합산
            } // 보정 합산 끝
            foreach (var variant in new[] { weaponVariant, armorVariant, accessoryVariant, bagVariant }) // 장착된 개별 특성 순회
            { // 개별 효과 합산 시작
                if (variant == null) // 이전 고정 장비 검사
                { // 이전 장비 시작
                    continue; // 추가 특성 없는 장비 유지
                } // 이전 장비 끝
                foreach (var effect in variant.options) // 확정된 추가 옵션 순회
                { // 추가 옵션 시작
                    attack += effect.optionId == TrialEquipmentTraits.Attack ? effect.value : 0; // 시험 공격 옵션 합산
                    defense += effect.optionId == TrialEquipmentTraits.Defense ? effect.value : 0; // 시험 방어 옵션 합산
                    speed += effect.optionId == TrialEquipmentTraits.Speed ? effect.value : 0; // 시험 속도 옵션 합산
                } // 추가 옵션 끝
                foreach (var effect in variant.curseEffects) // 공개된 저주 효과 순회
                { // 저주 효과 시작
                    attack += effect.optionId == TrialEquipmentTraits.Attack ? effect.value : 0; // 시험 저주의 공격 보정
                    defense += effect.optionId == TrialEquipmentTraits.Defense ? effect.value : 0; // 시험 저주의 방어 보정
                    speed += effect.optionId == TrialEquipmentTraits.Speed ? effect.value : 0; // 시험 저주의 속도 보정
                } // 저주 효과 끝
            } // 개별 효과 합산 끝
            attack = Math.Max(0L, attack); // 음수 공격력 제한
            defense = Math.Max(0L, defense); // 음수 방어력 제한
            speed = Math.Max(0L, speed); // 음수 속도 제한
            if (attack > int.MaxValue || defense > int.MaxValue || speed > int.MaxValue || capacity > int.MaxValue) // 최종 정수 범위 검사
            { // 범위 초과 시작
                return false; // 잘못된 계산과 넘침 차단
            } // 범위 초과 끝
            var criticalChance = weapon == null ? bareCriticalChance : weapon.CriticalChance; // 맨손 또는 장착 무기의 확률
            var criticalMultiplier = weapon == null ? bareCriticalMultiplierPercent : weapon.CriticalMultiplierPercent; // 맨손 또는 장착 무기의 배율
            result = new EquipmentStats((int)attack, (int)defense, (int)speed, criticalChance, criticalMultiplier, (int)capacity); // 검증된 최종 능력치 전달
            return true; // 정상 장비 합산 성공
        } // 계산 끝
        private EquipmentState Replace(EquipmentSlot slot, EquipmentDefinition definition, EquipmentVariantData variant) // 선택한 한 부위만 교체
        { // 교체 시작
            var result = new EquipmentState(slot == EquipmentSlot.Weapon ? definition : weapon, slot == EquipmentSlot.Armor ? definition : armor, slot == EquipmentSlot.Accessory ? definition : accessory, slot == EquipmentSlot.Bag ? definition : bag); // 기본 장비를 보존한 새 상태
            result.weaponVariant = EquipmentVariantRules.Copy(slot == EquipmentSlot.Weapon ? variant : weaponVariant); // 무기 특성 독립 보존
            result.armorVariant = EquipmentVariantRules.Copy(slot == EquipmentSlot.Armor ? variant : armorVariant); // 방어구 특성 독립 보존
            result.accessoryVariant = EquipmentVariantRules.Copy(slot == EquipmentSlot.Accessory ? variant : accessoryVariant); // 장신구 특성 독립 보존
            result.bagVariant = EquipmentVariantRules.Copy(slot == EquipmentSlot.Bag ? variant : bagVariant); // 가방 특성 독립 보존
            return result; // 독립 장비 상태 전달
        } // 교체 끝
        private static bool Matches(string id, EquipmentVariantData variant) // 기본 장비와 개별 특성 일치 검사
        { // 검사 시작
            return variant == null || variant.definitionId == id && TrialEquipmentTraits.IsValid(variant); // 이전 장비 또는 유효한 시험 특성 허용
        } // 검사 끝
        private static bool TryDefinition(string id, EquipmentSlot slot, out EquipmentDefinition definition) // 저장된 장비의 등록과 부위 검사
        { // 검사 시작
            definition = null; // 기본 빈 장비
            if (string.IsNullOrEmpty(id)) // 저장된 빈 부위 검사
            { // 빈 부위 시작
                return true; // 장비가 없는 부위 허용
            } // 빈 부위 끝
            definition = EquipmentCatalog.Find(id); // 저장된 장비 조회
            return definition != null && definition.Slot == slot; // 등록된 장비와 부위 일치 확인
        } // 검사 끝
        private static bool IsValidSlot(EquipmentSlot slot) // 정의된 장비 부위 검사
        { // 검사 시작
            return slot == EquipmentSlot.Weapon || slot == EquipmentSlot.Armor || slot == EquipmentSlot.Accessory || slot == EquipmentSlot.Bag; // 네 부위의 유효성
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
