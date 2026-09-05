namespace ProjectDelta.Domain
{
    // 131일차: 기획서 7.1~7.2절 판정 로직. 여러 조건이 동시에 맞을 수 있어서
    // "더 구체적인 조건을 먼저 검사하고, 아무 것도 안 맞으면 기본형(현실로의 귀환/
    // 던전의 왕)으로 떨어진다"는 우선순위를 명시적으로 둔다 - 기획서에 우선순위
    // 표는 없어서 이 순서는 구현 판단이다(도감·개별 엔딩처럼 아직 값이 항상
    // 기본값인 조건도 있어 실제로 부딪힐 일은 드물다).
    public static class MainEndingRule
    {
        private const float LowResourceThreshold = 0.2f;

        public static MainEndingId Evaluate(
            MainEndingConditions conditions)
        {
            if (conditions == null)
            {
                return MainEndingId.None;
            }

            // 패배·항복은 최종 선택 화면 자체가 뜨지 않고 곧바로 확정된다.
            if (conditions.BossOutcome == MainBossOutcome.Defeat
                || conditions.BossOutcome == MainBossOutcome.Surrender)
            {
                return MainEndingId.ServantOfTheDemonLord;
            }

            // 선택지와 무관한 특수 조건 - 승리 여부와 상관없이 먼저 검사한다.
            if (conditions.AllRelationshipsMaxed)
            {
                return MainEndingId.MonsterHarem;
            }

            if (conditions.Choice == MainEndingChoice.ReturnToReality)
            {
                return EvaluateReturnBranch(
                    conditions);
            }

            if (conditions.Choice == MainEndingChoice.StayInDungeon)
            {
                return EvaluateStayBranch(
                    conditions);
            }

            return MainEndingId.None;
        }

        private static MainEndingId EvaluateReturnBranch(
            MainEndingConditions conditions)
        {
            if (conditions.CursedItemCount >= 3)
            {
                return MainEndingId.CursedReturn;
            }

            if (conditions.EquippedAndRelicCount <= 0)
            {
                return MainEndingId.EmptyHandedReturn;
            }

            if (conditions.HpRatio <= LowResourceThreshold
                || conditions.StaminaRatio <= LowResourceThreshold)
            {
                return MainEndingId.WoundedReturn;
            }

            if (conditions.FloorExplorationComplete)
            {
                return MainEndingId.CompleteExplorerReturn;
            }

            if (conditions.IndividualEndingConditionsMetCount >= 5)
            {
                return MainEndingId.ReturnLeavingEverythingBehind;
            }

            if (conditions.BossOutcome == MainBossOutcome.NormalVictory)
            {
                return MainEndingId.HeroicReturn;
            }

            if (conditions.BossOutcome == MainBossOutcome.EventVictory)
            {
                return MainEndingId.ReturnOfWill;
            }

            return MainEndingId.ReturnToReality;
        }

        private static MainEndingId EvaluateStayBranch(
            MainEndingConditions conditions)
        {
            if (conditions.CursedItemCount >= 3)
            {
                return MainEndingId.CursedKing;
            }

            if (conditions.MonsterDexComplete)
            {
                return MainEndingId.KingOfRecords;
            }

            if (conditions.BossOutcome == MainBossOutcome.NormalVictory)
            {
                return MainEndingId.KingOfConquest;
            }

            if (conditions.BossOutcome == MainBossOutcome.EventVictory)
            {
                return MainEndingId.KingOfCharm;
            }

            return MainEndingId.KingOfTheDungeon;
        }
    }
}
