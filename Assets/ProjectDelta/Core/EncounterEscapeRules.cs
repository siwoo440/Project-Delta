using System.Collections.Generic; // 조우 참가자 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class EncounterEscapeRules // 조우 단계의 속도 도망 판정
    { // 클래스 시작
        public static bool TryResolve(BattleActor player, IReadOnlyList<BattleActor> actors, int bonus, int playerRoll, int enemyRoll, out bool escaped) // 생존 상대와 두 주사위 비교
        { // 판정 시작
            escaped = false; // 기본 실패 상태
            if (player == null || !player.IsAlive || actors == null || playerRoll < 1 || playerRoll > 20 || enemyRoll < 1 || enemyRoll > 20) // 플레이어와 주사위 검사
            { // 잘못된 판정 조건 시작
                return false; // 유효하지 않은 도망 차단
            } // 잘못된 판정 조건 끝
            var found = false; // 생존 상대 확인 여부
            var fastest = int.MinValue; // 가장 빠른 상대 속도
            foreach (var actor in actors) // 조우 참가자 순회
            { // 상대 검사 시작
                if (actor == null || !actor.IsAlive || actor.IsPlayer == player.IsPlayer) // 생존한 상대 진영 검사
                { // 대상 제외 조건 시작
                    continue; // 같은 진영과 사망자 제외
                } // 대상 제외 조건 끝
                fastest = System.Math.Max(fastest, actor.Speed); // 가장 빠른 생존 상대 선택
                found = true; // 생존 상대 존재 기록
            } // 상대 검사 끝
            if (!found) // 생존 상대 존재 검사
            { // 상대 없음 조건 시작
                return false; // 비교할 상대가 없는 도망 차단
            } // 상대 없음 조건 끝
            escaped = (long)player.Speed + bonus + playerRoll > (long)fastest + enemyRoll; // 동점 실패의 속도 비교
            return true; // 유효한 도망 판정 완료
        } // 판정 끝
    } // 클래스 끝
} // 이름 공간 끝
