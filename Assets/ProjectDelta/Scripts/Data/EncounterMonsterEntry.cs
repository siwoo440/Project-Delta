using UnityEngine;

namespace ProjectDelta.Data
{
    // 76일차: EncounterDefinition이 그룹의 2번째 자리 이후를 채울 때 뽑을 수 있는 몬스터 후보
    // 하나. weight가 클수록 이 몬스터가 뽑힐 상대적 확률이 높다.
    [System.Serializable]
    public sealed class EncounterMonsterEntry
    {
        [SerializeField] private MonsterDefinition monster;
        [Min(1)]
        [SerializeField] private int weight = 1;

        public MonsterDefinition Monster => monster;
        public int Weight => Mathf.Max(1, weight);
    }
}
