using System; // 잘못된 참가자 오류
using System.Collections.Generic; // 전투 참가자 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class FleeRules // 기존 임시 도주 판정
    { // 클래스 시작
        public static int Chance(BattleActor actor, IReadOnlyList<BattleActor> actors) // 살아 있는 상대 평균 속도의 도주 확률
        { // 계산 시작
            if (actor == null || actors == null) // 필수 참가자와 목록 검사
            { // 누락 조건 시작
                throw new ArgumentNullException(actor == null ? nameof(actor) : nameof(actors)); // 잘못된 전투 데이터 차단
            } // 누락 조건 끝
            long total = 0; // 살아 있는 상대 속도 합계
            var count = 0; // 살아 있는 상대 수
            foreach (var other in actors) // 전투 참가자 순회
            { // 상대 검사 시작
                if (other == null || !other.IsAlive || other.IsPlayer == actor.IsPlayer) // 생존한 상대 진영 검사
                { // 상대 아님 조건 시작
                    continue; // 같은 진영과 사망자 제외
                } // 상대 아님 조건 끝
                total += other.Speed; // 상대 속도 합산
                count++; // 상대 수 증가
            } // 상대 검사 끝
            var average = count == 0 ? 0 : total / count; // 기존 정수 평균과 상대 없음 처리
            var chance = 50L + actor.Speed - average; // 기본 확률과 속도 차이
            return (int)Math.Max(5L, Math.Min(95L, chance)); // 기존 최소와 최대 확률 제한
        } // 계산 끝
        public static bool TryFlee(BattleActor actor, IReadOnlyList<BattleActor> actors, int roll, out bool escaped) // 유효한 도주 시도와 결과
        { // 판정 시작
            escaped = false; // 기본 실패 상태
            if (actor == null || !actor.IsAlive || actors == null || roll < 0 || roll >= 100) // 행동자와 난수 검사
            { // 잘못된 시도 조건 시작
                return false; // 유효하지 않은 도주 차단
            } // 잘못된 시도 조건 끝
            escaped = roll < Chance(actor, actors); // 기존 확률 미만 성공 판정
            return true; // 유효한 도주 시도 완료
        } // 판정 끝
    } // 클래스 끝
} // 이름 공간 끝
