using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // CombatRng 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class CombatRngTests
    {
        [Test]
        public void NextInt_AlwaysStaysWithinRequestedRange()
        {
            IRandomSource rng =
                new CombatRng(
                    12345);

            for (int i = 0; i < 200; i++)
            {
                int value =
                    rng.NextInt(
                        0,
                        11); // BattleDamageCalculator.DamageVarianceRollCount와 동일한 범위

                Assert.GreaterOrEqual(
                    value,
                    0);

                Assert.Less(
                    value,
                    11);
            }
        }

        [Test]
        public void NextInt_SameSeed_ProducesSameSequence()
        {
            // 59일차: 시드가 같으면 항상 같은 결과가 나와야 한다 (기획서 9.3의 "저장 후 재현" 전제).
            IRandomSource first =
                new CombatRng(
                    777);

            IRandomSource second =
                new CombatRng(
                    777);

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(
                    first.NextInt(0, 100),
                    second.NextInt(0, 100));
            }
        }

        [Test]
        public void NextInt_DifferentSeeds_ProduceDifferentSequences()
        {
            IRandomSource first =
                new CombatRng(
                    1);

            IRandomSource second =
                new CombatRng(
                    2);

            bool foundDifference = false;

            for (int i = 0; i < 20; i++)
            {
                if (first.NextInt(0, 1000000) != second.NextInt(0, 1000000))
                {
                    foundDifference = true;
                    break;
                }
            }

            Assert.IsTrue(
                foundDifference); // 서로 다른 시드가 우연히 20번 모두 같은 값을 낼 확률은 무시 가능한 수준
        }
    }
}
