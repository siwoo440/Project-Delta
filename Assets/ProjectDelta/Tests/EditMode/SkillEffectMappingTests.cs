using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // SkillEffectMapping·DamageType·DefenseInteraction 사용
using ProjectDelta.Data; // SkillDamageType·SkillDefenseInteraction 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class SkillEffectMappingTests
    {
        [TestCase(SkillDamageType.Normal, DamageType.Normal)]
        [TestCase(SkillDamageType.StatusEffect, DamageType.StatusEffect)]
        [TestCase(SkillDamageType.DamageOverTime, DamageType.DamageOverTime)]
        [TestCase(SkillDamageType.Fixed, DamageType.Fixed)]
        public void ToDamageType_MapsEveryValueOneToOne(
            SkillDamageType skillDamageType,
            DamageType expected)
        {
            Assert.AreEqual(
                expected,
                SkillEffectMapping.ToDamageType(
                    skillDamageType));
        }

        [TestCase(SkillDefenseInteraction.Defendable, DefenseInteraction.Defendable)]
        [TestCase(SkillDefenseInteraction.PenetratesDefense, DefenseInteraction.PenetratesDefense)]
        [TestCase(SkillDefenseInteraction.IgnoresDefense, DefenseInteraction.IgnoresDefense)]
        public void ToDefenseInteraction_MapsEveryValueOneToOne(
            SkillDefenseInteraction skillDefenseInteraction,
            DefenseInteraction expected)
        {
            Assert.AreEqual(
                expected,
                SkillEffectMapping.ToDefenseInteraction(
                    skillDefenseInteraction));
        }
    }
}
