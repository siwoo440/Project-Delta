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

        // 78일차: 이 인카운터가 등장할 수 있는 층 범위. 기본값(1층 이상, 상한 없음)은
        // "아직 배치가 정해지지 않은 몬스터는 일단 모든 층에 나온다"는 정책과 같다.
        // 이후 밸런싱 때 minFloor·maxFloor만 좁히면 특정 층 전용으로 제한할 수 있다.
        [Header("78일차 층 제한")]
        [Min(1)]
        [SerializeField] private int minFloor = 1;
        [Tooltip("-1이면 상한 없음(minFloor 이상 모든 층에서 등장). 특정 층까지만 나오게 하려면 그 층 번호를 넣는다.")]
        [SerializeField] private int maxFloor = -1;

        public MonsterDefinition Monster => monster;
        public float RoomSpawnChance => roomSpawnChance;
        public bool Enabled => enabled;

        public EncounterMonsterEntry[] AdditionalMonsterPool => additionalMonsterPool;
        public int MinGroupSize => Mathf.Max(1, minGroupSize);
        public int MaxGroupSize => Mathf.Max(MinGroupSize, maxGroupSize);

        public int MinFloor => Mathf.Max(1, minFloor);
        public int MaxFloor => maxFloor;

        public bool IsValidForPlacement =>
            enabled
            && monster != null
            && !string.IsNullOrEmpty(Id)
            && !string.IsNullOrEmpty(monster.Id);

        // 78일차: 이 인카운터를 지정한 층에서 써도 되는지 확인한다.
        public bool IsAllowedOnFloor(
            int floor)
        {
            if (floor < MinFloor)
            {
                return false;
            }

            if (maxFloor >= 1
                && floor > maxFloor)
            {
                return false;
            }

            return true;
        }
    }
}
