namespace ProjectDelta.Application
{
    // 117일차: 별도 이벤트 전투로 들어오는 경로. 기획서상 4가지 진입 경로를 전부
    // EventBattleEntryService.TryEnter() 하나로 받는다 - 이 값은 그중 어디서 왔는지 기록만 한다.
    public enum EventBattleEntrySource
    {
        // 116일차 유혹(SeduceBattleCommand) 성공 시 전환.
        Seduction,

        // 스킬·몬스터 행동에서 트리거되는 전환 (아직 그런 스킬/행동이 없어 미사용).
        SkillOrMonsterAction,

        // 일반 이벤트·적대 NPC 전용 전투 (아직 그런 이벤트가 없어 미사용).
        HostileEvent,

        // 상위 개체(보스) 전용 전투 (아직 보스 콘텐츠가 없어 미사용).
        BossEncounter
    }
}
