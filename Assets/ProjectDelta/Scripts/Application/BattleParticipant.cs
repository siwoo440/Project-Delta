using System;

namespace ProjectDelta.Application
{
    // 47일차: 플레이어와 몬스터를 전투에서 동일하게 다루기 위한 참가자 런타임 데이터.
    // 53일차: 전투 능력치를 공격·방어·속도·명중·회피·매력·저항 7종으로 정정했다.
    // 54일차: 마나·정력 자원을 도입했다 (기획서 4.2 · 10.2). 소모 API는 스킬 Command가 생기는
    // 66~67일차에서 연결한다.
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

        // 52일차: 방어를 선택하면 true가 되고, 자기 다음 차례가 돌아오면 세션이 해제한다.
        public bool IsDefending { get; private set; }

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
                    ? Math.Min(currentHp, maxHp)
                    : maxHp; // 지정하지 않으면 만땅으로 시작 (기존 호출부 호환)

            MaxMana =
                maxMana;

            CurrentMana =
                currentMana >= 0
                    ? Math.Min(currentMana, maxMana)
                    : maxMana;

            MaxStamina =
                maxStamina;

            CurrentStamina =
                currentStamina >= 0
                    ? Math.Min(currentStamina, maxStamina)
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

        // 52일차: 방어 Command와 BattleSession(자기 다음 차례 시작 시 해제)만 이 값을 바꾼다.
        public void SetDefending(
            bool isDefending)
        {
            IsDefending =
                isDefending;
        }
    }
}
