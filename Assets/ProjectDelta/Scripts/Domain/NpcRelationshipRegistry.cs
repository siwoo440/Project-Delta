using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // 113일차: 고유 NPC별 관계 상태를 플레이 세션 안에서 공유한다.
    // 115일차: 저장/불러오기 시점에 DungeonSaveMapper가 Restore/All을 통해
    // 이 저장소 전체를 RunData와 동기화한다 - 그래서 층 이동·재접속 후에도
    // 호감도·적대 상태가 유지된다.
    public static class NpcRelationshipRegistry
    {
        private static readonly Dictionary<string, NpcRelationshipState> States =
            new Dictionary<string, NpcRelationshipState>();

        public static IReadOnlyDictionary<string, NpcRelationshipState> All =>
            States;

        // 저장 데이터에 있던 상태 하나를 그대로 등록한다(이미 있으면 덮어쓴다).
        public static void Restore(
            string npcId,
            int affinity,
            bool isHostile,
            int encounterCount,
            bool hasBeenRescued)
        {
            if (string.IsNullOrEmpty(
                    npcId))
            {
                return;
            }

            States[npcId] =
                new NpcRelationshipState(
                    npcId,
                    affinity,
                    isHostile,
                    encounterCount,
                    hasBeenRescued);
        }

        public static NpcRelationshipState GetOrCreate(
            string npcId,
            int initialAffinity,
            bool startsHostile)
        {
            string safeNpcId =
                string.IsNullOrEmpty(npcId)
                    ? "NPC_UNKNOWN"
                    : npcId;

            if (States.TryGetValue(
                    safeNpcId,
                    out NpcRelationshipState state))
            {
                return state;
            }

            state =
                new NpcRelationshipState(
                    safeNpcId,
                    initialAffinity,
                    startsHostile);

            States[safeNpcId] =
                state;

            return state;
        }

        public static bool TryGet(
            string npcId,
            out NpcRelationshipState state)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                state =
                    null;

                return false;
            }

            return States.TryGetValue(
                npcId,
                out state);
        }

        public static void Clear()
        {
            States.Clear();
        }
    }
}
