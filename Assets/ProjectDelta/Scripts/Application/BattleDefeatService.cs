namespace ProjectDelta.Application // 전투 응용 네임스페이스
{
    public static class BattleDefeatService // 패배 추적 서비스
    {
        private static string lastAttackerInstanceId; // 마지막 공격자 인스턴스 ID
        private static string lastAttackerDefinitionId; // 마지막 공격자 정의 ID
        private static bool surrenderPending; // 항복 확정 상태

        public static BattleDefeatRecord LastRecord { get; private set; } // 최근 패배 기록

        public static string LastAttackerInstanceId => // 마지막 공격자 인스턴스 ID 조회
            lastAttackerInstanceId;

        public static string LastAttackerDefinitionId => // 마지막 공격자 정의 ID 조회
            lastAttackerDefinitionId;

        public static void BeginBattle() // 새 전투 추적 시작
        {
            lastAttackerInstanceId =
                null; // 이전 공격자 제거

            lastAttackerDefinitionId =
                null; // 이전 공격자 정의 제거

            surrenderPending =
                false; // 이전 항복 상태 제거

            LastRecord =
                null; // 이전 패배 기록 제거
        }

        public static void RecordAppliedDamage( // 실제 피해 공격자 기록
            BattleParticipant attacker, // 공격자 입력
            BattleParticipant target, // 피해 대상 입력
            int appliedDamage) // 실제 적용 피해량 입력
        {
            if (attacker == null
                || target == null
                || appliedDamage <= 0) // 유효 피해 확인
            {
                return;
            }

            if (target.Team != BattleTeam.Player) // 플레이어 피해 여부 확인
            {
                return;
            }

            lastAttackerInstanceId =
                attacker.InstanceId; // 공격자 인스턴스 ID 저장

            lastAttackerDefinitionId =
                attacker.DefinitionId; // 공격자 정의 ID 저장
        }

        public static void RecordAppliedDamageBySourceId( // 상태 효과 피해 공격자 기록
            BattleContext context, // 현재 전투 정보 입력
            BattleParticipant target, // 피해 대상 입력
            string sourceInstanceId, // 상태 효과 원본 ID 입력
            int appliedDamage) // 실제 적용 피해량 입력
        {
            if (target == null
                || appliedDamage <= 0
                || target.Team != BattleTeam.Player) // 유효 플레이어 피해 확인
            {
                return;
            }

            if (string.IsNullOrEmpty(
                    sourceInstanceId)) // 원본 ID 존재 확인
            {
                return;
            }

            if (context != null
                && context.TryGetParticipant(
                    sourceInstanceId,
                    out BattleParticipant source)) // 전투 참가자 조회
            {
                RecordAppliedDamage(
                    source,
                    target,
                    appliedDamage); // 참가자 기반 공격자 기록

                return;
            }

            lastAttackerInstanceId =
                sourceInstanceId; // 미조회 원본 ID 보존

            lastAttackerDefinitionId =
                null; // 정의 ID 미확인 처리
        }

        public static BattleDefeatRecord RecordSurrender( // 항복 패배 기록
            int roundNumber) // 현재 라운드 입력
        {
            surrenderPending =
                true; // 항복 확정 표시

            LastRecord =
                new BattleDefeatRecord(
                    BattleDefeatReason.Surrender,
                    null,
                    null,
                    roundNumber); // 항복 기록 생성

            return LastRecord;
        }

        public static BattleDefeatRecord RecordEnemyDefeat( // 적 공격 패배 기록
            int roundNumber) // 현재 라운드 입력
        {
            LastRecord =
                new BattleDefeatRecord(
                    BattleDefeatReason.EnemyAttack,
                    lastAttackerInstanceId,
                    lastAttackerDefinitionId,
                    roundNumber); // 마지막 공격자 포함 기록 생성

            return LastRecord;
        }

        public static void ReturnToTitleAfterDefeat( // 패배 기록 후 임시 타이틀 복귀
            BattleContext context, // 현재 전투 정보 입력
            int roundNumber) // 현재 라운드 입력
        {
            if (!surrenderPending) // 일반 전투 패배 여부 확인
            {
                RecordEnemyDefeat(
                    roundNumber); // 일반 패배 기록 생성
            }

            surrenderPending =
                false; // 항복 대기 상태 해제

            ApplicationFlow.Current?.ReturnToTitle(); // 기존 임시 타이틀 복귀 실행
        }
    }
}
