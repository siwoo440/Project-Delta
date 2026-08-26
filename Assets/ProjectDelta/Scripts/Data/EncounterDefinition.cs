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

        // 76일차: 그룹 2번째 자리부터 뽑는 추가 몬스터 후보 (비어 있으면 monster만 반복해서
        // 채운다). 그룹 마리 수는 [minGroupSize, maxGroupSize] 범위에서 던전 생성 시 결정론적으로
        // 뽑힌다. 기본값(1~1)은 기존 "몬스터 1마리" 동작과 완전히 동일하다.
        [Header("76일차 몬스터 그룹")]
        [SerializeField] private EncounterMonsterEntry[] additionalMonsterPool =
            new EncounterMonsterEntry[0];
        [Min(1)]
        [SerializeField] private int minGroupSize = 1;
        [Min(1)]
        [SerializeField] private int maxGroupSize = 1;

        public MonsterDefinition Monster => monster;
        public float RoomSpawnChance => roomSpawnChance;
        public bool Enabled => enabled;

        public EncounterMonsterEntry[] AdditionalMonsterPool => additionalMonsterPool;
        public int MinGroupSize => Mathf.Max(1, minGroupSize);
        public int MaxGroupSize => Mathf.Max(MinGroupSize, maxGroupSize);

        public bool IsValidForPlacement =>
            enabled
            && monster != null
            && !string.IsNullOrEmpty(Id)
            && !string.IsNullOrEmpty(monster.Id);
    }
}
