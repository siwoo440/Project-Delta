using NUnit.Framework; // NUnit 테스트 기능
using ProjectDelta.Application; // 상태 적용 서비스 기능
using ProjectDelta.Data; // 상태 중첩 규칙

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class StatusEffectApplicationServiceTests // 63일차 상태 규칙 테스트
    {
        [Test] // 테스트 표시
        public void CalculateFinalSuccessChance_UsesDesignFormula() // 기본 공식 검증
        {
            int chance = StatusEffectApplicationService.CalculateFinalSuccessChance(60, 10, 20, 5); // 60 + 10 - 20 + 5 계산
            Assert.AreEqual(55, chance); // 계산 결과 확인
        }

        [Test] // 테스트 표시
        public void CalculateFinalSuccessChance_ClampsToFivePercent() // 최소 성공률 검증
        {
            int chance = StatusEffectApplicationService.CalculateFinalSuccessChance(0, 0, 999, 0); // 극단적 저항값 입력
            Assert.AreEqual(5, chance); // 최소 5% 확인
        }

        [Test] // 테스트 표시
        public void CalculateFinalSuccessChance_ClampsToNinetyFivePercent() // 최대 성공률 검증
        {
            int chance = StatusEffectApplicationService.CalculateFinalSuccessChance(100, 999, 0, 0); // 극단적 보정값 입력
            Assert.AreEqual(95, chance); // 최대 95% 확인
        }

        [TestCase(5, StatusSuccessLevel.Low)] // 낮음 시작 경계
        [TestCase(34, StatusSuccessLevel.Low)] // 낮음 끝 경계
        [TestCase(35, StatusSuccessLevel.Normal)] // 보통 시작 경계
        [TestCase(69, StatusSuccessLevel.Normal)] // 보통 끝 경계
        [TestCase(70, StatusSuccessLevel.High)] // 높음 시작 경계
        [TestCase(95, StatusSuccessLevel.High)] // 높음 끝 경계
        public void GetSuccessLevel_UsesDesignThresholds(int chance, StatusSuccessLevel expected) // 표시 단계 경계 검증
        {
            StatusSuccessLevel level = StatusEffectApplicationService.GetSuccessLevel(chance); // 표시 단계 계산
            Assert.AreEqual(expected, level); // 예상 단계 확인
        }

        [Test] // 테스트 표시
        public void TryApply_FailedRoll_DoesNotAddStatus() // 실패 시 미적용 검증
        {
            BattleParticipant target = CreateTarget(0); // 저항 0 대상 생성
            FixedRandomSource random = new FixedRandomSource(96); // 96 굴림 고정
            StatusEffectApplyResult result = StatusEffectApplicationService.TryApply(target, "SE001", "MON_A", 2, -3, StatusEffectKind.DamageOverTime, StatusStackRule.Stack, 3, 95, 0, 0, random); // 95% 상태 적용 시도
            Assert.IsFalse(result.Succeeded); // 실패 결과 확인
            Assert.AreEqual(0, target.StatusEffects.Count); // 상태 미추가 확인
        }

        [Test] // 테스트 표시
        public void TryApply_NewStatus_AddsOneStack() // 최초 부여 검증
        {
            BattleParticipant target = CreateTarget(0); // 저항 0 대상 생성
            FixedRandomSource random = new FixedRandomSource(1); // 성공 굴림 고정
            StatusEffectApplyResult result = StatusEffectApplicationService.TryApply(target, "SE001", "MON_A", 2, -3, StatusEffectKind.DamageOverTime, StatusStackRule.Stack, 3, 80, 0, 0, random); // 중독 최초 적용
            Assert.IsTrue(result.Succeeded); // 성공 결과 확인
            Assert.AreEqual(1, target.StatusEffects.Count); // 상태 한 건 확인
            Assert.AreEqual(1, target.StatusEffects[0].StackCount); // 최초 1중첩 확인
            Assert.AreEqual(2, target.StatusEffects[0].RemainingRounds); // 지속시간 확인
        }

        [Test] // 테스트 표시
        public void TryApply_RefreshDuration_DoesNotIncreaseStack() // 시간 갱신 규칙 검증
        {
            BattleParticipant target = CreateTarget(0); // 저항 0 대상 생성
            target.AddStatusEffect(new StatusEffectInstance("SE003", "MON_A", 1, 1, 0, StatusEffectKind.Neutral)); // 약화 기존 상태 추가
            FixedRandomSource random = new FixedRandomSource(1); // 성공 굴림 고정
            StatusEffectApplicationService.TryApply(target, "SE003", "MON_A", 3, 0, StatusEffectKind.Neutral, StatusStackRule.RefreshDuration, 1, 80, 0, 0, random); // 약화 재부여
            Assert.AreEqual(1, target.StatusEffects.Count); // 상태 개수 유지 확인
            Assert.AreEqual(1, target.StatusEffects[0].StackCount); // 중첩 증가 없음 확인
            Assert.AreEqual(3, target.StatusEffects[0].RemainingRounds); // 지속시간 갱신 확인
        }

        [Test] // 테스트 표시
        public void TryApply_StackRule_StopsAtThreeAndRefreshesDuration() // 중독 출혈 최대 중첩 검증
        {
            BattleParticipant target = CreateTarget(0); // 저항 0 대상 생성
            target.AddStatusEffect(new StatusEffectInstance("SE001", "MON_A", 1, 2, -3, StatusEffectKind.DamageOverTime)); // 중독 2중첩 상태 추가
            FixedRandomSource random = new FixedRandomSource(1); // 성공 굴림 고정
            StatusEffectApplicationService.TryApply(target, "SE001", "MON_A", 2, -3, StatusEffectKind.DamageOverTime, StatusStackRule.Stack, 3, 80, 0, 0, random); // 3중첩 적용
            StatusEffectApplicationService.TryApply(target, "SE001", "MON_A", 4, -3, StatusEffectKind.DamageOverTime, StatusStackRule.Stack, 3, 80, 0, 0, random); // 상한 이후 재부여
            Assert.AreEqual(3, target.StatusEffects[0].StackCount); // 최대 3중첩 확인
            Assert.AreEqual(4, target.StatusEffects[0].RemainingRounds); // 상한에서도 시간 갱신 확인
        }

        [Test] // 테스트 표시
        public void TryApply_NoStack_DoesNotRefreshExistingStatus() // 기절 중첩 불가 검증
        {
            BattleParticipant target = CreateTarget(0); // 저항 0 대상 생성
            target.AddStatusEffect(new StatusEffectInstance("SE005", "MON_A", 1, 1, 0, StatusEffectKind.Stun)); // 기절 기존 상태 추가
            FixedRandomSource random = new FixedRandomSource(1); // 성공 굴림 고정
            StatusEffectApplicationService.TryApply(target, "SE005", "MON_A", 3, 0, StatusEffectKind.Stun, StatusStackRule.NoStack, 1, 80, 0, 0, random); // 기절 재부여 시도
            Assert.AreEqual(1, target.StatusEffects.Count); // 상태 개수 유지 확인
            Assert.AreEqual(1, target.StatusEffects[0].StackCount); // 중첩 증가 없음 확인
            Assert.AreEqual(1, target.StatusEffects[0].RemainingRounds); // 지속시간 갱신 없음 확인
        }

        [Test] // 테스트 표시
        public void TryApply_UsesTargetResistanceInChance() // 대상 저항 반영 검증
        {
            BattleParticipant target = CreateTarget(30); // 저항 30 대상 생성
            FixedRandomSource random = new FixedRandomSource(50); // 50 굴림 고정
            StatusEffectApplyResult result = StatusEffectApplicationService.TryApply(target, "SE003", "MON_A", 2, 0, StatusEffectKind.Neutral, StatusStackRule.RefreshDuration, 1, 70, 0, 0, random); // 최종 40% 상태 적용 시도
            Assert.AreEqual(40, result.FinalSuccessChance); // 저항 차감 결과 확인
            Assert.AreEqual(StatusSuccessLevel.Normal, result.SuccessLevel); // 40% 보통 표시 확인
            Assert.IsFalse(result.Succeeded); // 50 굴림으로 실패 확인
        }

        private static BattleParticipant CreateTarget(int resistance) // 테스트 대상 생성
        {
            return new BattleParticipant("PLAYER", "PLAYER", BattleTeam.Player, 20, 5, 6, 3, 90, 10, 0, resistance); // 기본 전투 참가자 반환
        }

        private sealed class FixedRandomSource : IRandomSource // 고정 난수 테스트 대역
        {
            private readonly int value; // 반환할 고정값

            public FixedRandomSource(int value) // 고정 난수 생성자
            {
                this.value = value; // 고정값 저장
            }

            public int NextInt(int minInclusive, int maxExclusive) // 난수 인터페이스 구현
            {
                return value; // 지정한 고정값 반환
            }
        }
    }
}
