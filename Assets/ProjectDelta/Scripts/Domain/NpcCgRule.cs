using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // 133일차: 기획서 7.4절 "NPC 관계 이벤트 CG - NPC 5단계별 전용 CG" - 이미 있는
    // NpcRelationshipRules 단계 경계(34/67/85/100)를 그대로 재사용한다(113일차부터 쓰던
    // 4개 관계 단계 상승 지점과 같은 기준으로 통일 - 별도 CG 전용 수치를 새로 정하지 않는다).
    public static class NpcCgRule
    {
        public static readonly int[] AffinityThresholds =
        {
            34, 67, 85, 100
        };

        public static string BuildCgId(
            string npcId,
            int affinityThreshold)
        {
            return $"{npcId}_CG_{affinityThreshold}";
        }

        public static List<string> GetNewlyUnlockedCgIds(
            string npcId,
            int previousAffinity,
            int newAffinity)
        {
            List<string> unlocked =
                new List<string>();

            for (int i = 0; i < AffinityThresholds.Length; i++)
            {
                int threshold =
                    AffinityThresholds[i];

                if (previousAffinity < threshold
                    && newAffinity >= threshold)
                {
                    unlocked.Add(
                        BuildCgId(
                            npcId,
                            threshold));
                }
            }

            return unlocked;
        }
    }
}
