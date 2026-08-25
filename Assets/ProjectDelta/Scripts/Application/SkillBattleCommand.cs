using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // 67일차: SkillDefinition 하나를 실행하는 범용 Command. AttackBattleCommand(49일차)·
    // DefendBattleCommand(52일차)와 같은 원칙으로, Execute()는 아직 "선언이 유효한가"만
    // 판정한다 - 대상 유효성과 자원(마나·정력) 충분 여부만 확인하고 실제로 소모하지는 않는다.
    // 실제 명중·피해 계산, 상태 부여, 추가 행동, 자원 차감은 이후 일차에서 연결한다.
    //
    // 특정 스킬 하나가 아니라 어떤 SkillDefinition이든 받아 판정하는 범용 Command라서,
    // 스킬이 늘어나도 새 Command 클래스를 추가할 필요가 없다.
    public sealed class SkillBattleCommand : IBattleCommand
    {
        private readonly SkillDefinition skill;

        public SkillBattleCommand(
            SkillDefinition skill)
        {
            this.skill =
                skill;
        }

        public string Id =>
            skill != null
                ? skill.Id
                : "Skill";

        public string DisplayName =>
            skill != null
                ? skill.DisplayName
                : "스킬";

        public BattleCommandResult Execute(
            BattleContext context,
            BattleParticipant actor,
            BattleParticipant target)
        {
            if (skill == null)
            {
                return BattleCommandResult.Reject(
                    Id,
                    "스킬 데이터가 없습니다.");
            }

            if (context == null
                || actor == null)
            {
                return BattleCommandResult.Reject(
                    Id,
                    "현재 Battle 정보가 없습니다.");
            }

            if (skill.TargetType == SkillTargetType.Enemy
                && !BattleTargeting.IsValidTarget(
                    context,
                    actor,
                    target))
            {
                return BattleCommandResult.Reject(
                    Id,
                    "대상을 선택할 수 없습니다.");
            }

            if (actor.CurrentMana < skill.ManaCost)
            {
                return BattleCommandResult.Reject(
                    Id,
                    "마나가 부족합니다.");
            }

            if (actor.CurrentStamina < skill.StaminaCost)
            {
                return BattleCommandResult.Reject(
                    Id,
                    "정력이 부족합니다.");
            }

            string targetSuffix =
                skill.TargetType == SkillTargetType.Enemy
                    ? $" → {target.InstanceId}"
                    : string.Empty; // Self 대상은 표시할 대상이 따로 없음

            return BattleCommandResult.Accept(
                Id,
                $"스킬 선언 / {actor.InstanceId} → {skill.DisplayName}{targetSuffix}");
        }
    }
}
