using NUnit.Framework;
using ProjectDelta.Presentation;

namespace ProjectDelta.Tests.EditMode
{
    // 88일차: 화면 암전 전환의 알파 계산이 시작·중간·종료 구간에서 올바른지 검증한다.
    public sealed class BattleTransitionControllerTests
    {
        // Fade Out 시작 시 검은 오버레이 알파가 시작값을 유지하는지 확인한다.
        [Test]
        public void EvaluateTransitionAlpha_AtStart_ReturnsStartAlpha()
        {
            float alpha =
                BattleTransitionController.EvaluateTransitionAlpha(
                    0f,
                    1f,
                    0f,
                    0.2f);

            Assert.That(
                alpha,
                Is.EqualTo(
                    0f)
                    .Within(
                        0.0001f));
        }

        // Fade Out 절반 시점에서 알파가 정확히 절반인지 확인한다.
        [Test]
        public void EvaluateTransitionAlpha_Halfway_ReturnsHalfAlpha()
        {
            float alpha =
                BattleTransitionController.EvaluateTransitionAlpha(
                    0f,
                    1f,
                    0.1f,
                    0.2f);

            Assert.That(
                alpha,
                Is.EqualTo(
                    0.5f)
                    .Within(
                        0.0001f));
        }

        // Fade In 종료 시 검은 오버레이가 완전히 투명해지는지 확인한다.
        [Test]
        public void EvaluateTransitionAlpha_FadeInFinished_ReturnsZero()
        {
            float alpha =
                BattleTransitionController.EvaluateTransitionAlpha(
                    1f,
                    0f,
                    0.2f,
                    0.2f);

            Assert.That(
                alpha,
                Is.EqualTo(
                    0f)
                    .Within(
                        0.0001f));
        }

        // 경과 시간이 전환 시간을 넘어가도 목표 알파를 넘지 않는지 확인한다.
        [Test]
        public void EvaluateTransitionAlpha_AfterDuration_ClampsToTarget()
        {
            float alpha =
                BattleTransitionController.EvaluateTransitionAlpha(
                    0f,
                    1f,
                    1f,
                    0.2f);

            Assert.That(
                alpha,
                Is.EqualTo(
                    1f)
                    .Within(
                        0.0001f));
        }

        // 전환 시간이 0이면 나눗셈 없이 즉시 목표 알파를 반환하는지 확인한다.
        [Test]
        public void EvaluateTransitionAlpha_ZeroDuration_ReturnsTargetImmediately()
        {
            float alpha =
                BattleTransitionController.EvaluateTransitionAlpha(
                    0f,
                    1f,
                    0f,
                    0f);

            Assert.That(
                alpha,
                Is.EqualTo(
                    1f)
                    .Within(
                        0.0001f));
        }
    }
}
