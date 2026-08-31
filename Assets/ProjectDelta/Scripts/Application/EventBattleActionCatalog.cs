using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 118일차: 기획서가 요구한 "공통 행동 12종"을 한 곳에 모은다. 플레이어의 행동 목록
    // (EventBattleController 버튼)과 몬스터의 저항 행동(같은 12종을 공유 - "공통"의 의미)
    // 모두 이 카탈로그 하나를 재사용한다.
    public static class EventBattleActionCatalog
    {
        public static readonly IReadOnlyList<IEventBattleCommand> All =
            new IEventBattleCommand[]
            {
                new CourtEventBattleCommand(),
                new SootheEventBattleCommand(),
                new FlatterEventBattleCommand(),
                new GiftEventBattleCommand(),
                new TeaseEventBattleCommand(),
                new ListenEventBattleCommand(),
                new ConfessEventBattleCommand(),
                new DanceEventBattleCommand(),
                new SingEventBattleCommand(),
                new WinkEventBattleCommand(),
                new EmbraceEventBattleCommand(),
                new WhisperEventBattleCommand()
            };
    }
}
