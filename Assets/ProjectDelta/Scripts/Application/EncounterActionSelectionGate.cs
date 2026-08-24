namespace ProjectDelta.Application // 애플리케이션 네임스페이스
{
    // 45일차: 한 Encounter에서 행동 Command가 한 번만 확정되도록 선택 상태를 관리한다.
    public sealed class EncounterActionSelectionGate
    {
        public bool HasSelection { get; private set; } // 행동 확정 여부
        public string SelectedCommandId { get; private set; } // 확정된 Command ID

        public EncounterActionAvailability Evaluate(
            EncounterState state,
            EncounterContext context)
        {
            if (state != EncounterState.Active) // 행동 선택 가능한 상태 확인
            {
                return EncounterActionAvailability.Unavailable(
                    "현재 행동을 선택할 수 있는 Encounter 상태가 아닙니다."); // 상태 불일치 사유 반환
            }

            if (context == null) // Encounter 대상 정보 확인
            {
                return EncounterActionAvailability.Unavailable(
                    "현재 Encounter 대상 정보가 없습니다."); // 대상 누락 사유 반환
            }

            if (HasSelection) // 기존 행동 확정 여부 확인
            {
                return EncounterActionAvailability.Unavailable(
                    "이미 행동을 선택했습니다."); // 중복 입력 사유 반환
            }

            return EncounterActionAvailability.Available(); // 선택 가능 결과 반환
        }

        public bool TryCommit(
            string commandId)
        {
            if (HasSelection
                || string.IsNullOrEmpty(commandId)) // 중복 또는 잘못된 ID 확인
            {
                return false; // 선택 확정 거부
            }

            HasSelection =
                true; // 행동 확정 상태 저장

            SelectedCommandId =
                commandId; // 선택한 Command ID 저장

            return true; // 선택 확정 성공
        }

        public void Reset()
        {
            HasSelection =
                false; // 행동 확정 상태 초기화

            SelectedCommandId =
                null; // 선택 Command ID 초기화
        }
    }
}
