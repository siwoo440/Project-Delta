namespace ProjectDelta.Data
{
    public sealed class DataRepository
    {
        public DefinitionTable<MonsterDefinition> Monsters { get; } = new DefinitionTable<MonsterDefinition>();
        public DefinitionTable<ItemDefinition> Items { get; } = new DefinitionTable<ItemDefinition>();

        public MonsterDefinition GetMonster(string id) => Monsters.Get(id);
        public ItemDefinition GetItem(string id) => Items.Get(id);
    }
}
