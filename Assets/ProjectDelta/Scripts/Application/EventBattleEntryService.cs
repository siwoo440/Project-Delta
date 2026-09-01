using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 117일차: 기획서가 요구한 "하나의 Entry API" - 유혹 성공·스킬/몬스터 행동·일반 이벤트·
    // 적대 NPC·상위 개체(보스) 전용 결과, 이 4갈래가 전부 이 메서드 하나로 들어온다.
    // 119일차: 대상을 최대 3명까지 받을 수 있게 확장했다 - 상위 개체(보스)는 stageCounts로
    // 2단계 게이지를 지정할 수 있다(예: [2] = 1명짜리 2단계 보스).
    public static class EventBattleEntryService
    {
        // 117일차 호환용 - 대상 1명, 1단계.
        public static bool TryEnter(
            EventBattleEntrySource source,
            BattleParticipant player,
            BattleParticipant target,
            out EventBattleContext context)
        {
            return TryEnter(
                source,
                player,
                new[] { target },
                new[] { 1 },
                out context);
        }

        // 119일차: 다수 참가자(최대 3명) 진입. stageCounts[i]가 targets[i]의 게이지 단계 수다
        // (지정하지 않거나 부족하면 1단계로 취급).
        public static bool TryEnter(
            EventBattleEntrySource source,
            BattleParticipant player,
            IReadOnlyList<BattleParticipant> targets,
            IReadOnlyList<int> stageCounts,
            out EventBattleContext context)
        {
            context =
                null;

            if (player == null
                || !player.IsAlive
                || targets == null
                || targets.Count == 0
                || targets.Count > EventBattleContext.MaxTargets)
            {
                return false;
            }

            List<EventBattleParticipantState> states =
                new List<EventBattleParticipantState>();

            for (int index = 0; index < targets.Count; index++)
            {
                BattleParticipant target =
                    targets[index];

                if (target == null
                    || !target.IsAlive)
                {
                    return false;
                }

                int stageCount =
                    stageCounts != null
                    && index < stageCounts.Count
                        ? stageCounts[index]
                        : 1;

                states.Add(
                    new EventBattleParticipantState(
                        target,
                        stageCount));
            }

            context =
                new EventBattleContext(
                    source,
                    player,
                    states);

            return true;
        }
    }
}
