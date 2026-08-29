using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // 113일차: 세이브 구조 연결 전까지 고유 NPC별 관계 상태를 플레이 세션 안에서 공유한다.
    // 115일차 관계 저장 구현 시 이 저장소의 내용을 영구 저장 데이터로 옮긴다.
    public static class NpcRelationshipRegistry
    {
        private static readonly Dictionary<string, NpcRelationshipState> States =
            new Dictionary<string, NpcRelationshipState>();

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
