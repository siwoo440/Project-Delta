using System;

namespace ProjectDelta.Domain
{
    // 113일차: NPC의 현재 호감도·조우 횟수·적대 여부를 정적 Definition과 분리한다.
    [Serializable]
    public sealed class NpcRelationshipState
    {
        private readonly string npcId;
        private int affinity;
        private int encounterCount;
        private bool isHostile;

        public NpcRelationshipState(
            string npcId,
            int initialAffinity,
            bool startsHostile)
        {
            this.npcId =
                npcId;

            affinity =
                ClampAffinity(
                    initialAffinity);

            isHostile =
                startsHostile;
        }

        public string NpcId => npcId;
        public int Affinity => affinity;
        public int EncounterCount => encounterCount;
        public bool IsHostile => isHostile;
        public NpcRelationshipStage Stage => NpcRelationshipRules.GetStage(affinity);

        public void RegisterEncounter()
        {
            encounterCount++;
        }

        public void ChangeAffinity(
            int delta)
        {
            affinity =
                ClampAffinity(
                    affinity + delta);
        }

        public void SetHostile(
            bool hostile)
        {
            isHostile =
                hostile;
        }

        private static int ClampAffinity(
            int value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 100)
            {
                return 100;
            }

            return value;
        }
    }
}
