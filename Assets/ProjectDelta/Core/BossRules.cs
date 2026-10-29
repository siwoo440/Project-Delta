namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class BossRules // 마왕전의 시험 능력치와 기획 규칙
    { // 클래스 시작
        public const int TrialMaxHealth = 200; // 이전 마왕 자산의 시험 최대 체력
        public const int TrialMaxMana = 40; // 이전 마왕 자산의 시험 최대 마나
        public const int TrialSpeed = 9; // 이전 마왕 자산의 시험 속도
        public const int TrialAttack = 26; // 이전 마왕 자산의 시험 공격력
        public const int TrialDefense = 18; // 이전 마왕 자산의 시험 방어력
        public const int TrialAccuracy = 90; // 이전 마왕 자산의 시험 명중
        public const int TrialEvasion = 10; // 이전 마왕 자산의 시험 회피
        public const int TrialCharm = 15; // 이전 마왕 자산의 시험 매력
        public const int TrialResistance = 30; // 이전 마왕 자산의 시험 저항
        public const int VictoryExperience = 0; // 마왕 일반 승리 경험치 없음
        public const bool CanEscape = false; // 마왕전 도주 불가
        public const bool RequiresSurrenderConfirmation = true; // 항복 전 확인 필수
        public static int Phase(int health) // 시험 체력의 균등한 세 단계 조회
        { // 조회 시작
            if (health < 0 || health > TrialMaxHealth) // 시험 체력 범위 검사
            { // 오류 시작
                return -1; // 잘못된 체력 차단
            } // 오류 끝
            if (health * 3 > TrialMaxHealth * 2) // 최대 체력의 삼분의 이 초과 검사
            { // 첫 단계 시작
                return 1; // 첫 번째 단계
            } // 첫 단계 끝
            if (health * 3 > TrialMaxHealth) // 최대 체력의 삼분의 일 초과 검사
            { // 둘째 단계 시작
                return 2; // 두 번째 단계
            } // 둘째 단계 끝
            return 3; // 영 체력을 포함한 기존 마지막 단계
        } // 조회 끝
    } // 클래스 끝
} // 이름 공간 끝
