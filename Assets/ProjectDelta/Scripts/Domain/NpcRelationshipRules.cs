namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public static class NpcRelationshipRules // 호감도 → 관계 단계 변환 규칙
    {
        public static NpcRelationshipStage GetStage( // 호감도 수치로 관계 단계 조회
            int affinity) // 원본 호감도 값(범위를 벗어날 수 있음)
        {
            int safeAffinity = // 0~100으로 잘라낸 안전한 호감도
                affinity < 0 // 0 미만이면
                    ? 0 // 0으로 고정
                    : affinity > 100 // 100 초과면
                        ? 100 // 100으로 고정
                        : affinity; // 범위 안이면 그대로 사용

            if (safeAffinity >= 100) // 최대치에 도달했으면
            {
                return NpcRelationshipStage.EndingAvailable; // 엔딩 조건 단계 반환
            }

            if (safeAffinity >= 85) // 85 이상이면
            {
                return NpcRelationshipStage.Special; // 특별한 관계 단계 반환
            }

            if (safeAffinity >= 67) // 67 이상이면
            {
                return NpcRelationshipStage.Trust; // 신뢰 단계 반환
            }

            if (safeAffinity >= 34) // 34 이상이면
            {
                return NpcRelationshipStage.Interest; // 관심 단계 반환
            }

            return NpcRelationshipStage.Neutral; // 그 외는 무관심 단계
        }
    }
}
