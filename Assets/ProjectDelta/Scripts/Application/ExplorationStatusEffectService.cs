using System;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 83일차: 도주 후 남은 상태이상을 탐험 이동 단위로 처리한다.
    public static class ExplorationStatusEffectService
    {
        public static bool TryConsumeStunMoveAttempt(
            PlayerRunState player)
        {
            if (player == null
                || player.PersistentStatusEffects == null)
            {
                return false;
            }

            bool blocked =
                false;

            for (int index = 0;
                 index < player.PersistentStatusEffects.Count;
                 index++)
            {
                PersistentStatusEffectState status =
                    player.PersistentStatusEffects[index];

                if (status == null
                    || status.RemainingDuration <= 0)
                {
                    continue;
                }

                if ((StatusEffectKind)status.EffectKind
                    != StatusEffectKind.Stun)
                {
                    continue;
                }

                blocked =
                    true;

                status.RemainingDuration =
                    Math.Max(
                        0,
                        status.RemainingDuration - 1);
            }

            RemoveExpiredAndBattleOnlyEffects(
                player);

            SynchronizeLegacyStatusIds(
                player);

            return blocked;
        }

        public static bool ApplyAfterSuccessfulMove(
            PlayerRunState player)
        {
            if (player == null
                || player.PersistentStatusEffects == null)
            {
                return false;
            }

            StatBlock finalStats =
                player.GetFinalStats();

            for (int index = 0;
                 index < player.PersistentStatusEffects.Count;
                 index++)
            {
                PersistentStatusEffectState status =
                    player.PersistentStatusEffects[index];

                if (status == null
                    || status.RemainingDuration <= 0)
                {
                    continue;
                }

                StatusEffectKind kind =
                    (StatusEffectKind)status.EffectKind;

                if (kind == StatusEffectKind.ExtraAction)
                {
                    continue;
                }

                int tickAmount =
                    CalculateTickAmount(
                        status);

                switch (kind)
                {
                    case StatusEffectKind.DamageOverTime:
                        player.CurrentHp =
                            Math.Max(
                                0,
                                player.CurrentHp - tickAmount);
                        break;

                    case StatusEffectKind.HealOverTime:
                        player.CurrentHp =
                            Math.Min(
                                Math.Max(
                                    0,
                                    finalStats.MaxHealth),
                                player.CurrentHp + tickAmount);
                        break;
                }

                status.RemainingDuration =
                    Math.Max(
                        0,
                        status.RemainingDuration - 1);
            }

            RemoveExpiredAndBattleOnlyEffects(
                player);

            SynchronizeLegacyStatusIds(
                player);

            return player.CurrentHp <= 0;
        }

        public static void SynchronizeLegacyStatusIds(
            PlayerRunState player)
        {
            if (player == null)
            {
                return;
            }

            player.StatusEffects.Clear();

            for (int index = 0;
                 index < player.PersistentStatusEffects.Count;
                 index++)
            {
                PersistentStatusEffectState status =
                    player.PersistentStatusEffects[index];

                if (status == null
                    || status.RemainingDuration <= 0
                    || string.IsNullOrEmpty(
                        status.DefinitionId))
                {
                    continue;
                }

                player.StatusEffects.Add(
                    status.DefinitionId);
            }
        }

        private static void RemoveExpiredAndBattleOnlyEffects(
            PlayerRunState player)
        {
            player.PersistentStatusEffects.RemoveAll(
                status =>
                    status == null
                    || status.RemainingDuration <= 0
                    || (StatusEffectKind)status.EffectKind
                        == StatusEffectKind.ExtraAction);
        }

        private static int CalculateTickAmount(
            PersistentStatusEffectState status)
        {
            long magnitude =
                Math.Abs(
                    (long)status.AppliedValue);

            long stack =
                Math.Max(
                    1,
                    status.StackCount);

            long total =
                magnitude * stack;

            return total >= int.MaxValue
                ? int.MaxValue
                : (int)total;
        }
    }
}
