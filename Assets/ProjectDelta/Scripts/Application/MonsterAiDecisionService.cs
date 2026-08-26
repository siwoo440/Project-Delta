using System;
using System.Collections.Generic;
using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    public static class MonsterAiDecisionService
    {
        private enum CandidateType
        {
            Attack,
            Defend,
            Skill
        }

        private sealed class Candidate
        {
            public CandidateType Type { get; }
            public int Weight { get; }
            public SkillDefinition Skill { get; }

            public Candidate(
                CandidateType type,
                int weight,
                SkillDefinition skill = null)
            {
                Type =
                    type;

                Weight =
                    Math.Max(
                        0,
                        weight);

                Skill =
                    skill;
            }
        }

        public static bool TryCreateIntent(
            BattleParticipant actor,
            BattleParticipant target,
            MonsterAiProfile profile,
            bool skillsBlocked,
            IRandomSource rng,
            out BattleIntent intent)
        {
            if (profile == null)
            {
                return TryCreateIntent(
                    actor,
                    target,
                    100,
                    0,
                    0,
                    0,
                    Array.Empty<MonsterAiSkillEntry>(),
                    skillsBlocked,
                    rng,
                    out intent);
            }

            return TryCreateIntent(
                actor,
                target,
                profile.AttackWeight,
                profile.DefendWeight,
                profile.LowHpThresholdPercent,
                profile.LowHpDefendBonusWeight,
                profile.SkillEntries,
                skillsBlocked,
                rng,
                out intent);
        }

        public static bool TryCreateIntent(
            BattleParticipant actor,
            BattleParticipant target,
            int attackWeight,
            int defendWeight,
            int lowHpThresholdPercent,
            int lowHpDefendBonusWeight,
            IReadOnlyList<MonsterAiSkillEntry> skillEntries,
            bool skillsBlocked,
            IRandomSource rng,
            out BattleIntent intent)
        {
            intent =
                null;

            if (actor == null
                || !actor.IsAlive
                || rng == null)
            {
                return false;
            }

            List<Candidate> candidates =
                new List<Candidate>();

            bool targetAvailable =
                target != null
                && target.IsAlive;

            if (targetAvailable
                && attackWeight > 0)
            {
                candidates.Add(
                    new Candidate(
                        CandidateType.Attack,
                        attackWeight));
            }

            int effectiveDefendWeight =
                GetEffectiveDefendWeight(
                    actor,
                    defendWeight,
                    lowHpThresholdPercent,
                    lowHpDefendBonusWeight);

            if (effectiveDefendWeight > 0)
            {
                candidates.Add(
                    new Candidate(
                        CandidateType.Defend,
                        effectiveDefendWeight));
            }

            if (!skillsBlocked
                && skillEntries != null)
            {
                for (int index = 0; index < skillEntries.Count; index++)
                {
                    MonsterAiSkillEntry entry =
                        skillEntries[index];

                    if (entry == null
                        || entry.Skill == null
                        || entry.Weight <= 0
                        || !CanUseSkill(
                            actor,
                            target,
                            entry.Skill))
                    {
                        continue;
                    }

                    candidates.Add(
                        new Candidate(
                            CandidateType.Skill,
                            entry.Weight,
                            entry.Skill));
                }
            }

            int totalWeight =
                0;

            for (int index = 0; index < candidates.Count; index++)
            {
                totalWeight +=
                    candidates[index].Weight;
            }

            if (totalWeight <= 0)
            {
                return false;
            }

            int roll =
                rng.NextInt(
                    0,
                    totalWeight);

            Candidate selected =
                SelectCandidate(
                    candidates,
                    roll);

            if (selected == null)
            {
                return false;
            }

            switch (selected.Type)
            {
                case CandidateType.Attack:
                    intent =
                        BattleIntent.CreateBasicAttack(
                            actor,
                            target);
                    break;

                case CandidateType.Defend:
                    intent =
                        BattleIntent.CreateDefend(
                            actor);
                    break;

                case CandidateType.Skill:
                    intent =
                        BattleIntent.CreateSkill(
                            actor,
                            target,
                            selected.Skill,
                            ResolveSkillIcon(
                                selected.Skill));
                    break;
            }

            return intent != null;
        }

        public static int GetEffectiveDefendWeight(
            BattleParticipant actor,
            int defendWeight,
            int lowHpThresholdPercent,
            int lowHpDefendBonusWeight)
        {
            int result =
                Math.Max(
                    0,
                    defendWeight);

            if (actor == null
                || actor.MaxHp <= 0
                || lowHpDefendBonusWeight <= 0)
            {
                return result;
            }

            int hpPercent =
                (actor.CurrentHp * 100)
                / actor.MaxHp;

            if (hpPercent <= Math.Max(
                    0,
                    lowHpThresholdPercent))
            {
                result +=
                    lowHpDefendBonusWeight;
            }

            return result;
        }

        private static bool CanUseSkill(
            BattleParticipant actor,
            BattleParticipant target,
            SkillDefinition skill)
        {
            if (actor == null
                || skill == null
                || actor.CurrentMana < skill.ManaCost
                || actor.CurrentStamina < skill.StaminaCost)
            {
                return false;
            }

            if (skill.TargetType == SkillTargetType.Enemy)
            {
                return target != null
                    && target.IsAlive;
            }

            return true;
        }

        private static Candidate SelectCandidate(
            IReadOnlyList<Candidate> candidates,
            int roll)
        {
            int cursor =
                0;

            for (int index = 0; index < candidates.Count; index++)
            {
                Candidate candidate =
                    candidates[index];

                cursor +=
                    candidate.Weight;

                if (roll < cursor)
                {
                    return candidate;
                }
            }

            return candidates.Count > 0
                ? candidates[candidates.Count - 1]
                : null;
        }

        private static BattleIntentIconType ResolveSkillIcon(
            SkillDefinition skill)
        {
            if (skill == null)
            {
                return BattleIntentIconType.Special;
            }

            StatusEffectDefinition status =
                skill.GrantedStatusEffect;

            if (status == null)
            {
                return skill.TargetType == SkillTargetType.Self
                    ? BattleIntentIconType.Buff
                    : BattleIntentIconType.Attack;
            }

            switch (status.EffectKind)
            {
                case StatusEffectKind.HealOverTime:
                    return BattleIntentIconType.Heal;

                case StatusEffectKind.DamageOverTime:
                case StatusEffectKind.Stun:
                    return BattleIntentIconType.Status;

                case StatusEffectKind.ExtraAction:
                    return BattleIntentIconType.Buff;

                case StatusEffectKind.StatModifier:
                    return skill.TargetType == SkillTargetType.Self
                        ? BattleIntentIconType.Buff
                        : BattleIntentIconType.Debuff;

                case StatusEffectKind.Neutral:
                    return skill.TargetType == SkillTargetType.Self
                        ? BattleIntentIconType.Buff
                        : BattleIntentIconType.Debuff;

                default:
                    return BattleIntentIconType.Special;
            }
        }
    }
}
