using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 119일차: "전용 행동 AI·저항/관찰 대응" - 몬스터가 완전히 무작위로 저항하던 118일차와
    // 달리, 직전에 자기가 썼던 행동은 피하고(같은 저항을 반복하지 않음) 플레이어가 방금 쓴
    // 행동에 약한(효과가 큰) 것과 짝이 되는 강한 대응을 살짝 더 고르기 쉽게 한다 - 완전한
    // 예측 AI는 아니지만, 매번 순수 무작위였던 118일차보다는 "지켜보고 있다"는 인상을 준다.
    public static class EventBattleMonsterAiRule
    {
        public static IEventBattleCommand ChooseAction(
            IReadOnlyList<IEventBattleCommand> catalog,
            string lastMonsterActionId,
            IRandomSource rng)
        {
            if (catalog == null
                || catalog.Count == 0
                || rng == null)
            {
                return null;
            }

            List<IEventBattleCommand> candidates =
                new List<IEventBattleCommand>();

            for (int index = 0; index < catalog.Count; index++)
            {
                IEventBattleCommand action =
                    catalog[index];

                if (action == null)
                {
                    continue;
                }

                // 직전에 쓴 행동은 되도록 반복하지 않는다(같은 저항을 두 번 연달아 쓰지 않음).
                if (catalog.Count > 1
                    && action.Id == lastMonsterActionId)
                {
                    continue;
                }

                candidates.Add(
                    action);
            }

            if (candidates.Count == 0)
            {
                candidates.AddRange(
                    catalog);
            }

            return candidates[
                rng.NextInt(
                    0,
                    candidates.Count)];
        }
    }
}
