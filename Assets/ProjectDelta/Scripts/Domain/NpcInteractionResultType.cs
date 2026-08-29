namespace ProjectDelta.Domain
{
    // 113일차: NPC 기능이 끝난 뒤 탐험·서비스·전투 중 어디로 이어질지 공통 결과로 표현한다.
    public enum NpcInteractionResultType
    {
        ContinueInteraction = 0,
        OpenService = 1,
        ReturnToExploration = 2,
        StartBattle = 3
    }
}
