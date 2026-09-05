using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 123일차: 타이틀(새 게임)과 던전 사이의 로비 화면 - TitleSceneController(24일차)와
    // 완전히 같은 임시 OnGUI 패턴을 따른다. "던전 입장" 버튼이 실제로 새 런을 시작한다
    // (ApplicationFlow.StartNewGame - 이 메서드 자체는 그대로 두고 호출 지점만 옮겼다).
    // TODO: 실제 로비 UI(아트/애니메이션, 인벤토리·유물·상점·탐험 강화 등)는 이후 별도 일차에서 정식으로 만든다.
    public sealed class LobbySceneController : MonoBehaviour
    {
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;

        // 125일차: 영구 성장 재화(기억의 조각) 표시용 스타일.
        private GUIStyle shardStyle;

        // 126일차: 강화 상점 패널 스타일.
        private GUIStyle upgradeRowLabelStyle;
        private GUIStyle upgradeBuyButtonStyle;

        private ProfileData profile;

        // 126일차: 상점 패널을 열고 닫는 상태.
        private bool showUpgradeShop;

        private static readonly string[] UpgradeDisplayNames =
        {
            "공격력",
            "방어력",
            "최대 체력"
        };

        private void OnEnable()
        {
            RefreshProfile();
        }

        private void RefreshProfile()
        {
            profile =
                ApplicationFlow.Current?.ReadOrCreateProfile();
        }

        private void OnGUI()
        {
            EnsureStyles();

            float centerX =
                Screen.width / 2f;

            GUI.Label(
                new Rect(
                    centerX - 200f,
                    Screen.height * 0.25f,
                    400f,
                    60f),
                "로비",
                titleStyle);

            int memoryShards =
                profile != null
                    ? profile.PermanentGrowth.MemoryShards
                    : 0;

            GUI.Label(
                new Rect(
                    centerX - 200f,
                    Screen.height * 0.25f + 56f,
                    400f,
                    28f),
                $"기억의 조각 {memoryShards}",
                shardStyle);

            float buttonWidth = 220f;
            float buttonHeight = 50f;
            float spacing = buttonHeight + 16f;
            float buttonX = centerX - (buttonWidth / 2f);
            float y = Screen.height * 0.45f;

            if (GUI.Button(
                    new Rect(buttonX, y, buttonWidth, buttonHeight),
                    "던전 입장",
                    buttonStyle))
            {
                ApplicationFlow.Current?.StartNewGame();
            }

            y += spacing;

            if (GUI.Button(
                    new Rect(buttonX, y, buttonWidth, buttonHeight),
                    showUpgradeShop
                        ? "강화 닫기"
                        : "강화",
                    buttonStyle))
            {
                showUpgradeShop =
                    !showUpgradeShop;
            }

            y += spacing;

            if (GUI.Button(
                    new Rect(buttonX, y, buttonWidth, buttonHeight),
                    "타이틀로",
                    buttonStyle))
            {
                ApplicationFlow.Current?.EnterTitle();
            }

            if (showUpgradeShop)
            {
                DrawUpgradeShop(
                    centerX,
                    y + spacing);
            }
        }

        // 126일차: 기억의 조각을 소비해 영구 능력치를 강화하는 임시 상점 패널.
        private void DrawUpgradeShop(
            float centerX,
            float startY)
        {
            float panelWidth = 320f;
            float rowHeight = 40f;
            float panelX = centerX - (panelWidth / 2f);
            float y = startY + 12f;

            for (int index = 0;
                 index < PermanentStatUpgradeRule.UpgradableStatIds.Length;
                 index++)
            {
                string statId =
                    PermanentStatUpgradeRule.UpgradableStatIds[index];

                int level =
                    PermanentStatUpgradeRule.GetLevel(
                        profile?.PermanentGrowth.PermanentStatUpgradeLevels,
                        statId);

                bool hasNextLevel =
                    PermanentStatUpgradeRule.TryGetUpgradeCost(
                        profile?.PermanentGrowth.PermanentStatUpgradeLevels,
                        statId,
                        out int cost);

                string rowLabel =
                    hasNextLevel
                        ? $"{UpgradeDisplayNames[index]}  Lv.{level} → {level + 1}  ({cost} 조각)"
                        : $"{UpgradeDisplayNames[index]}  Lv.{level} (최대)";

                GUI.Label(
                    new Rect(panelX, y, panelWidth - 90f, rowHeight),
                    rowLabel,
                    upgradeRowLabelStyle);

                bool canAfford =
                    hasNextLevel
                    && profile != null
                    && profile.PermanentGrowth.MemoryShards >= cost;

                GUI.enabled =
                    canAfford;

                if (GUI.Button(
                        new Rect(panelX + panelWidth - 80f, y, 80f, rowHeight - 4f),
                        "구매",
                        upgradeBuyButtonStyle)
                    && ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchasePermanentStatUpgrade(
                        statId))
                {
                    RefreshProfile();
                }

                GUI.enabled =
                    true;

                y += rowHeight;
            }

            y +=
                rowHeight;

            DrawSimpleUpgradeRow(
                panelX, panelWidth, rowHeight, y,
                "인벤토리 슬롯",
                profile != null ? profile.PermanentGrowth.InventorySlotUpgradeLevel : 0,
                InventorySlotUpgradeRule.TryGetUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseInventorySlotUpgrade());

            y +=
                rowHeight;

            DrawSimpleUpgradeRow(
                panelX, panelWidth, rowHeight, y,
                "유물 보유량",
                profile != null ? profile.PermanentGrowth.RelicSlotUpgradeLevel : 0,
                RelicSlotUpgradeRule.TryGetUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseRelicSlotUpgrade());

            y +=
                rowHeight;

            DrawSimpleUpgradeRow(
                panelX, panelWidth, rowHeight, y,
                "상점 구매 할인",
                profile != null ? profile.PermanentGrowth.ShopDiscountLevel : 0,
                ShopUpgradeRule.TryGetDiscountUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseShopDiscountUpgrade());

            y +=
                rowHeight;

            DrawSimpleUpgradeRow(
                panelX, panelWidth, rowHeight, y,
                "상점 재고 확장",
                profile != null ? profile.PermanentGrowth.ShopStockLevel : 0,
                ShopUpgradeRule.TryGetStockUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseShopStockUpgrade());

            y +=
                rowHeight;

            DrawSimpleUpgradeRow(
                panelX, panelWidth, rowHeight, y,
                "희귀 상품 확률",
                profile != null ? profile.PermanentGrowth.ShopRareChanceLevel : 0,
                ShopUpgradeRule.TryGetRareChanceUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseShopRareChanceUpgrade());

            y +=
                rowHeight;

            DrawSimpleUpgradeRow(
                panelX, panelWidth, rowHeight, y,
                "상점 판매가",
                profile != null ? profile.PermanentGrowth.ShopSellBonusLevel : 0,
                ShopUpgradeRule.TryGetSellBonusUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseShopSellBonusUpgrade());
        }

        // 130일차: 인벤토리 슬롯(127)·유물 보유량(128)·상점 강화 4종(130) 모두 "레벨 하나 +
        // 비용 조회 델리게이트 + 구매 델리게이트" 모양이 완전히 같아서 공용 행 그리기로 합쳤다.
        private delegate bool TryGetUpgradeCost(
            int currentLevel,
            out int cost);

        private void DrawSimpleUpgradeRow(
            float panelX,
            float panelWidth,
            float rowHeight,
            float y,
            string label,
            int level,
            TryGetUpgradeCost tryGetCost,
            System.Func<bool> purchase)
        {
            bool hasNextLevel =
                tryGetCost(
                    level,
                    out int cost);

            string rowLabel =
                hasNextLevel
                    ? $"{label}  Lv.{level} → {level + 1}  ({cost} 조각)"
                    : $"{label}  Lv.{level} (최대)";

            GUI.Label(
                new Rect(panelX, y, panelWidth - 90f, rowHeight),
                rowLabel,
                upgradeRowLabelStyle);

            bool canAfford =
                hasNextLevel
                && profile != null
                && profile.PermanentGrowth.MemoryShards >= cost;

            GUI.enabled =
                canAfford;

            if (GUI.Button(
                    new Rect(panelX + panelWidth - 80f, y, 80f, rowHeight - 4f),
                    "구매",
                    upgradeBuyButtonStyle)
                && purchase())
            {
                RefreshProfile();
            }

            GUI.enabled =
                true;
        }

        private void EnsureStyles()
        {
            if (titleStyle == null)
            {
                titleStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 36,
                        fontStyle = FontStyle.Bold
                    };

                titleStyle.normal.textColor =
                    Color.white;
            }

            if (buttonStyle == null)
            {
                buttonStyle =
                    new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 20
                    };
            }

            if (shardStyle == null)
            {
                shardStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 18
                    };

                shardStyle.normal.textColor =
                    new Color(0.86f, 0.72f, 0.3f);
            }

            if (upgradeRowLabelStyle == null)
            {
                upgradeRowLabelStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 15
                    };

                upgradeRowLabelStyle.normal.textColor =
                    Color.white;
            }

            if (upgradeBuyButtonStyle == null)
            {
                upgradeBuyButtonStyle =
                    new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 14
                    };
            }
        }
    }
}
