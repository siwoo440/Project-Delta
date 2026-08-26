using System;
using System.Text;

namespace ProjectDelta.Application
{
    public static class BattleRewardSummaryFormatter
    {
        private const int MaximumDisplayedItemTypes = 5;

        public static string Build(
            BattleGrowthResult growth,
            BattleDropResult drop)
        {
            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(
                "전투 승리");

            builder.AppendLine();

            AppendGrowth(
                builder,
                growth);

            builder.AppendLine();

            AppendDrops(
                builder,
                drop);

            builder.AppendLine();
            builder.Append(
                "추가 보상 하나를 선택하세요.");

            return builder.ToString();
        }

        private static void AppendGrowth(
            StringBuilder builder,
            BattleGrowthResult growth)
        {
            if (growth == null)
            {
                builder.AppendLine(
                    "획득 경험치 +0 EXP");

                builder.Append(
                    "레벨 변화 없음");

                return;
            }

            builder.Append(
                "획득 경험치 +");

            builder.Append(
                Math.Max(
                    0,
                    growth.EarnedExperience));

            builder.AppendLine(
                " EXP");

            if (growth.GainedLevels > 0)
            {
                builder.Append(
                    "레벨 Lv.");

                builder.Append(
                    growth.PreviousLevel);

                builder.Append(
                    " → Lv.");

                builder.AppendLine(
                    growth.CurrentLevel.ToString());

                builder.Append(
                    "스탯 포인트 +");

                builder.Append(
                    Math.Max(
                        0,
                        growth.GainedStatPoints));

                return;
            }

            builder.Append(
                "레벨 Lv.");

            builder.Append(
                growth.CurrentLevel);

            builder.Append(
                " / 변화 없음");
        }

        private static void AppendDrops(
            StringBuilder builder,
            BattleDropResult drop)
        {
            int gold =
                drop != null
                    ? Math.Max(
                        0,
                        drop.Gold)
                    : 0;

            builder.Append(
                "획득 골드 ");

            builder.Append(
                gold);

            builder.AppendLine(
                " Gold");

            if (drop == null
                || drop.Items == null
                || drop.Items.Count == 0)
            {
                builder.Append(
                    "획득 아이템 없음");

                return;
            }

            builder.AppendLine(
                "획득 아이템");

            int displayCount =
                Math.Min(
                    MaximumDisplayedItemTypes,
                    drop.Items.Count);

            for (int index = 0;
                 index < displayCount;
                 index++)
            {
                BattleDropItemResult item =
                    drop.Items[index];

                if (item == null)
                {
                    continue;
                }

                builder.Append(
                    "- ");

                builder.Append(
                    string.IsNullOrEmpty(
                        item.DisplayName)
                        ? item.ItemId
                        : item.DisplayName);

                builder.Append(
                    " ×");

                builder.Append(
                    Math.Max(
                        0,
                        item.Quantity));

                if (index
                    < displayCount - 1)
                {
                    builder.AppendLine();
                }
            }

            int remaining =
                drop.Items.Count
                - displayCount;

            if (remaining > 0)
            {
                builder.AppendLine();
                builder.Append(
                    "외 ");

                builder.Append(
                    remaining);

                builder.Append(
                    "종");
            }
        }
    }
}
