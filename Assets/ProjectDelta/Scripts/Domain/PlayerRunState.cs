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

        public int CurrentHp;
        public int CurrentMana;
        public int CurrentStamina;

        public List<string> StatusEffects = new List<string>();

        public int Gold;
        public string CurrentRoomId;

        public StatBlock GetFinalStats()
        {
            return StatBlock.Sum(BaseStats, AllocatedStats, TemporaryStats);
        }
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

        public static StatBlock Sum(params StatBlock[] blocks)
        {
            var result = new StatBlock();
            foreach (var block in blocks)
            {
                result.MaxHealth += block.MaxHealth;
                result.MaxMana += block.MaxMana;
                result.MaxStamina += block.MaxStamina;
                result.Attack += block.Attack;
                result.Defense += block.Defense;
                result.Speed += block.Speed;
                result.Charm += block.Charm;
                result.Evasion += block.Evasion;
                result.Resistance += block.Resistance;
            }

            return result;
        }
    }
}
