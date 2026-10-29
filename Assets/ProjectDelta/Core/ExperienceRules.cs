namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class ExperienceProgress // 검증된 경험치 반영 결과
    { // 클래스 시작
        public int Level { get; } // 반영 후 레벨
        public int CurrentExperience { get; } // 현재 레벨의 진행 경험치
        public int AppliedExperience { get; } // 최대 레벨까지 실제 반영한 경험치
        public int GainedLevels { get; } // 이번 획득에서 상승한 레벨 수
        public int GainedStatPoints { get; } // 이번 획득의 레벨별 스탯 포인트
        internal ExperienceProgress(int level, int currentExperience, int appliedExperience, int gainedLevels, int gainedStatPoints) // 검증된 성장 결과 생성
        { // 생성 시작
            Level = level; // 반영 후 레벨 보관
            CurrentExperience = currentExperience; // 다음 레벨의 잔여 경험치 보관
            AppliedExperience = appliedExperience; // 실제 반영 경험치 보관
            GainedLevels = gainedLevels; // 상승한 레벨 수 보관
            GainedStatPoints = gainedStatPoints; // 레벨별 추가 포인트 보관
        } // 생성 끝
    } // 클래스 끝
    public static class ExperienceRules // 현재 시험 층의 경험치와 레벨 계산
    { // 클래스 시작
        public const int MaxLevel = 10; // 기획의 최대 레벨
        private static readonly int[] StatPointsByLevel = { 0, 0, 1, 1, 1, 2, 1, 1, 1, 2, 3 }; // 도달 레벨별 추가 포인트
        public static bool TryCalculateVictory(int floor, int normalEnemies, int eliteEnemies, out int experience) // 일반 전투의 1·2층 적별 경험치 합산
        { // 계산 시작
            experience = 0; // 기본 경험치 없음
            var totalEnemies = (long)normalEnemies + eliteEnemies; // 정수 범위 보호의 전체 적 수
            if ((floor < 1 || floor > 5) || normalEnemies < 0 || eliteEnemies < 0 || totalEnemies < 1 || totalEnemies > 3) // 시험 층과 적 수 검사
            { // 입력 거절 시작
                return false; // 잘못된 시험 전투 차단
            } // 입력 거절 끝
            var baseExperience = 40 + floor * 20; // 기획된 층별 일반 적 경험치
            experience = normalEnemies * baseExperience + eliteEnemies * baseExperience * 3; // 일반과 상위 개체의 개별 경험치 합산
            return true; // 유효한 승리 경험치 계산 성공
        } // 계산 끝
        public static bool TryAdvance(int level, int currentExperience, int gainedExperience, out ExperienceProgress result) // 기획 요구 경험치에 따른 성장 계산
        { // 계산 시작
            result = null; // 기본 성장 결과 없음
            if (level < 1 || level > MaxLevel || currentExperience < 0 || gainedExperience < 0 || (level == MaxLevel && currentExperience != 0)) // 레벨과 현재 및 획득 경험치 검사
            { // 입력 오류 시작
                return false; // 잘못된 성장 상태 차단
            } // 입력 오류 끝
            if (level < MaxLevel && currentExperience >= RequiredExperience(level)) // 이미 레벨업해야 하는 진행값 검사
            { // 진행 오류 시작
                return false; // 정규화되지 않은 저장 상태 차단
            } // 진행 오류 끝
            var points = 0; // 이번에 획득한 스탯 포인트
            var levels = 0; // 이번에 상승한 레벨 수
            var remaining = (long)currentExperience + gainedExperience; // 정수 범위 보호의 누적 경험치
            while (level < MaxLevel && remaining >= RequiredExperience(level)) // 현재 레벨의 요구 경험치 충족 검사
            { // 연속 레벨 상승 시작
                remaining -= RequiredExperience(level); // 현재 레벨의 요구 경험치 차감
                level++; // 다음 레벨 도달
                levels++; // 이번 레벨 상승 수 증가
                points += StatPointsByLevel[level]; // 도달 레벨의 추가 포인트 합산
            } // 연속 레벨 상승 끝
            var discarded = level == MaxLevel ? remaining : 0L; // 최대 레벨 이후 미적용 경험치
            var applied = gainedExperience - discarded; // 실제 성장에 반영한 획득량
            result = new ExperienceProgress(level, level == MaxLevel ? 0 : (int)remaining, (int)applied, levels, points); // 검증된 레벨과 이월 경험치 전달
            return true; // 유효한 성장 계산 성공
        } // 계산 끝
        private static int RequiredExperience(int level) // 현재 레벨의 다음 레벨 요구량
        { // 요구량 계산 시작
            return 100 + (level - 1) * 50; // 기획된 단계별 요구 경험치
        } // 계산 끝
    } // 클래스 끝
} // 이름 공간 끝
