using System;
using System.Collections.Generic;
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

        [Header("79일차 성장 보상")]
        [Min(0)]
        [SerializeField] private int experienceReward = 20;

        [Header("80일차 전투 드롭")]
        [SerializeField] private MonsterDropTable dropTable;

        // 118일차: 별도 이벤트 전투 공통 행동 12종 중 이 종족이 강점(적게 반응, 50%)·약점
        // (크게 반응, 150%)을 보이는 행동 ID들. 아직 몬스터별 실제 상성 데이터를 입력하는
        // 133~135일차(몬스터 콘텐츠 완성) 전이라 기본값은 빈 배열 - 지정되지 않은 행동은
        // 전부 보통(100%)으로 취급된다(EventBattleAffinityRule).
        [Header("118일차 이벤트 전투 상성 (강점4·약점4, 나머지는 보통)")]
        [SerializeField] private string[] eventBattleStrongActionIds =
            Array.Empty<string>();

        [SerializeField] private string[] eventBattleWeakActionIds =
            Array.Empty<string>();

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

        public int ExperienceReward =>
            Mathf.Max(
                0,
                experienceReward);

        public MonsterDropTable DropTable =>
            dropTable;

        public IReadOnlyList<string> EventBattleStrongActionIds =>
            eventBattleStrongActionIds
            ?? Array.Empty<string>();

        public IReadOnlyList<string> EventBattleWeakActionIds =>
            eventBattleWeakActionIds
            ?? Array.Empty<string>();
    }
}
