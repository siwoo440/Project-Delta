namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public enum NpcInteractionCommand // 플레이어가 NPC에게 내릴 수 있는 행동
    {
        Talk = 0, // 대화하기
        Service = 1, // 서비스(상점 등) 이용
        Leave = 2, // 상호작용 종료

        // 115일차: 우호 상호작용(선물·구조)과 적대 전환(공격) 추가.
        Gift = 3, // 선물 주기
        Rescue = 4, // 구조하기
        Attack = 5 // 공격해 적대로 전환
    }
}
