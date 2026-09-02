namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 113일차: NPC 기능이 끝난 뒤 탐험·서비스·전투 중 어디로 이어질지 공통 결과로 표현한다.
    public enum NpcInteractionResultType // NPC 상호작용 종료 후 다음 화면 종류
    {
        ContinueInteraction = 0, // 같은 NPC 대화를 계속 이어간다
        OpenService = 1, // 상점 등 서비스 화면을 연다
        ReturnToExploration = 2, // 탐험 화면으로 돌아간다
        StartBattle = 3 // 전투로 전환한다
    }
}
