using System;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 110일차: 함정 방의 회피 확률·피해량을 굴린다. 정확한 수치는 임의로 정했고,
    // 실제 콘텐츠 제작 단계에서 조정될 값이다.
    public static class RoomTrapRollService
    {
        // 기본 회피 확률(%). 회피 스탯 1당 1%p씩 가산하고 95%를 넘지 않게 한다.
        private const int BaseAvoidChancePercent = 20;
        private const int MaxAvoidChancePercent = 95;

        private const int MinDamage = 8;
        private const int MaxDamage = 15;

        public static bool RollAvoided(
            PlayerRunState player,
            Random random = null)
        {
            Random rng =
                random
                ?? new Random();

            int evasion =
                player != null
                    ? player.GetFinalStats().Evasion
                    : 0;

            int chancePercent =
                BaseAvoidChancePercent
                + evasion;

            if (chancePercent > MaxAvoidChancePercent)
            {
                chancePercent =
                    MaxAvoidChancePercent;
            }

            if (chancePercent < 0)
            {
                chancePercent =
                    0;
            }

            return rng.Next(
                100) < chancePercent;
        }

        public static int RollDamage(
            Random random = null)
        {
            Random rng =
                random
                ?? new Random();

            return rng.Next(
                MinDamage,
                MaxDamage + 1);
        }
    }
}
