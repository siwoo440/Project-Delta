using System.Collections.Generic; // 전투 참가자와 중복 검사 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public enum BattleDefeatReason // 기록 가능한 일반 전투 패배 원인
    { // 원인 목록 시작
        HealthZero, // 플레이어 체력 소진
        Surrender // 플레이어의 확정된 항복
    } // 원인 목록 끝
    public sealed class BattleDefeatRecord // 실제 패배 원인과 기록 대상
    { // 클래스 시작
        public BattleDefeatReason Reason { get; } // 확정된 패배 원인
        public BattleActor Opponent { get; } // 패배 기록의 실제 상대
        internal BattleDefeatRecord(BattleDefeatReason reason, BattleActor opponent) // 검증된 패배 기록 생성
        { // 생성 시작
            Reason = reason; // 실제 패배 원인 보관
            Opponent = opponent; // 실제 기록 대상 보관
        } // 생성 끝
    } // 클래스 끝
    public static class BattleDefeatRules // 패배 원인과 상대 선택 규칙
    { // 클래스 시작
        public static bool TryCreate(BattleActor player, IReadOnlyList<BattleActor> enemies, bool surrenderConfirmed, BattleActor selectedEnemy, BattleActor lastInfluencer, BattleActor lastDamageSource, out BattleDefeatRecord record) // 확정된 패배 기록 생성
        { // 생성 시작
            record = null; // 기본 기록 없음
            if (player == null || !player.IsPlayer || enemies == null || enemies.Count < 1 || enemies.Count > 3) // 플레이어와 적 수 검사
            { // 참가자 오류 시작
                return false; // 잘못된 전투 구성 차단
            } // 참가자 오류 끝
            var participants = new HashSet<BattleActor>(); // 동일 참가자 중복 검사
            foreach (var enemy in enemies) // 전투 적 목록 순회
            { // 적 검사 시작
                if (enemy == null || enemy.IsPlayer || !participants.Add(enemy)) // 누락과 아군 및 중복 검사
                { // 잘못된 적 시작
                    return false; // 잘못된 참가자 기록 차단
                } // 잘못된 적 끝
            } // 적 검사 끝
            if (lastDamageSource != null && !participants.Contains(lastDamageSource)) // 마지막 피해자의 전투 참가 검사
            { // 대상 오류 시작
                return false; // 전투 밖 피해 원인 차단
            } // 대상 오류 끝
            if (!player.IsAlive) // 플레이어 체력 소진 검사
            { // 체력 소진 시작
                if (lastDamageSource == null) // 마지막 피해 원인 검사
                { // 피해 원인 누락 시작
                    return false; // 근거 없는 패배 대상 차단
                } // 피해 원인 누락 끝
                record = new BattleDefeatRecord(BattleDefeatReason.HealthZero, lastDamageSource); // 실제 피해 원인의 패배 기록
                return true; // 체력 소진 기록 성공
            } // 체력 소진 끝
            if ((selectedEnemy != null && (!participants.Contains(selectedEnemy) || !selectedEnemy.IsAlive)) || (lastInfluencer != null && !participants.Contains(lastInfluencer))) // 항복 대상의 참가와 선택 대상 생존 검사
            { // 항복 대상 오류 시작
                return false; // 외부 참가자와 사망 선택 대상 차단
            } // 항복 대상 오류 끝
            if (!surrenderConfirmed) // 항복 확인 여부 검사
            { // 항복 미확인 시작
                return false; // 확인되지 않은 회차 종료 차단
            } // 항복 미확인 끝
            var opponent = selectedEnemy ?? lastInfluencer; // 선택 대상 우선과 마지막 영향 대상 대체
            if (opponent == null) // 확인된 항복 대상 검사
            { // 대상 부재 시작
                return false; // 근거 없는 임의 대상 차단
            } // 대상 부재 끝
            record = new BattleDefeatRecord(BattleDefeatReason.Surrender, opponent); // 확인된 항복 기록
            return true; // 항복 기록 성공
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
