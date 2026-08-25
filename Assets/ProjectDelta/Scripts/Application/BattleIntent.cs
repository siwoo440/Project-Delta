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
            bool isSilenceSensitive = false)
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
    }
}
