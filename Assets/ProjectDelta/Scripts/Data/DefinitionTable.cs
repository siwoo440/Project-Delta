using System.Collections.Generic;

namespace ProjectDelta.Data
{
    public sealed class DefinitionTable<TDefinition> where TDefinition : DefinitionBase
    {
        private readonly Dictionary<string, TDefinition> _definitions = new Dictionary<string, TDefinition>();

        public IReadOnlyCollection<TDefinition> All => _definitions.Values;

        public void Load(IEnumerable<TDefinition> definitions)
        {
            _definitions.Clear();
            foreach (var definition in definitions)
            {
                _definitions[definition.Id] = definition;
            }
        }

        public TDefinition Get(string id)
        {
            if (_definitions.TryGetValue(id, out var definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"Definition not found for id '{id}'.");
        }

        public bool TryGet(string id, out TDefinition definition)
        {
            return _definitions.TryGetValue(id, out definition);
        }
    }
}
