using System;
using System.Collections.Generic;
using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // 47일차: 플레이어와 몬스터를 전투에서 동일하게 다루기 위한 참가자 런타임 데이터.
    // 53일차: 전투 능력치를 공격·방어·속도·명중·회피·매력·저항 7종으로 정정했다.
    // 54일차: 마나·정력 자원을 도입했다.
    public sealed class BattleParticipant
    {
        public string InstanceId { get; }

        public string DefinitionId { get; }

        public BattleTeam Team { get; }

        public int MaxHp { get; }

        public int CurrentHp { get; private set; }

        public int MaxMana { get; }

        public int CurrentMana { get; private set; }

        public int MaxStamina { get; }

        public int CurrentStamina { get; private set; }

        public int Speed { get; }

        public int Attack { get; }

        public int Defense { get; }

        public int Accuracy { get; }

        public int Evasion { get; }

        public int Charm { get; }

        public int Resistance { get; }

        public bool IsAlive =>
            CurrentHp > 0;

        public bool IsDefending { get; private set; }

        private readonly List<StatusEffectInstance> statusEffects =
            new List<StatusEffectInstance>();

        public IReadOnlyList<StatusEffectInstance> StatusEffects =>
            statusEffects;

        public BattleParticipant(
            string instanceId,
            string definitionId,
            BattleTeam team,
            int maxHp,
            int speed,
            int attack,
            int defense,
            int accuracy,
            int evasion,
            int charm = 0,
            int resistance = 0,
            int maxMana = 0,
            int maxStamina = 0,
            int currentHp = -1,
            int currentMana = -1,
            int currentStamina = -1)
        {
            InstanceId =
                instanceId;

            DefinitionId =
                definitionId;

            Team =
                team;

            MaxHp =
                maxHp;

            CurrentHp =
                currentHp >= 0
                    ? Math.Min(
                        currentHp,
                        maxHp)
                    : maxHp;

            MaxMana =
                maxMana;

            CurrentMana =
                currentMana >= 0
                    ? Math.Min(
                        currentMana,
                        maxMana)
                    : maxMana;

            MaxStamina =
                maxStamina;

            CurrentStamina =
                currentStamina >= 0
                    ? Math.Min(
                        currentStamina,
                        maxStamina)
                    : maxStamina;

            Speed =
                speed;

            Attack =
                attack;

            Defense =
                defense;

            Accuracy =
                accuracy;

            Evasion =
                evasion;

            Charm =
                charm;

            Resistance =
                resistance;
        }

        public int ApplyDamage(
            int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int appliedDamage =
                Math.Min(
                    amount,
                    CurrentHp);

            CurrentHp -=
                appliedDamage;

            return appliedDamage;
        }

        public int Heal(
            int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int appliedHeal =
                Math.Min(
                    amount,
                    MaxHp - CurrentHp);

            CurrentHp +=
                appliedHeal;

            return appliedHeal;
        }

        // 93일차: 전투 중 소비 아이템의 MP 회복에 사용한다.
        public int RestoreMana(
            int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int appliedRestore =
                Math.Min(
                    amount,
                    MaxMana - CurrentMana);

            CurrentMana +=
                appliedRestore;

            return appliedRestore;
        }

        // 93일차: 전투 중 소비 아이템의 정력 회복에 사용한다.
        public int RestoreStamina(
            int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int appliedRestore =
                Math.Min(
                    amount,
                    MaxStamina - CurrentStamina);

            CurrentStamina +=
                appliedRestore;

            return appliedRestore;
        }

        public bool TrySpendMana(
            int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (amount > CurrentMana)
            {
                return false;
            }

            CurrentMana -=
                amount;

            return true;
        }

        public bool TrySpendStamina(
            int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (amount > CurrentStamina)
            {
                return false;
            }

            CurrentStamina -=
                amount;

            return true;
        }

        public void SetDefending(
            bool isDefending)
        {
            IsDefending =
                isDefending;
        }

        public void AddStatusEffect(
            StatusEffectInstance statusEffect)
        {
            if (statusEffect == null)
            {
                return;
            }

            statusEffects.Add(
                statusEffect);
        }

        public void RemoveExpiredStatusEffects()
        {
            statusEffects.RemoveAll(
                statusEffect =>
                    statusEffect.IsExpired);
        }

        public bool HasActiveStatusEffectOfKind(
            StatusEffectKind effectKind)
        {
            for (int index = 0;
                 index < statusEffects.Count;
                 index++)
            {
                StatusEffectInstance statusEffect =
                    statusEffects[index];

                if (!statusEffect.IsExpired
                    && statusEffect.EffectKind
                        == effectKind)
                {
                    return true;
                }
            }

            return false;
        }

        public void RemoveAllStatusEffects()
        {
            statusEffects.Clear();
        }
    }
}
