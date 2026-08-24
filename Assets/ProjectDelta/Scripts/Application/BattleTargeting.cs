using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 49일차: 행동자 진영에 따라 선택 가능한 대상을 계산한다.
    public static class BattleTargeting
    {
        // actor가 선택할 수 있는 살아있는 상대 진영 참가자 목록을 반환한다.
        public static IReadOnlyList<BattleParticipant> GetValidTargets(
            BattleContext context,
            BattleParticipant actor)
        {
            List<BattleParticipant> targets =
                new List<BattleParticipant>();

            if (context == null
                || actor == null)
            {
                return targets;
            }

            if (actor.Team == BattleTeam.Player)
            {
                if (context.Enemies != null)
                {
                    foreach (BattleParticipant enemy in context.Enemies)
                    {
                        if (enemy != null
                            && enemy.IsAlive)
                        {
                            targets.Add(
                                enemy);
                        }
                    }
                }

                return targets;
            }

            // Enemy 차례 → 아직 아군이 Player 1명뿐이므로 Player만 대상이 될 수 있다.
            if (context.Player != null
                && context.Player.IsAlive)
            {
                targets.Add(
                    context.Player);
            }

            return targets;
        }

        // target이 actor가 지금 선택할 수 있는 대상인지 확인한다.
        public static bool IsValidTarget(
            BattleContext context,
            BattleParticipant actor,
            BattleParticipant target)
        {
            if (actor == null
                || target == null
                || !target.IsAlive) // 대상 생존 여부 확인
            {
                return false;
            }

            if (target.Team == actor.Team) // 아군 오폭 금지
            {
                return false;
            }

            if (context == null
                || !context.TryGetParticipant(
                    target.InstanceId,
                    out BattleParticipant resolved)
                || resolved != target) // 현재 Battle 소속 참가자인지 확인
            {
                return false;
            }

            return true;
        }
    }
}
