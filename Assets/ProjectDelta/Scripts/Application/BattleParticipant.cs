using System;
using System.Collections.Generic;
using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // 47일차: 플레이어와 몬스터를 전투에서 동일하게 다루기 위한 참가자 런타임 데이터.
    // 53일차: 전투 능력치를 공격·방어·속도·명중·회피·매력·저항 7종으로 정정했다.
    // 54일차: 마나·정력 자원을 도입했다 (기획서 4.2 · 10.2). 소모 API는 스킬 Command가 생기는
    // 66~67일차에서 연결한다.
    // 60~61일차: 상태 이상 목록(StatusEffectInstance)을 도입했다. 아직 이걸 실제로 부여하는
    // 스킬이 없어 지금은 항상 빈 목록이지만, 라운드 파이프라인(BattleRoundStatusProcessor)이
    // 이 목록을 순회할 수 있는 자리를 만들어둔다.
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

        // 61일차: 이 참가자에게 적용된 상태 이상 목록.
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

        // 60일차: 지속 회복(재생 등)에 쓰는 첫 회복 API. ApplyDamage와 대칭으로, 최대 HP를
        // 넘는 회복은 잘라내고 실제로 회복된 양을 반환한다.
        public int Heal(
            int amount)
        {
            if (amount <= 0)
            {
                return 0; // 0 이하 회복은 적용하지 않음
            }

            int appliedHeal =
                Math.Min(
                    amount,
                    MaxHp - CurrentHp); // 최대 HP를 넘는 회복은 잘라냄

            CurrentHp +=
                appliedHeal;

            return appliedHeal;
        }

        // 66일차: 스킬 사용 시 자원이 충분한지 확인하고 소모하는 첫 API. ApplyDamage와 달리
        // 일부만 깎는 개념이 없어(자원이 모자라면 스킬 자체를 못 쓴다), 충분하면 전액 차감하고
        // true를, 모자라면 아무것도 바꾸지 않고 false를 반환한다.
        public bool TrySpendMana(
            int amount)
        {
            if (amount <= 0)
            {
                return true; // 소모량이 없으면 항상 성공
            }

            if (amount > CurrentMana)
            {
                return false; // 마나 부족 - 사용 불가
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
                return true; // 소모량이 없으면 항상 성공
            }

            if (amount > CurrentStamina)
            {
                return false; // 정력 부족 - 사용 불가
            }

            CurrentStamina -=
                amount;

            return true;
        }

        // 52일차: 방어 Command와 BattleSession(자기 다음 차례 시작 시 해제)만 이 값을 바꾼다.
        public void SetDefending(
            bool isDefending)
        {
            IsDefending =
                isDefending;
        }

        // 61일차: 상태 이상을 부여·해제한다. 중첩 규칙(NoStack/RefreshDuration/Stack) 판정은
        // 64일차에서 다루므로, 지금은 목록에 더하고 빼는 것까지만 한다.
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

        // 60일차: 라운드 파이프라인의 "상태 지속 시간 감소" 단계에서, 이번 라운드에 만료된
        // 상태를 걷어낼 때 쓴다.
        public void RemoveExpiredStatusEffects()
        {
            statusEffects.RemoveAll(
                statusEffect => statusEffect.IsExpired);
        }

        // 64일차: 만료되지 않은 상태 중 지정한 EffectKind를 가진 것이 있는지 확인한다.
        // BattleSession이 기절 판정(Stun) 등에 사용한다.
        public bool HasActiveStatusEffectOfKind(
            StatusEffectKind effectKind)
        {
            for (int index = 0; index < statusEffects.Count; index++)
            {
                StatusEffectInstance statusEffect =
                    statusEffects[index];

                if (!statusEffect.IsExpired
                    && statusEffect.EffectKind == effectKind)
                {
                    return true;
                }
            }

            return false;
        }

        // 64일차: 전투 종료 시 전투 한정 상태를 모두 제거한다. Rounds·UntilCombatEnd 구분 없이
        // 다음 전투까지 어떤 상태도 남지 않아야 하므로 전체를 비운다.
        public void RemoveAllStatusEffects()
        {
            statusEffects.Clear();
        }
    }
}
