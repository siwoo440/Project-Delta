using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    public static class BattleIntentExecutionPolicy
    {
        public static BattleIntentCancelReason EvaluateCurrentCancelReason(
            BattleContext context,
            BattleParticipant actor,
            BattleIntent intent,
            bool isSatisfied = false)
        {
            if (intent == null)
            {
                return BattleIntentCancelReason.None;
            }

            bool actorAlive =
                actor != null
                && actor.IsAlive;

            bool isStunned =
                actor != null
                && actor.HasActiveStatusEffectOfKind(
                    StatusEffectKind.Stun);

            bool isSilenced =
                BattleStatusRestrictionPolicy.IsSilenced(
                    actor);

            bool targetAvailable =
                IsTargetAvailable(
                    context,
                    intent);

            return BattleIntentService.EvaluateCancelReason(
                intent,
                actorAlive,
                isStunned,
                isSilenced,
                isSatisfied,
                targetAvailable);
        }

        private static bool IsTargetAvailable(
            BattleContext context,
            BattleIntent intent)
        {
            if (intent == null
                || !intent.HasTarget)
            {
                return true;
            }

            return context != null
                && context.TryGetParticipant(
                    intent.TargetInstanceId,
                    out BattleParticipant target)
                && target != null
                && target.IsAlive;
        }
    }
}
