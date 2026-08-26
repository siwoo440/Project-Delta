using System;

namespace ProjectDelta.Application
{
    public static class BattleStatusRestrictionPolicy
    {
        public static bool IsSilenced(
            BattleParticipant participant)
        {
            if (participant == null
                || participant.StatusEffects == null)
            {
                return false;
            }

            for (int index = 0;
                 index < participant.StatusEffects.Count;
                 index++)
            {
                StatusEffectInstance status =
                    participant.StatusEffects[index];

                if (status == null
                    || status.IsExpired
                    || string.IsNullOrEmpty(
                        status.DefinitionId))
                {
                    continue;
                }

                if (status.DefinitionId.IndexOf(
                        "SILENCE",
                        StringComparison.OrdinalIgnoreCase) >= 0
                    || status.DefinitionId.IndexOf(
                        "침묵",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
