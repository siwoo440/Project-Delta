namespace ProjectDelta.Domain
{
    public static class NpcRelationshipRules
    {
        public static NpcRelationshipStage GetStage(
            int affinity)
        {
            int safeAffinity =
                affinity < 0
                    ? 0
                    : affinity > 100
                        ? 100
                        : affinity;

            if (safeAffinity >= 100)
            {
                return NpcRelationshipStage.EndingAvailable;
            }

            if (safeAffinity >= 85)
            {
                return NpcRelationshipStage.Special;
            }

            if (safeAffinity >= 67)
            {
                return NpcRelationshipStage.Trust;
            }

            if (safeAffinity >= 34)
            {
                return NpcRelationshipStage.Interest;
            }

            return NpcRelationshipStage.Neutral;
        }
    }
}
