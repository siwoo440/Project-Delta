using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 106일차: 상자 등급별 미믹 확률 굴림을 통계적으로 검증한다.
    // System.Random의 내부 구현에 의존하지 않도록 정확한 시퀀스 대신
    // 다회 시행의 분포 경향을 확인한다.
    public sealed class ChestMimicRollServiceTests
    {
        [Test]
        public void RollIsMimic_RareChest_TriggersMoreOftenThanCommonChest()
        {
            System.Random commonRandom =
                new System.Random(
                    2026);

            System.Random rareRandom =
                new System.Random(
                    2026);

            int commonMimicCount =
                0;

            int rareMimicCount =
                0;

            const int trials = 5000;

            for (int trial = 0;
                 trial < trials;
                 trial++)
            {
                if (ChestMimicRollService.RollIsMimic(
                        ChestRarity.Common,
                        commonRandom))
                {
                    commonMimicCount++;
                }

                if (ChestMimicRollService.RollIsMimic(
                        ChestRarity.Rare,
                        rareRandom))
                {
                    rareMimicCount++;
                }
            }

            // 8% vs 18% - 5000회 시행에서는 통계적으로 확실히 차이가 나야 한다.
            Assert.That(
                rareMimicCount,
                Is.GreaterThan(
                    commonMimicCount));
        }

        [Test]
        public void RollIsMimic_CommonChest_RoughlyMatchesEightPercent()
        {
            System.Random random =
                new System.Random(
                    12345);

            int mimicCount =
                0;

            const int trials = 20000;

            for (int trial = 0;
                 trial < trials;
                 trial++)
            {
                if (ChestMimicRollService.RollIsMimic(
                        ChestRarity.Common,
                        random))
                {
                    mimicCount++;
                }
            }

            double observedPercent =
                mimicCount
                * 100.0
                / trials;

            // 기대값 8% 대비 넉넉한 오차 범위(±2.5%p)로 확인한다.
            Assert.That(
                observedPercent,
                Is.InRange(
                    5.5,
                    10.5));
        }
    }
}
