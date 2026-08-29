using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 110일차: 방 종류 굴림과 함정 회피/피해 굴림을 통계적으로 검증한다.
    public sealed class RoomTypeRollServiceTests
    {
        [Test]
        public void RoomTypeRules_GetDisplayName_ReturnsDistinctKoreanNames()
        {
            Assert.That(
                RoomTypeRules.GetDisplayName(
                    RoomType.Normal),
                Is.EqualTo("일반"));

            Assert.That(
                RoomTypeRules.GetDisplayName(
                    RoomType.Combat),
                Is.EqualTo("전투"));

            Assert.That(
                RoomTypeRules.GetDisplayName(
                    RoomType.Event),
                Is.EqualTo("이벤트"));

            Assert.That(
                RoomTypeRules.GetDisplayName(
                    RoomType.Trap),
                Is.EqualTo("함정"));
        }

        [Test]
        public void RoomTypeRoll_ManyTrials_OnlyReturnsNormalOrTrap()
        {
            System.Random random =
                new System.Random(
                    2026);

            for (int trial = 0;
                 trial < 1000;
                 trial++)
            {
                RoomType roomType =
                    RoomTypeRollService.Roll(
                        random);

                Assert.That(
                    roomType == RoomType.Normal
                    || roomType == RoomType.Trap,
                    Is.True);
            }
        }

        [Test]
        public void RoomTypeRoll_ManyTrials_NormalIsMoreFrequentThanTrap()
        {
            System.Random random =
                new System.Random(
                    777);

            int normalCount =
                0;

            int trapCount =
                0;

            for (int trial = 0;
                 trial < 2000;
                 trial++)
            {
                if (RoomTypeRollService.Roll(
                        random)
                    == RoomType.Trap)
                {
                    trapCount++;
                }
                else
                {
                    normalCount++;
                }
            }

            Assert.That(
                normalCount,
                Is.GreaterThan(
                    trapCount));
        }

        [Test]
        public void RollDamage_ManyTrials_StaysWithinExpectedRange()
        {
            System.Random random =
                new System.Random(
                    12345);

            for (int trial = 0;
                 trial < 500;
                 trial++)
            {
                int damage =
                    RoomTrapRollService.RollDamage(
                        random);

                Assert.That(
                    damage,
                    Is.InRange(
                        8,
                        15));
            }
        }

        [Test]
        public void RollAvoided_HigherEvasion_AvoidsMoreOftenThanZeroEvasion()
        {
            PlayerRunState lowEvasionPlayer =
                PlayerRunState.CreateDefault();

            lowEvasionPlayer.BaseStats.Evasion =
                0;

            PlayerRunState highEvasionPlayer =
                PlayerRunState.CreateDefault();

            highEvasionPlayer.BaseStats.Evasion =
                60;

            System.Random lowRandom =
                new System.Random(
                    99);

            System.Random highRandom =
                new System.Random(
                    99);

            int lowAvoidCount =
                0;

            int highAvoidCount =
                0;

            const int trials = 2000;

            for (int trial = 0;
                 trial < trials;
                 trial++)
            {
                if (RoomTrapRollService.RollAvoided(
                        lowEvasionPlayer,
                        lowRandom))
                {
                    lowAvoidCount++;
                }

                if (RoomTrapRollService.RollAvoided(
                        highEvasionPlayer,
                        highRandom))
                {
                    highAvoidCount++;
                }
            }

            Assert.That(
                highAvoidCount,
                Is.GreaterThan(
                    lowAvoidCount));
        }
    }
}
