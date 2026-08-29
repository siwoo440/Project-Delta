using System;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 110~111일차: 새로 생성된 방의 종류를 굴린다. 가중치는 임의로 정했다.
    // 111일차부터 Combat(몬스터 조우)/Event(이벤트 화면)가 각각
    // RoomEncounterPlacementService/RoomEventTriggerController에 연결되어
    // 한 방에 두 시스템이 동시에 배정될 걱정 없이 함께 굴릴 수 있게 됐다.
    public static class RoomTypeRollService
    {
        private const int TrapChancePercent = 15;
        private const int CombatChancePercent = 25;
        private const int EventChancePercent = 15;
        // 나머지(45%)는 Normal.

        public static RoomType Roll(
            Random random = null)
        {
            Random rng =
                random
                ?? new Random();

            int roll =
                rng.Next(
                    100);

            if (roll < TrapChancePercent)
            {
                return RoomType.Trap;
            }

            if (roll < TrapChancePercent + CombatChancePercent)
            {
                return RoomType.Combat;
            }

            if (roll < TrapChancePercent + CombatChancePercent + EventChancePercent)
            {
                return RoomType.Event;
            }

            return RoomType.Normal;
        }
    }
}
