namespace ProjectDelta.Application // 전투 응용 네임스페이스
{
    public sealed class BattleDefeatRecord // 패배 기록 데이터
    {
        public BattleDefeatReason Reason { get; } // 패배 원인
        public string AttackerInstanceId { get; } // 마지막 공격자 인스턴스 ID
        public string AttackerDefinitionId { get; } // 마지막 공격자 정의 ID
        public int RoundNumber { get; } // 패배 라운드

        public bool HasAttacker => // 공격자 존재 여부
            !string.IsNullOrEmpty(
                AttackerInstanceId);

        public BattleDefeatRecord( // 패배 기록 생성
            BattleDefeatReason reason, // 패배 원인 입력
            string attackerInstanceId, // 공격자 인스턴스 ID 입력
            string attackerDefinitionId, // 공격자 정의 ID 입력
            int roundNumber) // 패배 라운드 입력
        {
            Reason =
                reason; // 패배 원인 저장

            AttackerInstanceId =
                attackerInstanceId; // 공격자 인스턴스 ID 저장

            AttackerDefinitionId =
                attackerDefinitionId; // 공격자 정의 ID 저장

            RoundNumber =
                roundNumber; // 패배 라운드 저장
        }
    }
}
