using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 47일차: EncounterContext(어떤 방에서 어떤 몬스터를 만났는가)와 분리된,
    // 현재 진행 중인 전투가 어떤 참가자들로 구성되어 있는가를 나타내는 데이터.
    public sealed class BattleContext
    {
        // 47일차: 전투 화면의 적 슬롯은 맨 왼쪽 1번부터 최대 4번까지다.
        // 75일차: 적 최대 인원을 4명으로 확정한다 (BattleSession.TryBeginBattle()에서 강제).
        public const int MaxEnemySlots = 4;

        public BattleParticipant Player { get; }
        public IReadOnlyList<BattleParticipant> Enemies { get; }

        public BattleContext(
            BattleParticipant player,
            IReadOnlyList<BattleParticipant> enemies)
        {
            Player =
                player;

            Enemies =
                enemies;
        }

        // 47일차: 슬롯 번호(0 = 맨 왼쪽)로 적을 찾는다. 빈 슬롯이면 false를 반환한다.
        public bool TryGetEnemyAtSlot(
            int slotIndex,
            out BattleParticipant enemy)
        {
            enemy =
                null;

            if (Enemies == null
                || slotIndex < 0
                || slotIndex >= MaxEnemySlots
                || slotIndex >= Enemies.Count)
            {
                return false;
            }

            enemy =
                Enemies[slotIndex];

            return enemy != null;
        }

        public bool TryGetParticipant(
            string instanceId,
            out BattleParticipant participant)
        {
            participant =
                null;

            if (string.IsNullOrEmpty(instanceId))
            {
                return false;
            }

            if (Player != null
                && Player.InstanceId == instanceId)
            {
                participant =
                    Player;

                return true;
            }

            if (Enemies == null)
            {
                return false;
            }

            foreach (BattleParticipant enemy in Enemies)
            {
                if (enemy != null
                    && enemy.InstanceId == instanceId)
                {
                    participant =
                        enemy;

                    return true;
                }
            }

            return false;
        }
    }
}
