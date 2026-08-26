using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDelta.Data
{
    [Serializable]
    public sealed class MonsterAiSkillEntry
    {
        [SerializeField] private SkillDefinition skill;
        [Min(0)]
        [SerializeField] private int weight = 20;

        public SkillDefinition Skill => skill;
        public int Weight => Math.Max(0, weight);

        public MonsterAiSkillEntry(
            SkillDefinition skill,
            int weight)
        {
            this.skill =
                skill;

            this.weight =
                Math.Max(
                    0,
                    weight);
        }
    }

    [CreateAssetMenu(
        fileName = "MonsterAiProfile",
        menuName = "Project Delta/Data/Monster AI Profile")]
    public sealed class MonsterAiProfile : ScriptableObject
    {
        [Header("기본 행동 가중치")]
        [Min(0)]
        [SerializeField] private int attackWeight = 60;
        [Min(0)]
        [SerializeField] private int defendWeight = 20;

        [Header("저체력 방어 보정")]
        [Range(0, 100)]
        [SerializeField] private int lowHpThresholdPercent = 40;
        [Min(0)]
        [SerializeField] private int lowHpDefendBonusWeight = 30;

        [Header("사용 가능 스킬")]
        [SerializeField] private MonsterAiSkillEntry[] skillEntries =
            Array.Empty<MonsterAiSkillEntry>();

        public int AttackWeight =>
            Math.Max(
                0,
                attackWeight);

        public int DefendWeight =>
            Math.Max(
                0,
                defendWeight);

        public int LowHpThresholdPercent =>
            Mathf.Clamp(
                lowHpThresholdPercent,
                0,
                100);

        public int LowHpDefendBonusWeight =>
            Math.Max(
                0,
                lowHpDefendBonusWeight);

        public IReadOnlyList<MonsterAiSkillEntry> SkillEntries =>
            skillEntries
            ?? Array.Empty<MonsterAiSkillEntry>();
    }
}
