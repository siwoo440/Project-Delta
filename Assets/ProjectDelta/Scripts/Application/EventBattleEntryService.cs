namespace ProjectDelta.Application
{
    // 117일차: 기획서가 요구하는 "하나의 Entry API" - 유혹 성공·스킬/몬스터 행동·일반 이벤트·
    // 적대 NPC·상위 개체(보스) 전용 결과, 이 4갈래가 전부 이 메서드 하나로 들어온다.
    // 지금은 유혇(Seduction) 경로만 실제로 쓰이지만, 나머지 3개도 같은 방식으로 EventBattleContext를
    // 만들면 되므로 새 콘텐츠가 생겼을 때 이 서비스만 재사용하면 된다.
    public static class EventBattleEntryService
    {
        public static bool TryEnter(
            EventBattleEntrySource source,
            BattleParticipant player,
            BattleParticipant target,
            out EventBattleContext context)
        {
            context =
                null;

            if (player == null
                || target == null
                || !player.IsAlive
                || !target.IsAlive)
            {
                return false;
            }

            context =
                new EventBattleContext(
                    source,
                    player,
                    target);

            return true;
        }
    }
}
