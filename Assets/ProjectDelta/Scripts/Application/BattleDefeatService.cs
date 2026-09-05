using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public static class BattleDefeatService
    {
        private static string lastAttackerInstanceId;
        private static string lastAttackerDefinitionId;
        private static bool surrenderPending;

        public static BattleDefeatRecord LastRecord { get; private set; }

        public static string LastAttackerInstanceId =>
            lastAttackerInstanceId;

        public static string LastAttackerDefinitionId =>
            lastAttackerDefinitionId;

        public static void BeginBattle()
        {
            lastAttackerInstanceId =
                null;

            lastAttackerDefinitionId =
                null;

            surrenderPending =
                false;

            LastRecord =
                null;
        }

        public static void RecordAppliedDamage(
            BattleParticipant attacker,
            BattleParticipant target,
            int appliedDamage)
        {
            if (attacker == null
                || target == null
                || appliedDamage <= 0)
            {
                return;
            }

            if (target.Team != BattleTeam.Player)
            {
                return;
            }

            lastAttackerInstanceId =
                attacker.InstanceId;

            lastAttackerDefinitionId =
                attacker.DefinitionId;
        }

        public static void RecordAppliedDamageBySourceId(
            BattleContext context,
            BattleParticipant target,
            string sourceInstanceId,
            int appliedDamage)
        {
            if (target == null
                || appliedDamage <= 0
                || target.Team != BattleTeam.Player)
            {
                return;
            }

            if (string.IsNullOrEmpty(
                    sourceInstanceId))
            {
                return;
            }

            if (context != null
                && context.TryGetParticipant(
                    sourceInstanceId,
                    out BattleParticipant source))
            {
                RecordAppliedDamage(
                    source,
                    target,
                    appliedDamage);

                return;
            }

            lastAttackerInstanceId =
                sourceInstanceId;

            lastAttackerDefinitionId =
                null;
        }

        public static BattleDefeatRecord RecordSurrender(
            int roundNumber)
        {
            surrenderPending =
                true;

            LastRecord =
                new BattleDefeatRecord(
                    BattleDefeatReason.Surrender,
                    null,
                    null,
                    roundNumber);

            return LastRecord;
        }

        public static BattleDefeatRecord RecordEnemyDefeat(
            int roundNumber)
        {
            LastRecord =
                new BattleDefeatRecord(
                    BattleDefeatReason.EnemyAttack,
                    lastAttackerInstanceId,
                    lastAttackerDefinitionId,
                    roundNumber);

            return LastRecord;
        }

        public static void ReturnToTitleAfterDefeat(
            BattleContext context,
            int roundNumber)
        {
            if (!surrenderPending)
            {
                RecordEnemyDefeat(
                    roundNumber);
            }

            surrenderPending =
                false;

            int floorNumber =
                RunContext.Current != null
                    ? RunContext.Current.Dungeon.CurrentFloor
                    : 1;

            DefeatSceneState.Capture(
                LastRecord,
                floorNumber);

            // 132일차: 기획서 7.3절 "패배 기록" - 체력 0이든 항복이든 상대 하나로 귀결된다.
            // 마지막 공격자가 있으면 그걸, 없으면(예: 피해 없이 곧바로 항복) 지금 싸우던
            // 첫 번째 생존 적을 상대로 취급한다.
            string opponentDefinitionId =
                LastRecord?.AttackerDefinitionId;

            if (string.IsNullOrEmpty(
                    opponentDefinitionId)
                && context?.Enemies != null)
            {
                foreach (BattleParticipant enemy in context.Enemies)
                {
                    if (enemy != null)
                    {
                        opponentDefinitionId =
                            enemy.DefinitionId;

                        break;
                    }
                }
            }

            ApplicationFlow.Current?.RecordDefeat(
                opponentDefinitionId);

            ApplicationFlow.Current?.EnterDefeat();
        }
    }
}
