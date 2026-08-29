namespace ProjectDelta.Data
{
    public sealed class DataRepository
    {
        public DefinitionTable<MonsterDefinition> Monsters { get; } = new DefinitionTable<MonsterDefinition>();
        public DefinitionTable<ItemDefinition> Items { get; } = new DefinitionTable<ItemDefinition>();

        // 113일차: NPC도 몬스터·아이템과 같은 영구 ID 기반 정의 테이블로 조회한다.
        public DefinitionTable<NpcDefinition> Npcs { get; } = new DefinitionTable<NpcDefinition>();

        public MonsterDefinition GetMonster(string id) => Monsters.Get(id);
        public ItemDefinition GetItem(string id) => Items.Get(id);
        public NpcDefinition GetNpc(string id) => Npcs.Get(id);
    }
}
