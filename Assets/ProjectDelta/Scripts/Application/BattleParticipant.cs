using System;

namespace ProjectDelta.Application
{
    // 47일차: 플레이어와 몬스터를 전투에서 동일하게 다루기 위한 참가자 런타임 데이터.
    // 50일차: 명중·회피·피해·관통 계산에 필요한 전투 스탯과 HP 감소 API를 추가했다.
    public sealed class BattleParticipant
    {
        public string InstanceId { get; }
        public string DefinitionId { get; }
        public BattleTeam Team { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int Speed { get; }
        public int Attack { get; }
        public int Defense { get; }
        public int Accuracy { get; }
        public int Evasion { get; }
        public int Penetration { get; }

        public bool IsAlive =>
            CurrentHp > 0;

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
            int penetration)
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
                maxHp;

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

            Penetration =
                penetration;
        }

        // 50일차: HP를 실제로 깎는 첫 API. 남은 HP보다 큰 피해는 잘라내고,
        // 실제로 적용된 피해량을 반환한다. 사망 판정(전투 이탈 등)은 51일차에서 다룬다.
        public int ApplyDamage(
            int amount)
        {
            if (amount <= 0)
            {
                return 0; // 0 이하 피해는 적용하지 않음
            }

            int appliedDamage =
                Math.Min(
                    amount,
                    CurrentHp); // 남은 HP보다 큰 피해는 잘라냄

            CurrentHp -=
                appliedDamage;

            return appliedDamage;
        }
    }
}
