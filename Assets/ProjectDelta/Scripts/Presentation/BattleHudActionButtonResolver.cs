using System;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 83일차: 기존 Battle HUD의 actionButtons 배열에서 도주 버튼을 안전하게 찾는다.
    public static class BattleHudActionButtonResolver
    {
        private const int LegacyFleeButtonIndex = 2;

        public static Button ResolveFleeButton(
            Button explicitButton,
            Button[] actionButtons)
        {
            if (explicitButton != null)
            {
                return explicitButton;
            }

            if (actionButtons == null
                || actionButtons.Length == 0)
            {
                return null;
            }

            for (int index = 0;
                 index < actionButtons.Length;
                 index++)
            {
                Button candidate =
                    actionButtons[index];

                if (candidate == null)
                {
                    continue;
                }

                if (ContainsFleeKeyword(
                        candidate.name))
                {
                    return candidate;
                }

                Text[] labels =
                    candidate.GetComponentsInChildren<Text>(
                        true);

                for (int labelIndex = 0;
                     labelIndex < labels.Length;
                     labelIndex++)
                {
                    Text label =
                        labels[labelIndex];

                    if (label != null
                        && ContainsFleeKeyword(
                            label.text))
                    {
                        return candidate;
                    }
                }
            }

            if (actionButtons.Length > LegacyFleeButtonIndex)
            {
                return actionButtons[LegacyFleeButtonIndex];
            }

            return null;
        }

        private static bool ContainsFleeKeyword(
            string value)
        {
            if (string.IsNullOrEmpty(
                    value))
            {
                return false;
            }

            return value.IndexOf(
                       "도주",
                       StringComparison.OrdinalIgnoreCase) >= 0
                   || value.IndexOf(
                       "flee",
                       StringComparison.OrdinalIgnoreCase) >= 0
                   || value.IndexOf(
                       "escape",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
