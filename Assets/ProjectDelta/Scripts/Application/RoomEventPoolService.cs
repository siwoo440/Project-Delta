using System;
using System.Collections.Generic;
using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // 111일차: Event 방에 진입했을 때 보여줄 EventDefinition을 후보 목록에서 하나 고른다.
    // 무작위 판정은 이 프로젝트의 다른 RollService들과 동일하게 System.Random을 받는다.
    public static class RoomEventPoolService
    {
        public static EventDefinition Pick(
            IReadOnlyList<EventDefinition> pool,
            Random random = null)
        {
            if (pool == null
                || pool.Count == 0)
            {
                return null;
            }

            Random rng =
                random
                ?? new Random();

            return pool[
                rng.Next(
                    pool.Count)];
        }
    }
}
