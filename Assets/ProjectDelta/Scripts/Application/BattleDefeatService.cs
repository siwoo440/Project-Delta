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

            ApplicationFlow.Current?.EnterDefeat();
        }
    }
}
