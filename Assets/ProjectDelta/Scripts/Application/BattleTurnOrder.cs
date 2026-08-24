using System.Collections.Generic;
using System.Linq;

namespace ProjectDelta.Application
{
    // 48일차: 이번 턴에 행동할 참가자 순서를 Speed 기준으로 계산한다.
    public static class BattleTurnOrder
    {
        // Speed 내림차순으로 정렬한다. 동률일 때는 Player를 먼저, 그다음 적을 슬롯 1→4번 순서로 둔다.
        // (참가자를 Player → 적 슬롯 순으로 먼저 나열한 뒤 안정 정렬을 쓰면 이 우선순위가 그대로 유지된다.)
        // 사망한 참가자(IsAlive == false)는 순서에서 제외한다.
        public static IReadOnlyList<BattleParticipant> Build(
            BattleContext context)
        {
            if (context == null)
            {
                return new List<BattleParticipant>();
            }

            List<BattleParticipant> candidates =
                new List<BattleParticipant>();

            if (context.Player != null
                && context.Player.IsAlive)
            {
                candidates.Add(
                    context.Player);
            }

            if (context.Enemies != null)
            {
                foreach (BattleParticipant enemy in context.Enemies)
                {
                    if (enemy != null
                        && enemy.IsAlive)
                    {
                        candidates.Add(
                            enemy);
                    }
                }
            }

            return candidates
                .OrderByDescending(
                    participant => participant.Speed)
                .ToList();
        }
    }
}
