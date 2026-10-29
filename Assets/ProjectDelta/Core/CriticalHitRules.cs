namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class CriticalHitRules // 무기와 기술의 치명타 공통 판정
    { // 클래스 시작
        public static bool TryApply(int damage, int chance, int roll, int multiplierPercent, out int result, out bool critical) // 설정된 확률과 배율 적용
        { // 판정 시작
            result = 0; // 기본 피해 결과 초기화
            critical = false; // 기본 치명타 상태 초기화
            if (damage < 0 || roll < 0 || roll >= 100 || multiplierPercent < 100) // 피해와 난수 및 배율 검사
            { // 잘못된 입력 시작
                return false; // 잘못된 판정 차단
            } // 잘못된 입력 끝
            var limitedChance = System.Math.Max(0, System.Math.Min(100, chance)); // 확률의 0~100 범위 제한
            critical = roll < limitedChance; // 확률과 난수의 치명타 비교
            var calculated = critical ? (long)damage * multiplierPercent / 100L : damage; // 배율 적용과 소수점 버림
            result = (int)System.Math.Min(int.MaxValue, calculated); // 정수 범위의 최종 피해
            return true; // 정상적인 판정 완료
        } // 판정 끝
    } // 클래스 끝
} // 이름 공간 끝
