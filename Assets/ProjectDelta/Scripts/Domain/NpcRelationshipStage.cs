namespace ProjectDelta.Domain
{
    // 113일차: NPC 호감도 0~100을 관계 화면에서 사용할 단계로 변환한다.
    public enum NpcRelationshipStage
    {
        Neutral = 0,
        Interest = 1,
        Trust = 2,
        Special = 3,
        EndingAvailable = 4
    }
}
