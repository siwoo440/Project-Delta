using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // 123일차: "1층과 5층 고정 테마, 2~4층은 무작위 순서" 규칙. 던전 생성에 이미 쓰는
    // baseSeed로 완전히 결정적이다 - 같은 회차(같은 seed)는 항상 같은 테마 순서를 얻으므로
    // 저장할 필요 없이 매번 다시 계산해도 된다.
    public static class FloorThemeSchedule
    {
        public const int FloorCount = 5;

        private static readonly FloorTheme[] MiddleThemePool =
        {
            FloorTheme.Ruins,
            FloorTheme.Forest,
            FloorTheme.Crypt
        };

        public static FloorTheme GetTheme(
            int baseSeed,
            int floor)
        {
            if (floor <= 1)
            {
                return FloorTheme.Cave;
            }

            if (floor >= FloorCount)
            {
                return FloorTheme.ThroneRoom;
            }

            List<FloorTheme> shuffled =
                ShuffleMiddleThemes(
                    baseSeed);

            int middleIndex =
                floor
                - 2;

            return shuffled[
                middleIndex
                % shuffled.Count];
        }

        private static List<FloorTheme> ShuffleMiddleThemes(
            int baseSeed)
        {
            List<FloorTheme> themes =
                new List<FloorTheme>(
                    MiddleThemePool);

            // 123일차: 실제 층 생성(baseSeed + (floor-1)*1000)과 겹치지 않도록 별도 상수를 더한다.
            Random random =
                new Random(
                    unchecked(
                        baseSeed
                        + 777));

            for (int index = themes.Count - 1; index > 0; index--)
            {
                int swapIndex =
                    random.Next(
                        index + 1);

                (themes[index], themes[swapIndex]) =
                    (themes[swapIndex], themes[index]);
            }

            return themes;
        }
    }
}
