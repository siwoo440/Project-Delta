using System;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 83일차: 도주 후 남아야 하는 플레이어 상태이상을 전투와 탐험 런 상태 사이에서 변환한다.
    public static class PersistentPlayerStatusService
    {
        public static void RestoreToBattleAndClear(
            PlayerRunState runState,
            BattleParticipant player)
        {
            if (runState == null
                || player == null)
            {
                return;
            }

            for (int index = 0;
                 index < runState.PersistentStatusEffects.Count;
                 index++)
            {
                PersistentStatusEffectState saved =
                    runState.PersistentStatusEffects[index];

                if (!IsRestorable(saved))
                {
                    continue;
                }

                player.AddStatusEffect(
                    new StatusEffectInstance(
                        saved.DefinitionId,
                        saved.SourceInstanceId,
                        Math.Max(1, saved.RemainingDuration),
                        Math.Max(1, saved.StackCount),
                        saved.AppliedValue,
                        (StatusEffectKind)saved.EffectKind,
                        (BattleStatType)saved.TargetStat));
            }

            runState.PersistentStatusEffects.Clear();
            runState.StatusEffects.Clear();
        }

        public static void CaptureFromBattleAfterEscape(
            BattleParticipant player,
            PlayerRunState runState)
        {
            if (runState == null)
            {
                return;
            }

            runState.PersistentStatusEffects.Clear();
            runState.StatusEffects.Clear();

            if (player == null
                || player.StatusEffects == null)
            {
                return;
            }

            for (int index = 0;
                 index < player.StatusEffects.Count;
                 index++)
            {
                StatusEffectInstance status =
                    player.StatusEffects[index];

                if (status == null
                    || status.IsExpired
                    || status.EffectKind == StatusEffectKind.ExtraAction)
                {
                    continue;
                }

                PersistentStatusEffectState saved =
                    new PersistentStatusEffectState
                    {
                        DefinitionId = status.DefinitionId,
                        SourceInstanceId = status.SourceInstanceId,
                        RemainingDuration = Math.Max(1, status.RemainingRounds),
                        StackCount = Math.Max(1, status.StackCount),
                        AppliedValue = status.AppliedValue,
                        EffectKind = (int)status.EffectKind,
                        TargetStat = (int)status.TargetStat
                    };

                runState.PersistentStatusEffects.Add(
                    saved);

                if (!string.IsNullOrEmpty(
                        saved.DefinitionId))
                {
                    runState.StatusEffects.Add(
                        saved.DefinitionId);
                }
            }
        }

        public static void ClearPersistentEffects(
            PlayerRunState runState)
        {
            if (runState == null)
            {
                return;
            }

            runState.PersistentStatusEffects.Clear();
            runState.StatusEffects.Clear();
        }

        private static bool IsRestorable(
            PersistentStatusEffectState saved)
        {
            if (saved == null
                || string.IsNullOrEmpty(
                    saved.DefinitionId)
                || saved.RemainingDuration <= 0)
            {
                return false;
            }

            return (StatusEffectKind)saved.EffectKind
                != StatusEffectKind.ExtraAction;
        }
    }
}
