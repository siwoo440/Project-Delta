namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class CombatRules // 기본 공격과 승패 규칙
    { // 클래스 시작
        public static int HitChance(CombatFighter attacker, CombatFighter defender) // 기존 명중률 계산
        { // 계산 시작
            if (attacker == null || defender == null) // 공격자와 방어자 검사
            { // 참가자 누락 조건 시작
                return 0; // 잘못된 명중 계산 차단
            } // 참가자 누락 조건 끝
            var raw = 70L + attacker.Accuracy - (long)defender.Evasion / 2; // 기본 명중과 절반 회피 적용
            return (int)System.Math.Max(5L, System.Math.Min(95L, raw)); // 명중률의 5~95% 제한
        } // 계산 끝
        public static int Damage(CombatFighter attacker, CombatFighter defender, int varianceRoll) // 기존 피해 계산
        { // 계산 시작
            if (attacker == null || defender == null) // 공격자와 방어자 검사
            { // 참가자 누락 조건 시작
                return 0; // 잘못된 피해 계산 차단
            } // 참가자 누락 조건 끝
            var roll = System.Math.Max(0, System.Math.Min(10, varianceRoll)); // 편차 난수의 0~10 제한
            var basic = (long)attacker.Attack * 100 / (100L + defender.Defense); // 방어력에 따른 기본 피해
            var varied = basic * (95 + roll) / 100; // 95~105% 무작위 편차
            return (int)System.Math.Min(int.MaxValue, System.Math.Max(1L, varied)); // 최소 피해와 정수 범위 보장
        } // 계산 끝
        public static bool TryAttack(CombatFighter attacker, CombatFighter defender, int hitRoll, int varianceRoll, out int applied) // 명중과 체력 감소 처리
        { // 판정 시작
            applied = 0; // 피해 초기화
            if (attacker == null || defender == null || !attacker.Actor.IsAlive || !defender.Actor.IsAlive || attacker.Actor.IsPlayer == defender.Actor.IsPlayer || hitRoll < 0 || hitRoll >= 100 || varianceRoll < 0 || varianceRoll > 10) // 생존 대상과 난수 범위 검사
            { // 공격 거절 조건 시작
                return false; // 잘못된 공격 차단
            } // 공격 거절 조건 끝
            if (hitRoll < HitChance(attacker, defender)) // 명중률과 난수 비교
            { // 명중 조건 시작
                applied = defender.ApplyDamage(Damage(attacker, defender, varianceRoll)); // 실제 체력 피해 적용
            } // 명중 조건 끝
            return true; // 유효한 공격 처리 완료
        } // 판정 끝
        public static CombatOutcome Outcome(CombatFighter player, CombatFighter enemy) // 전투 승패 검사
        { // 검사 시작
            if (player == null || !player.Actor.IsAlive) // 플레이어 부재 또는 사망 검사
            { // 패배 조건 시작
                return CombatOutcome.Defeat; // 동시 전멸 시에도 패배 우선
            } // 패배 조건 끝
            return enemy == null || !enemy.Actor.IsAlive ? CombatOutcome.Victory : CombatOutcome.Ongoing; // 적 부재 또는 사망 시 승리
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
