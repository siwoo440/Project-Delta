namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 113일차: NPC 호감도 0~100을 관계 화면에서 사용할 단계로 변환한다.
    public enum NpcRelationshipStage // 호감도 수치를 변환한 관계 단계
    {
        Neutral = 0, // 무관심 단계
        Interest = 1, // 관심 단계
        Trust = 2, // 신뢰 단계
        Special = 3, // 특별한 관계 단계
        EndingAvailable = 4 // 엔딩 조건을 만족한 단계
    }
}
