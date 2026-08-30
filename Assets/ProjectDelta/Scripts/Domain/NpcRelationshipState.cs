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
        private bool hasBeenRescued;

        public NpcRelationshipState(
            string npcId,
            int initialAffinity,
            bool startsHostile)
            : this(
                npcId,
                initialAffinity,
                startsHostile,
                0,
                false)
        {
        }

        // 115일차: 저장 데이터 복원용 - 조우 횟수·구조 여부까지 그대로 되살린다.
        public NpcRelationshipState(
            string npcId,
            int initialAffinity,
            bool startsHostile,
            int savedEncounterCount,
            bool savedHasBeenRescued)
        {
            this.npcId =
                npcId;

            affinity =
                ClampAffinity(
                    initialAffinity);

            isHostile =
                startsHostile;

            encounterCount =
                savedEncounterCount < 0
                    ? 0
                    : savedEncounterCount;

            hasBeenRescued =
                savedHasBeenRescued;
        }

        public string NpcId => npcId;
        public int Affinity => affinity;
        public int EncounterCount => encounterCount;
        public bool IsHostile => isHostile;
        public bool HasBeenRescued => hasBeenRescued;
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

        // 115일차: "구조"는 NPC 한 명당 한 번만 허용한다.
        public void MarkRescued()
        {
            hasBeenRescued =
                true;
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
