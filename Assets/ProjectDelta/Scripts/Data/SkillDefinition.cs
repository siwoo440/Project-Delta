using UnityEngine;

namespace ProjectDelta.Data
{
    /// <summary>
    /// 스킬 데이터 원본 (기획서 4.2 "전투 명령" - 공격·방어에 이은 세 번째 전투 행동).
    /// 66일차에는 기존 계산 엔진(BattleDamageCalculator, StatusEffectApplicationService,
    /// BattleSession.TryGrantExtraAction)이 이미 받을 수 있는 값을 데이터로 옮기는 것까지만
    /// 담당한다. 실제로 이 데이터를 읽어 실행하는 SkillBattleCommand는 이후 일차에서 만든다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkillDefinition",
        menuName = "Project Delta/Data/Skill Definition")]
    public class SkillDefinition : DefinitionBase
    {
        [Header("Display")]
        [SerializeField] private string displayName;

        [Header("Cost")]
        [Tooltip("스킬 사용에 필요한 마나. 부족하면 사용할 수 없다.")]
        [Min(0)]
        [SerializeField] private int manaCost;
        [Tooltip("스킬 사용에 필요한 정력. 부족하면 사용할 수 없다.")]
        [Min(0)]
        [SerializeField] private int staminaCost;

        [Header("Damage")]
        [Tooltip("기본 공격 대비 공격 배율(%). 기본 공격은 100.")]
        [SerializeField] private int damageMultiplierPercent = 100;
        [SerializeField] private SkillDamageType damageType;
        [SerializeField] private SkillDefenseInteraction defenseInteraction;
        [Tooltip("명중률 공식에 더해지는 스킬 자체 보정치. 양수면 더 잘 맞고 음수면 더 잘 빗나간다.")]
        [SerializeField] private int accuracyModifierPercent;
        [Tooltip("치명타 확률(%). 0이면 이 스킬로는 치명타가 발생하지 않는다.")]
        [Range(0, 100)]
        [SerializeField] private int criticalChancePercent;
        [Tooltip("치명타 배율(%). 0이면 치명타 확률과 무관하게 발생하지 않는다.")]
        [SerializeField] private int criticalMultiplierPercent;

        [Header("Status Effect")]
        [Tooltip("이 스킬이 명중 시 부여를 시도하는 상태. 없으면 상태를 부여하지 않는다.")]
        [SerializeField] private StatusEffectDefinition grantedStatusEffect;
        [Tooltip("상태 성공률 공식의 효과 기본 확률(%). grantedStatusEffect가 있을 때만 쓰인다.")]
        [Range(0, 100)]
        [SerializeField] private int statusEffectBaseChancePercent;
        [Min(1)]
        [SerializeField] private int statusEffectDurationRounds = 1;
        [Tooltip("상태 인스턴스에 넘길 적용 수치. DamageOverTime·HealOverTime은 절대값, StatModifier는 부호 있는 보정치로 쓴다.")]
        [SerializeField] private int statusEffectAppliedValue;

        [Header("Extra Action")]
        [Tooltip("성공적으로 사용하면 시전자에게 추가 행동을 부여하는지 (BattleSession.TryGrantExtraAction 연결 대상).")]
        [SerializeField] private bool grantsExtraAction;

        public string DisplayName
        {
            get
            {
                return string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
            }
        }

        public int ManaCost => manaCost;
        public int StaminaCost => staminaCost;

        public int DamageMultiplierPercent => damageMultiplierPercent;
        public SkillDamageType DamageType => damageType;
        public SkillDefenseInteraction DefenseInteraction => defenseInteraction;
        public int AccuracyModifierPercent => accuracyModifierPercent;
        public int CriticalChancePercent => criticalChancePercent;
        public int CriticalMultiplierPercent => criticalMultiplierPercent;

        public StatusEffectDefinition GrantedStatusEffect => grantedStatusEffect;
        public int StatusEffectBaseChancePercent => statusEffectBaseChancePercent;
        public int StatusEffectDurationRounds => statusEffectDurationRounds;
        public int StatusEffectAppliedValue => statusEffectAppliedValue;

        public bool GrantsExtraAction => grantsExtraAction;
    }
}
