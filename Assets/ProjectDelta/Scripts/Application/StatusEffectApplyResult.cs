namespace ProjectDelta.Application // 상태 적용 결과 네임스페이스
{
    public sealed class StatusEffectApplyResult // 상태 적용 결과 데이터
    {
        public int FinalSuccessChance { get; } // 최종 성공률
        public StatusSuccessLevel SuccessLevel { get; } // 성공 가능성 단계
        public int Roll { get; } // 실제 확률 굴림값
        public bool Succeeded { get; } // 적용 성공 여부
        public int ActiveStackCount { get; } // 적용 후 중첩 수
        public int RemainingRounds { get; } // 적용 후 남은 라운드

        public StatusEffectApplyResult(int finalSuccessChance, StatusSuccessLevel successLevel, int roll, bool succeeded, int activeStackCount, int remainingRounds) // 결과 생성자
        {
            FinalSuccessChance = finalSuccessChance; // 최종 성공률 저장
            SuccessLevel = successLevel; // 표시 단계 저장
            Roll = roll; // 굴림값 저장
            Succeeded = succeeded; // 성공 여부 저장
            ActiveStackCount = activeStackCount; // 중첩 수 저장
            RemainingRounds = remainingRounds; // 남은 라운드 저장
        }
    }
}
