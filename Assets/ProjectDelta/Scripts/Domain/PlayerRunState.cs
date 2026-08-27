using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // Runtime player state for the active run (기획서 10.2).
    // Final stats are computed on demand from BaseStats + AllocatedStats + TemporaryStats;
    // they are never cached back into definition data ("기본 정의 데이터에 현재 능력치를 덮어쓰지 않는다").
    public sealed class PlayerRunState
    {
        public int Level;
        public int Experience;
        public int UnusedStatPoints;

        public StatBlock BaseStats = new StatBlock();
        public StatBlock AllocatedStats = new StatBlock();
        public StatBlock TemporaryStats = new StatBlock();

        // 99일차: 장착 중인 장비 6부위의 스탯 보너스 합계.
        // EquipmentService가 장착/해제/교체 시점마다 최신 값으로 갱신한다.
        public StatBlock EquipmentBonuses = new StatBlock();

        public int CurrentHp;
        public int CurrentMana;
        public int CurrentStamina;

        // 기존 ID 목록은 호환성을 위해 유지한다.
        public List<string> StatusEffects =
            new List<string>();

        // 83일차: 도주 후 탐험까지 이어지는 상태이상의 실제 남은 값.
        public List<PersistentStatusEffectState> PersistentStatusEffects =
            new List<PersistentStatusEffectState>();

        public int Gold;
        public int KeyCount;
        public string CurrentRoomId;
        public GridPosition CurrentGridPosition =
            GridPosition.Zero;

        public StatBlock GetFinalStats()
        {
            return StatBlock.Sum(
                BaseStats,
                AllocatedStats,
                TemporaryStats,
                EquipmentBonuses);
        }

        // 99일차: 장비 변경 직후 현재 자원이 새로운 최대치를 넘지 않도록 정리한다.
        // 최대치가 늘어난다고 현재 값을 채워주지는 않는다 (기존 CreateDefault만 완전 회복 처리).
        public void ClampCurrentResourcesToFinalStats()
        {
            StatBlock finalStats =
                GetFinalStats();

            if (CurrentHp > finalStats.MaxHealth)
            {
                CurrentHp =
                    finalStats.MaxHealth;
            }

            if (CurrentMana > finalStats.MaxMana)
            {
                CurrentMana =
                    finalStats.MaxMana;
            }

            if (CurrentStamina > finalStats.MaxStamina)
            {
                CurrentStamina =
                    finalStats.MaxStamina;
            }
        }

        public static PlayerRunState CreateDefault()
        {
            var state =
                new PlayerRunState
                {
                    Level = 1,
                    BaseStats =
                        new StatBlock
                        {
                            MaxHealth = 100,
                            MaxMana = 50,
                            MaxStamina = 100,
                            Attack = 50,
                            Defense = 40,
                            Speed = 50,
                            Charm = 50,
                            Evasion = 40,
                            Resistance = 50
                        }
                };

            StatBlock finalStats =
                state.GetFinalStats();

            state.CurrentHp =
                finalStats.MaxHealth;

            state.CurrentMana =
                finalStats.MaxMana;

            state.CurrentStamina =
                finalStats.MaxStamina;

            return state;
        }
    }

    [Serializable]
    public sealed class PersistentStatusEffectState
    {
        public string DefinitionId;
        public string SourceInstanceId;
        public int RemainingDuration;
        public int StackCount;
        public int AppliedValue;

        // Domain이 Data/Application enum에 의존하지 않도록 정수 값으로 보관한다.
        public int EffectKind;
        public int TargetStat;
    }

    // Mirrors the 6.1절 기본 능력치 table: 3 resource caps + 6 combat stats.
    [Serializable]
    public sealed class StatBlock
    {
        public int MaxHealth;
        public int MaxMana;
        public int MaxStamina;
        public int Attack;
        public int Defense;
        public int Speed;
        public int Charm;
        public int Evasion;
        public int Resistance;

        public static StatBlock Sum(
            params StatBlock[] blocks)
        {
            var result =
                new StatBlock();

            foreach (var block in blocks)
            {
                result.MaxHealth +=
                    block.MaxHealth;

                result.MaxMana +=
                    block.MaxMana;

                result.MaxStamina +=
                    block.MaxStamina;

                result.Attack +=
                    block.Attack;

                result.Defense +=
                    block.Defense;

                result.Speed +=
                    block.Speed;

                result.Charm +=
                    block.Charm;

                result.Evasion +=
                    block.Evasion;

                result.Resistance +=
                    block.Resistance;
            }

            return result;
        }
    }
}
