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

        // 121일차: 일반 몬스터와 구분되는 상위 개체 등급 - 능력치·보상 배율에 쓰인다
        // (MonsterTierRules). Rarity(76일차, 탐험 화면 대표 외형 우선순위)와는 별개다.
        [SerializeField] private MonsterTier tier = MonsterTier.Normal;

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

        // 122일차: 상위 개체(보스) 전용 - Tier가 Boss가 아닌 몬스터는 사용하지 않는다.
        // 페이즈 수(체력 구간별 패턴 전환)와 후퇴·항복 가능 여부를 BossPhaseRule/전투 흐름이 읽는다.
        [Header("122일차 보스 전용 (Tier == Boss일 때만 의미 있음)")]
        [Min(1)]
        [SerializeField] private int phaseCount = 1;

        [SerializeField] private bool canRetreat = true;

        [SerializeField] private bool canSurrenderOnly;

        public string DisplayName => displayName;
        public MonsterRarity Rarity => rarity;
        public MonsterTier Tier => tier;

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

        // 121일차: 등급 배율(MonsterTierRules)을 곱한 값을 돌려준다 - 79일차 PlayerGrowthService가
        // 이 값을 그대로 합산하므로, 여기서 한 번만 곱하면 보스/정예가 자동으로 경험치를 더 준다.
        public int ExperienceReward =>
            Mathf.Max(
                0,
                Mathf.RoundToInt(
                    experienceReward
                    * MonsterTierRules.GetRewardMultiplier(
                        tier)));

        public MonsterDropTable DropTable =>
            dropTable;

        public IReadOnlyList<string> EventBattleStrongActionIds =>
            eventBattleStrongActionIds
            ?? Array.Empty<string>();

        public IReadOnlyList<string> EventBattleWeakActionIds =>
            eventBattleWeakActionIds
            ?? Array.Empty<string>();

        public int PhaseCount =>
            Mathf.Max(
                1,
                phaseCount);

        public bool CanRetreat =>
            canRetreat;

        public bool CanSurrenderOnly =>
            canSurrenderOnly;
    }
}
