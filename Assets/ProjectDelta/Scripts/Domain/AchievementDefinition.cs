namespace ProjectDelta.Domain
{
    // 134일차: Steam 도전과제 100개를 네 카테고리로 분류한다.
    public enum AchievementCategory
    {
        Ending = 0, // 엔딩 도전과제 분류
        Defeat = 1, // 패배 기록 도전과제 분류
        Lifetime = 2, // 탐험·전투·성장 누적 도전과제 분류
        ActionProficiency = 3 // 이벤트 전투 행동 숙련도 도전과제 분류
    }

    // 134일차: 프로필의 어떤 영구 기록을 읽어 달성 여부를 판정할지 구분한다.
    public enum AchievementConditionType
    {
        MainEnding = 0, // 주요 엔딩 ID 직접 확인
        MonsterEndingCount = 1, // 몬스터 개별 엔딩 고유 개수 확인
        NpcEndingCount = 2, // NPC 개별 엔딩 고유 개수 확인
        DefeatRecordCount = 3, // 패배 기록 고유 개수 확인
        LifetimeStat = 4, // LifetimeStats 누적 수치 확인
        ActionProficiency = 5 // 공통 행동 숙련도 레벨 확인
    }

    // 134일차: 도전과제 하나의 고정 정의 데이터다.
    public sealed class AchievementDefinition
    {
        public string Id { get; } // 내부·Steam 연동용 고정 ID
        public string DisplayName { get; } // 화면 표시용 이름
        public AchievementCategory Category { get; } // 네 카테고리 중 하나
        public AchievementConditionType ConditionType { get; } // 달성 판정 방식
        public string TargetId { get; } // 엔딩·통계·행동 대상 식별자
        public int TargetValue { get; } // 필요 개수·누적값·숙련도 레벨
        public bool IsHidden { get; } // Steam 숨김 도전과제 여부

        public AchievementDefinition(
            string id,
            string displayName,
            AchievementCategory category,
            AchievementConditionType conditionType,
            string targetId,
            int targetValue,
            bool isHidden)
        {
            Id = id; // 도전과제 ID 저장
            DisplayName = displayName; // 표시 이름 저장
            Category = category; // 카테고리 저장
            ConditionType = conditionType; // 판정 방식 저장
            TargetId = targetId; // 대상 ID 저장
            TargetValue = targetValue; // 목표값 저장
            IsHidden = isHidden; // 숨김 여부 저장
        }
    }
}
