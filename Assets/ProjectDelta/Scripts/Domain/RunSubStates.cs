namespace ProjectDelta.Domain
{
    // Placeholders for the remaining RunContext sub-states (기획서 10.2).
    // Each is filled in once its owning system exists:
    //   DungeonRunState   - 3.1~3.2절 던전 생성
    //   InventoryRunState - 6.4절 인벤토리·장비·유물
    //   SkillRunState     - 6.3절 스킬과 행동 숙련도
    //   CharacterRunState - 5장 몬스터·NPC (CharacterInstanceState)
    //   EventRunState     - 3.4~3.5절 이벤트
    //   BattleRunState    - 4장 전투
    //   RewardRunState    - 6.5절 아이템·보상
    //   RunStatistics     - 회차 단위 진행 통계
    public sealed class DungeonRunState { }
    public sealed class InventoryRunState { }
    public sealed class SkillRunState { }
    public sealed class CharacterRunState { }
    public sealed class EventRunState { }
    public sealed class BattleRunState { }
    public sealed class RewardRunState { }
    public sealed class RunStatistics { }
}
