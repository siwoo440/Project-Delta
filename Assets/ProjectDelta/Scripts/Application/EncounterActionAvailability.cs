namespace ProjectDelta.Application // 애플리케이션 네임스페이스
{
    // 45일차: Encounter 행동 선택 가능 여부와 선택 불가 사유를 함께 전달한다.
    public sealed class EncounterActionAvailability
    {
        public bool CanSelect { get; } // 행동 선택 가능 여부
        public string Reason { get; } // 선택 불가 사유

        private EncounterActionAvailability(
            bool canSelect,
            string reason)
        {
            CanSelect =
                canSelect; // 가능 여부 저장

            Reason =
                reason ?? string.Empty; // 사유 저장
        }

        public static EncounterActionAvailability Available()
        {
            return new EncounterActionAvailability(
                true,
                string.Empty); // 선택 가능 결과 생성
        }

        public static EncounterActionAvailability Unavailable(
            string reason)
        {
            return new EncounterActionAvailability(
                false,
                reason); // 선택 불가 결과 생성
        }
    }
}
