using UnityEngine;

namespace ProjectDelta.Data
{
    [CreateAssetMenu(fileName = "MonsterDefinition", menuName = "ProjectDelta/Data/Monster Definition")]
    public sealed class MonsterDefinition : DefinitionBase
    {
        [SerializeField] private string displayName;

        // 76일차: 한 방에 여러 몬스터가 섞여 나올 때 탐험 화면 대표 외형을 정하는 기준
        // (일반 < 희귀 < 보스 순).
        [SerializeField] private MonsterRarity rarity = MonsterRarity.Normal;

        // 54일차: BattleParticipant를 만드는 데 필요한 전투 능력치 7종 + 자원.
        // 층 보정·개체 등급·난이도 보정·개체 편차(기획서 3.5)는 아직 적용하지 않는다.
        [Header("전투 능력치")]
        [SerializeField] private int maxHp = 1;
        [SerializeField] private int maxMana;
        [SerializeField] private int speed;
        [SerializeField] private int attack;
        [SerializeField] private int defense;
        [SerializeField] private int accuracy;
        [SerializeField] private int evasion;
        [SerializeField] private int charm;
        [SerializeField] private int resistance;

        [Header("74일차 몬스터 AI")]
        [SerializeField] private MonsterAiProfile aiProfile;

        public string DisplayName => displayName;
        public MonsterRarity Rarity => rarity;

        public int MaxHp => maxHp;
        public int MaxMana => maxMana;
        public int Speed => speed;
        public int Attack => attack;
        public int Defense => defense;
        public int Accuracy => accuracy;
        public int Evasion => evasion;
        public int Charm => charm;
        public int Resistance => resistance;

        public MonsterAiProfile AiProfile => aiProfile;
    }
}
