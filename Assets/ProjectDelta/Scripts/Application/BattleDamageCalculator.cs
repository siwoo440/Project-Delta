namespace ProjectDelta.Application
{
    // 53일차: 명중/회피·피해·방어 계산에서 관통을 제거하고 정식 전투 능력치 구조에 맞춘다.
    // 55일차: 피해 공식을 비율형(방어력 감쇠)으로 바꾸고 95~105% 무작위 편차를 추가한다 (기획서 4.2).
    // 56일차: 명중 공식을 정합한다 — 스킬 기본값, 회피 가중치 50%, 5~95% 클램프 (기획서 4.2).
    // 57일차: 방어 감소율을 고정값에서 방어력 기반 곡선으로 바꾸고, 방어 가능·관통·불가를
    // 구분한다 (기획서 4.2).
    // 58일차: 치명타(기본 확률 0%, 무기·스킬·유물·상태 효과로만 발생)와 피해 유형별 방어
    // 수치(고정 피해는 방어 무시, 상태 이상은 저항)를 추가한다 (기획서 4.2).
    // 65일차: 명중·피해·방어 감소 계산에 쓰는 공격력·방어력·명중·회피·저항은 참가자의 기본값이
    // 아니라 BattleStatModifierService가 강화·약화 상태(StatModifier)를 반영해 계산한 값이다.
    // 실제 데미지 적용(51일차 사망 판정 포함)이나 Command 연결은 이 클래스의 책임이 아니다.
    public static class BattleDamageCalculator
    {
        // 56일차: 기획서 4.2 "명중 공식 정합 — 스킬 기본값". 실제 스킬 데이터(66일차 이후)가
        // 생기기 전까지는 기본 공격의 고정 기본 명중률로 취급한다.
        public const int BaseSkillHitChancePercent = 70;

        // 56일차: 회피는 100%가 아니라 이 비율만큼만 명중률에서 깎인다.
        public const int EvasionWeightPercent = 50;

        public const int MinHitChancePercent = 5; // 아무리 회피가 높아도 최소 명중률 보장
        public const int MaxHitChancePercent = 95; // 56일차: 명중률 상한을 100 → 95로 낮춤

        public const int MinDamage = 1; // 방어력이 아무리 높아도 최소 피해 보장

        // 57일차: 기획서 4.2 "방어 피해 감소율 = 30% + 방어력 ÷ (방어력 + 100) × 30%, 최대 60%".
        public const int DefendBaseReductionPercent = 30;
        public const int DefendVariableReductionScalePercent = 30;
        public const int DefendMaxReductionPercent = 60;

        // 57일차: 기획서 4.2 "방어 관통 - 일부 감소율만 적용". 정확한 비율이 문서에 없어
        // 회피 가중치(56일차)와 같은 50%를 임시로 쓴다. 실제 관통 스킬이 생기면(66일차 이후)
        // 재검토가 필요하다.
        public const int PenetratingDefenseReductionWeightPercent = 50;

        // 55일차: 기획서 4.2 "최종 피해 = 기본 피해 × 95~105% 무작위 편차 × ...".
        // 치명타 배율·기타 보정은 58일차 이후 별도 항목에서 곱한다.
        public const int MinDamageVariancePercent = 95;
        public const int MaxDamageVariancePercent = 105;

        // varianceRoll이 가질 수 있는 값의 개수(0~10, 11칸) = 95~105% 11단계에 1:1 대응.
        public const int DamageVarianceRollCount =
            MaxDamageVariancePercent - MinDamageVariancePercent + 1;

        // 58일차: 기획서 4.2 "치명타 배율이 지정되지 않은 피해는 치명타가 발생하지 않는다".
        // 기본 공격은 배율을 지정하지 않으므로 이 값 그대로 두면 치명타가 절대 발생하지 않는다.
        // 실제 치명타 가능 여부·확률·배율은 무기·스킬·유물·상태 효과 데이터(66일차 이후)에서 온다.
        public const int NoCriticalChancePercent = 0;
        public const int NoCriticalMultiplierPercent = 0;

        // 56일차 명중률(%) = 스킬 기본 명중률 + 공격자 명중 - 방어자 회피 × 50%, 5~95% 사이로 고정.
        // 회피 가중치는 정수 나눗셈으로 버림 처리한다.
        // 68일차: skillAccuracyModifierPercent는 SkillDefinition.AccuracyModifierPercent가 넘어오는
        // 자리다. 기본 공격은 이 값을 넘기지 않으므로(기본값 0) 기존 계산과 동일하게 유지된다.
        public static int CalculateHitChancePercent(
            BattleParticipant attacker,
            BattleParticipant defender,
            int skillAccuracyModifierPercent = 0)
        {
            int weightedEvasion =
                BattleStatModifierService.GetEffectiveEvasion(
                    defender) * EvasionWeightPercent / 100;

            int rawHitChance =
                BaseSkillHitChancePercent
                + BattleStatModifierService.GetEffectiveAccuracy(
                    attacker)
                - weightedEvasion
                + skillAccuracyModifierPercent;

            return Clamp(
                rawHitChance,
                MinHitChancePercent,
                MaxHitChancePercent);
        }

        // 55일차 기본 피해 = 공격력 × 100 ÷ (100 + 적용 방어 수치).
        // "공격 배율"은 스킬 데이터(66일차 이후)에서 오므로, 기본 공격은 배율 100%로 취급한다.
        // 58일차: 적용 방어 수치는 기획서 4.2 피해 유형 표를 따른다 — 일반 공격은 방어력,
        // 상태 이상·지속 피해는 저항, 고정 피해는 방어력을 무시(0으로 취급)한다.
        public static int CalculateBaseDamage(
            BattleParticipant attacker,
            BattleParticipant defender,
            DamageType damageType = DamageType.Normal)
        {
            int defenseValue =
                GetDefenseValue(
                    defender,
                    damageType);

            int effectiveAttack =
                BattleStatModifierService.GetEffectiveAttack(
                    attacker);

            return effectiveAttack * 100 / (100 + defenseValue);
        }

        private static int GetDefenseValue(
            BattleParticipant defender,
            DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Fixed:
                    return 0; // 고정 피해 - 방어력 무시

                case DamageType.StatusEffect:
                case DamageType.DamageOverTime:
                    return BattleStatModifierService.GetEffectiveResistance(
                        defender); // 상태 이상·지속 피해 - 저항 사용

                default:
                    return BattleStatModifierService.GetEffectiveDefense(
                        defender); // 일반 공격·직접 공격 스킬 - 방어력 사용
            }
        }

        // 58일차: 치명타 배율이 지정되지 않았으면(0 이하) 확률과 무관하게 치명타가 발생하지 않는다.
        public static bool CanCriticalHit(
            int criticalMultiplierPercent)
        {
            return criticalMultiplierPercent > 0;
        }

        // criticalRoll(0~99 난수)이 치명타 확률보다 작으면 치명타가 발생한다.
        public static bool IsCriticalHit(
            int criticalChancePercent,
            int criticalMultiplierPercent,
            int criticalRoll)
        {
            return CanCriticalHit(
                    criticalMultiplierPercent)
                && criticalRoll < criticalChancePercent;
        }

        // varianceRoll(0~10)을 95~105% 편차(%)로 바꾼다. 범위를 벗어나면 가장 가까운 경계로 고정한다.
        public static int CalculateVariancePercent(
            int varianceRoll)
        {
            return MinDamageVariancePercent
                + Clamp(
                    varianceRoll,
                    0,
                    DamageVarianceRollCount - 1);
        }

        // 57일차 방어 피해 감소율(%) = 30% + 방어력 ÷ (방어력 + 100) × 30%, 최대 60%로 고정.
        // 방어력이 높을수록 감소율이 30%에서 60%로 완만하게 수렴하는 곡선이다.
        public static int CalculateDefendReductionPercent(
            BattleParticipant defender)
        {
            int effectiveDefense =
                BattleStatModifierService.GetEffectiveDefense(
                    defender);

            int variablePercent =
                effectiveDefense * DefendVariableReductionScalePercent
                / (effectiveDefense + 100);

            int reductionPercent =
                DefendBaseReductionPercent
                + variablePercent;

            return reductionPercent > DefendMaxReductionPercent
                ? DefendMaxReductionPercent
                : reductionPercent;
        }

        // 기본 피해에 편차를 곱하고, 스킬 배율을 곱하고, 치명타면 배율을 곱한 뒤, 방어 중이면
        // 방어 가능·관통·불가에 따라 한 번 더 감소시키고 마지막에 최소 피해 1을 보장한다.
        // 68일차: skillDamageMultiplierPercent는 SkillDefinition.DamageMultiplierPercent가
        // 넘어오는 자리다. 기본 공격은 100%를 넘겨 기존 계산과 동일하게 유지된다.
        public static int CalculateDamage(
            BattleParticipant attacker,
            BattleParticipant defender,
            int varianceRoll,
            DefenseInteraction defenseInteraction = DefenseInteraction.Defendable,
            DamageType damageType = DamageType.Normal,
            int criticalChancePercent = NoCriticalChancePercent,
            int criticalMultiplierPercent = NoCriticalMultiplierPercent,
            int criticalRoll = 0,
            int skillDamageMultiplierPercent = 100)
        {
            int baseDamage =
                CalculateBaseDamage(
                    attacker,
                    defender,
                    damageType);

            int variancePercent =
                CalculateVariancePercent(
                    varianceRoll);

            int damage =
                baseDamage * variancePercent / 100;

            damage =
                damage * skillDamageMultiplierPercent / 100;

            // 58일차: 치명타가 발생했으면 배율을 곱한다 (기획서 4.2 "... × 치명타 배율 × ...").
            if (IsCriticalHit(
                    criticalChancePercent,
                    criticalMultiplierPercent,
                    criticalRoll))
            {
                damage =
                    damage * criticalMultiplierPercent / 100;
            }

            // 57일차: 방어 불가 피해는 방어 중이어도 감소하지 않는다.
            if (defender.IsDefending
                && defenseInteraction != DefenseInteraction.IgnoresDefense)
            {
                int reductionPercent =
                    CalculateDefendReductionPercent(
                        defender);

                // 방어 관통 피해는 감소율을 일부만 적용한다.
                if (defenseInteraction == DefenseInteraction.PenetratesDefense)
                {
                    reductionPercent =
                        reductionPercent * PenetratingDefenseReductionWeightPercent / 100;
                }

                damage =
                    damage * (100 - reductionPercent) / 100;
            }

            return damage > MinDamage
                ? damage
                : MinDamage;
        }

        // roll0To99(0~99 난수)로 명중 여부를 판정하고, 명중 시에만 varianceRoll(0~10 난수)로
        // 피해량을 계산한다. 난수를 밖에서 주입받으므로 호출하는 쪽(실제 플레이 vs 테스트)이
        // 난수 발생 방식을 결정한다.
        public static BattleDamageResult Resolve(
            BattleParticipant attacker,
            BattleParticipant defender,
            int roll0To99,
            int varianceRoll,
            DefenseInteraction defenseInteraction = DefenseInteraction.Defendable,
            DamageType damageType = DamageType.Normal,
            int criticalChancePercent = NoCriticalChancePercent,
            int criticalMultiplierPercent = NoCriticalMultiplierPercent,
            int criticalRoll = 0,
            int skillAccuracyModifierPercent = 0,
            int skillDamageMultiplierPercent = 100)
        {
            int hitChancePercent =
                CalculateHitChancePercent(
                    attacker,
                    defender,
                    skillAccuracyModifierPercent);

            bool isHit =
                roll0To99 < hitChancePercent;

            if (!isHit)
            {
                return BattleDamageResult.Miss(
                    hitChancePercent);
            }

            int damage =
                CalculateDamage(
                    attacker,
                    defender,
                    varianceRoll,
                    defenseInteraction,
                    damageType,
                    criticalChancePercent,
                    criticalMultiplierPercent,
                    criticalRoll,
                    skillDamageMultiplierPercent);

            // 55일차: 편차 적용 전 기본 피해·적용된 편차(%)를 디버그 표시용으로 함께 담는다.
            int baseDamage =
                CalculateBaseDamage(
                    attacker,
                    defender,
                    damageType);

            int variancePercent =
                CalculateVariancePercent(
                    varianceRoll);

            // 58일차: 디버그 표시용으로 치명타 발생 여부도 함께 담는다.
            bool isCritical =
                IsCriticalHit(
                    criticalChancePercent,
                    criticalMultiplierPercent,
                    criticalRoll);

            return BattleDamageResult.Hit(
                damage,
                hitChancePercent,
                baseDamage,
                variancePercent,
                isCritical);
        }

        private static int Clamp(
            int value,
            int min,
            int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
