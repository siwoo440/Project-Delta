using System.Collections.Generic;

namespace ProjectDelta.Application
{
    public static class BattleIntentService
    {
        private static readonly Dictionary<string, BattleIntent> intents =
            new Dictionary<string, BattleIntent>();

        private static readonly Dictionary<string, BattleIntentCancelReason> lastCancelReasons =
            new Dictionary<string, BattleIntentCancelReason>();

        public static int Count =>
            intents.Count;

        public static bool TryRegister(
            BattleIntent intent)
        {
            if (intent == null
                || string.IsNullOrEmpty(
                    intent.ActorInstanceId)
                || intents.ContainsKey(
                    intent.ActorInstanceId)
                || HasPendingCancellation(
                    intent.ActorInstanceId))
            {
                return false;
            }

            intents.Add(
                intent.ActorInstanceId,
                intent);

            return true;
        }

        public static bool TryGet(
            string actorInstanceId,
            out BattleIntent intent)
        {
            intent =
                null;

            if (string.IsNullOrEmpty(
                    actorInstanceId))
            {
                return false;
            }

            return intents.TryGetValue(
                actorInstanceId,
                out intent);
        }

        public static bool TryConsume(
            string actorInstanceId,
            out BattleIntent intent)
        {
            if (!TryGet(
                    actorInstanceId,
                    out intent))
            {
                return false;
            }

            intents.Remove(
                actorInstanceId);

            return true;
        }

        public static bool Cancel(
            string actorInstanceId,
            BattleIntentCancelReason reason)
        {
            if (string.IsNullOrEmpty(
                    actorInstanceId)
                || reason == BattleIntentCancelReason.None
                || !intents.Remove(
                    actorInstanceId))
            {
                return false;
            }

            lastCancelReasons[actorInstanceId] =
                reason;

            return true;
        }

        public static BattleIntentCancelReason GetLastCancelReason(
            string actorInstanceId)
        {
            if (string.IsNullOrEmpty(
                    actorInstanceId)
                || !lastCancelReasons.TryGetValue(
                    actorInstanceId,
                    out BattleIntentCancelReason reason))
            {
                return BattleIntentCancelReason.None;
            }

            return reason;
        }

        public static bool HasPendingCancellation(
            string actorInstanceId)
        {
            return GetLastCancelReason(
                    actorInstanceId)
                != BattleIntentCancelReason.None;
        }

        public static bool TryConsumeCancellation(
            string actorInstanceId,
            out BattleIntentCancelReason reason)
        {
            reason =
                BattleIntentCancelReason.None;

            if (string.IsNullOrEmpty(
                    actorInstanceId)
                || !lastCancelReasons.TryGetValue(
                    actorInstanceId,
                    out reason))
            {
                return false;
            }

            lastCancelReasons.Remove(
                actorInstanceId);

            return true;
        }

        public static BattleIntentCancelReason EvaluateCancelReason(
            BattleIntent intent,
            bool actorAlive,
            bool isStunned,
            bool isSilenced,
            bool isSatisfied,
            bool targetAvailable)
        {
            if (intent == null)
            {
                return BattleIntentCancelReason.None;
            }

            if (!actorAlive)
            {
                return BattleIntentCancelReason.ActorDefeated;
            }

            if (isStunned)
            {
                return BattleIntentCancelReason.Stunned;
            }

            if (intent.IsSilenceSensitive
                && isSilenced)
            {
                return BattleIntentCancelReason.Silenced;
            }

            if (isSatisfied)
            {
                return BattleIntentCancelReason.Satisfied;
            }

            if (intent.HasTarget
                && !targetAvailable)
            {
                return BattleIntentCancelReason.TargetUnavailable;
            }

            return BattleIntentCancelReason.None;
        }

        public static void Clear()
        {
            intents.Clear();
            lastCancelReasons.Clear();
        }
    }
}
