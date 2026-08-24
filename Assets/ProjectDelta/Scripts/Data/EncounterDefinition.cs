using UnityEngine;

namespace ProjectDelta.Data
{
    [CreateAssetMenu(
        fileName = "EncounterDefinition",
        menuName = "ProjectDelta/Data/Encounter Definition")]
    public sealed class EncounterDefinition : DefinitionBase
    {
        [SerializeField] private MonsterDefinition monster;
        [SerializeField, Range(0f, 1f)] private float roomSpawnChance = 0.35f;
        [SerializeField] private bool enabled = true;

        public MonsterDefinition Monster => monster;
        public float RoomSpawnChance => roomSpawnChance;
        public bool Enabled => enabled;

        public bool IsValidForPlacement =>
            enabled
            && monster != null
            && !string.IsNullOrEmpty(Id)
            && !string.IsNullOrEmpty(monster.Id);
    }
}
