namespace ProjectDelta.Domain
{
    // 132일차: 기획서 7.3절 "몬스터 개별 엔딩(20)"/"NPC 개별 엔딩(10)" 발동 조건 판정.
    // 실제 "재조우 후 함께 남기 선택" 화면은 이후 일차 몫이라, 여기서는 "지금 이 대상으로
    // 개별 엔딩을 볼 수 있는 상태인가"만 순수하게 판정한다.
    public static class CharacterEndingRule
    {
        // 몬스터 개별 엔딩 - 이번 회차 호감도 100(성인 이벤트 전투 승리) 또는 해당 종족
        // 도감 100% 중 하나만 만족하면 된다. 단, 몬스터 하렘(MainEndingId.MonsterHarem)은
        // 기획서상 도감으로 대체할 수 없어 이 판정과 별개다 - 호출자가 하렘용으로 이
        // 메서드를 재사용하면 안 된다.
        public static bool IsMonsterEndingEligible(
            bool affinityMaxedThisRun,
            bool speciesDexComplete)
        {
            return affinityMaxedThisRun
                || speciesDexComplete;
        }

        // NPC 개별 엔딩 - 호감도 100(회차를 넘어 유지)과 핵심 선택 이벤트 완료를 모두 만족해야 한다.
        public static bool IsNpcEndingEligible(
            int affinity,
            bool hasCompletedKeyEvent)
        {
            return affinity >= 100
                && hasCompletedKeyEvent;
        }
    }
}
