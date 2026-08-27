using System;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 106일차: 상자 등급별 미믹 확률(일반 8%·고급 12%·희귀 18%)을 굴린다.
    // ChestService(Domain)는 무작위성을 갖지 않도록, 실제 굴림은 이 계층에서만
    // 수행하고 결과(bool)만 ChestService.ResolveMimic에 넘긴다.
    public static class ChestMimicRollService
    {
        public static bool RollIsMimic(
            ChestRarity rarity,
            Random random = null)
        {
            Random rng =
                random
                ?? new Random();

            int chancePercent =
                ChestRarityRules.GetMimicChancePercent(
                    rarity);

            return rng.Next(
                100) < chancePercent;
        }
    }
}
