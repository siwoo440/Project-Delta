using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    public sealed class BattleIntent
    {
        public string ActorInstanceId { get; }
        public string TargetInstanceId { get; }
        public string CommandId { get; }
        public string DisplayName { get; }
        public string SkillId { get; }
        public BattleIntentIconType IconType { get; }
        public bool IsSilenceSensitive { get; }
        public SkillDefinition Skill { get; }

        public bool HasTarget =>
            !string.IsNullOrEmpty(
                TargetInstanceId);

        public BattleIntent(
            string actorInstanceId,
            string targetInstanceId,
            string commandId,
            string displayName,
            BattleIntentIconType iconType,
            string skillId = null,
            bool isSilenceSensitive = false,
            SkillDefinition skill = null)
        {
            ActorInstanceId =
                actorInstanceId;

            TargetInstanceId =
                targetInstanceId;

            CommandId =
                commandId;

            DisplayName =
                displayName;

            IconType =
                iconType;

            SkillId =
                skillId;

            IsSilenceSensitive =
                isSilenceSensitive;

            Skill =
                skill;
        }

        public static BattleIntent CreateBasicAttack(
            BattleParticipant actor,
            BattleParticipant target)
        {
            if (actor == null
                || target == null)
            {
                return null;
            }

            return new BattleIntent(
                actor.InstanceId,
                target.InstanceId,
                "Attack",
                "공격",
                BattleIntentIconType.Attack);
        }

        public static BattleIntent CreateDefend(
            BattleParticipant actor)
        {
            if (actor == null)
            {
                return null;
            }

            return new BattleIntent(
                actor.InstanceId,
                null,
                "Defend",
                "방어",
                BattleIntentIconType.Defend);
        }

        public static BattleIntent CreateSkill(
            BattleParticipant actor,
            BattleParticipant target,
            SkillDefinition skill,
            BattleIntentIconType iconType)
        {
            if (actor == null
                || skill == null)
            {
                return null;
            }

            if (skill.TargetType == SkillTargetType.Enemy
                && target == null)
            {
                return null;
            }

            BattleParticipant resolvedTarget =
                skill.TargetType == SkillTargetType.Self
                    ? actor
                    : target;

            return new BattleIntent(
                actor.InstanceId,
                resolvedTarget?.InstanceId,
                "Skill",
                skill.DisplayName,
                iconType,
                skill.Id,
                true,
                skill);
        }
    }
}
