using System;
using System.Collections.Generic;
using ProjectDelta.Application; // ApplicationFlow와 도전과제 진행도 사용
using ProjectDelta.Data; // ProfileData 사용
using ProjectDelta.Domain; // 영구 강화 규칙과 자유 탐험 해금 규칙 사용
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 123일차: 타이틀(새 게임)과 던전 사이의 로비 화면. 126~134일차에 강화 상점·도전과제
    // 연동까지 OnGUI로 채워졌다가, 139일차에 기획서 8.2절 "화면별 정식 UI" 전환의
    // 일환으로 런타임 Canvas 방식으로 옮겼다. 판정/저장 로직(ApplicationFlow 호출)은
    // 그대로 두고 그리는 방식만 바꿨다.
    public sealed class LobbySceneController : MonoBehaviour
    {
        // 130일차: 인벤토리 슬롯(127)·유물 보유량(128)·상점 강화 4종(130) 모두 "레벨 하나 +
        // 비용 조회 델리게이트 + 구매 델리게이트" 모양이 완전히 같아서 공용 행으로 합쳤다.
        private delegate bool TryGetUpgradeCost(
            int currentLevel,
            out int cost);

        // 139일차: Canvas로 옮기면서 각 강화 행이 자기 라벨/구매 버튼을 들고 있다가
        // RefreshUpgradeRows()가 호출될 때마다 최신 레벨/비용/구매 가능 여부로 다시
        // 그려지도록 했다(OnGUI는 매 프레임 다시 그렸지만 Canvas는 값이 바뀔 때만 갱신).
        private sealed class UpgradeRowView
        {
            public Text LabelText;
            public Button BuyButton;
            public Func<string> ComputeLabel;
            public Func<bool> ComputeCanAfford;
        }

        private static readonly string[] UpgradeDisplayNames = // 영구 스탯 표시 이름
        {
            "공격력",
            "방어력",
            "최대 체력"
        };

        private ProfileData profile; // 현재 영구 프로필 데이터
        private AchievementProgressSnapshot achievementProgress; // 현재 도전과제 진행도
        private bool freeExplorationUnlocked; // 자유 탐험 해금 상태

        private Text shardText; // 기억의 조각 표시
        private Text achievementText; // 도전과제 진행도 표시
        private Text freeExplorationText; // 자유 탐험 해금 상태 표시

        private GameObject upgradeShopPanel; // 강화 상점 패널 루트
        private Text upgradeShopToggleText; // 강화 버튼 라벨
        private readonly List<UpgradeRowView> upgradeRows = new List<UpgradeRowView>();

        private void Awake()
        {
            RuntimeUiFactory.EnsureEventSystem();

            RefreshProfile(); // 로비 진입 시 프로필과 메타 진행도 갱신

            Transform canvasTransform =
                RuntimeUiFactory.BuildScreenCanvas(
                    transform,
                    "LobbyCanvas",
                    "로비");

            BuildStatusLabels(
                canvasTransform);

            BuildMainButtons(
                canvasTransform);

            BuildUpgradeShopPanel(
                canvasTransform);

            RefreshLobbyLabels();
            RefreshUpgradeRows();
        }

        private void RefreshProfile()
        {
            profile = // 최신 프로필 읽기
                ApplicationFlow.Current?.ReadOrCreateProfile();

            if (profile == null) // ApplicationFlow 준비 여부 확인
            {
                achievementProgress = AchievementProgressSnapshot.Empty();
                freeExplorationUnlocked = false;
                return;
            }

            achievementProgress = // 기존 영구 기록 기반 도전과제 판정
                AchievementProgressService.EvaluateAndRecord(profile);

            ApplicationFlow.Current?.SyncSteamAchievements( // 134일차: 이번에 새로 True가 된 항목만 Steam으로 전달
                achievementProgress);

            int unlockedMainEndingCount =
                profile.PermanentRecord != null
                && profile.PermanentRecord.UnlockedMainEndingIds != null
                    ? profile.PermanentRecord.UnlockedMainEndingIds.Count
                    : 0;

            freeExplorationUnlocked = // 주요 엔딩 1개 이상 자유 탐험 해금 판정
                FreeExplorationUnlockRule.IsUnlocked(unlockedMainEndingCount);

            if (achievementProgress.NewlyUnlockedCount > 0)
            {
                ApplicationFlow.Current?.WriteProfile(profile); // 신규 달성 ID 영구 저장
            }
        }

        private void BuildStatusLabels(
            Transform parent)
        {
            shardText =
                CreateCenteredLabel(
                    parent,
                    "ShardText",
                    new Vector2(0f, 220f),
                    new Vector2(440f, 28f),
                    18);

            shardText.color =
                new Color(0.86f, 0.72f, 0.3f); // 기억의 조각 금색 계열

            achievementText =
                CreateCenteredLabel(
                    parent,
                    "AchievementText",
                    new Vector2(0f, 190f),
                    new Vector2(440f, 26f),
                    15);

            achievementText.color =
                new Color(0.85f, 0.85f, 0.85f);

            freeExplorationText =
                CreateCenteredLabel(
                    parent,
                    "FreeExplorationText",
                    new Vector2(0f, 160f),
                    new Vector2(440f, 26f),
                    15);

            freeExplorationText.color =
                new Color(0.85f, 0.85f, 0.85f);
        }

        private void BuildMainButtons(
            Transform parent)
        {
            RuntimeUiFactory.CreateCenteredButton(
                parent,
                "EnterDungeonButton",
                new Vector2(0f, 90f),
                new Vector2(220f, 50f),
                "던전 입장",
                20,
                () => ApplicationFlow.Current?.StartNewGame(),
                out _);

            RuntimeUiFactory.CreateCenteredButton(
                parent,
                "UpgradeShopToggleButton",
                new Vector2(0f, 24f),
                new Vector2(220f, 50f),
                "강화",
                20,
                ToggleUpgradeShop,
                out upgradeShopToggleText);

            RuntimeUiFactory.CreateCenteredButton(
                parent,
                "ToTitleButton",
                new Vector2(0f, -42f),
                new Vector2(220f, 50f),
                "타이틀로",
                20,
                () => ApplicationFlow.Current?.EnterTitle(),
                out _);
        }

        // 126일차: 기억의 조각을 소비해 영구 능력치를 강화하는 상점 패널.
        private void BuildUpgradeShopPanel(
            Transform parent)
        {
            // 139일차: level을 값으로 한 번 캡처하면 구매 후 갱신할 때 옛 값을 계속
            // 들고 있게 된다 - getLevel을 델리게이트로 넘겨 매번 프로필에서 다시 읽는다.
            List<(string label, Func<int> getLevel, TryGetUpgradeCost tryGetCost, Func<bool> purchase)> rows =
                new List<(string, Func<int>, TryGetUpgradeCost, Func<bool>)>();

            for (int i = 0; i < PermanentStatUpgradeRule.UpgradableStatIds.Length; i++)
            {
                string statId =
                    PermanentStatUpgradeRule.UpgradableStatIds[i];

                string displayName =
                    UpgradeDisplayNames[i];

                rows.Add(
                    (
                        displayName,
                        () => PermanentStatUpgradeRule.GetLevel(
                            profile?.PermanentGrowth.PermanentStatUpgradeLevels,
                            statId),
                        (int level, out int cost) =>
                            PermanentStatUpgradeRule.TryGetUpgradeCost(
                                profile?.PermanentGrowth.PermanentStatUpgradeLevels,
                                statId,
                                out cost),
                        () => ApplicationFlow.Current != null
                            && ApplicationFlow.Current.TryPurchasePermanentStatUpgrade(
                                statId)
                    ));
            }

            rows.Add(
                (
                    "인벤토리 슬롯",
                    () => profile != null ? profile.PermanentGrowth.InventorySlotUpgradeLevel : 0,
                    InventorySlotUpgradeRule.TryGetUpgradeCost,
                    () => ApplicationFlow.Current != null
                        && ApplicationFlow.Current.TryPurchaseInventorySlotUpgrade()
                ));

            rows.Add(
                (
                    "유물 보유량",
                    () => profile != null ? profile.PermanentGrowth.RelicSlotUpgradeLevel : 0,
                    RelicSlotUpgradeRule.TryGetUpgradeCost,
                    () => ApplicationFlow.Current != null
                        && ApplicationFlow.Current.TryPurchaseRelicSlotUpgrade()
                ));

            rows.Add(
                (
                    "상점 구매 할인",
                    () => profile != null ? profile.PermanentGrowth.ShopDiscountLevel : 0,
                    ShopUpgradeRule.TryGetDiscountUpgradeCost,
                    () => ApplicationFlow.Current != null
                        && ApplicationFlow.Current.TryPurchaseShopDiscountUpgrade()
                ));

            rows.Add(
                (
                    "상점 재고 확장",
                    () => profile != null ? profile.PermanentGrowth.ShopStockLevel : 0,
                    ShopUpgradeRule.TryGetStockUpgradeCost,
                    () => ApplicationFlow.Current != null
                        && ApplicationFlow.Current.TryPurchaseShopStockUpgrade()
                ));

            rows.Add(
                (
                    "희귀 상품 확률",
                    () => profile != null ? profile.PermanentGrowth.ShopRareChanceLevel : 0,
                    ShopUpgradeRule.TryGetRareChanceUpgradeCost,
                    () => ApplicationFlow.Current != null
                        && ApplicationFlow.Current.TryPurchaseShopRareChanceUpgrade()
                ));

            rows.Add(
                (
                    "상점 판매가",
                    () => profile != null ? profile.PermanentGrowth.ShopSellBonusLevel : 0,
                    ShopUpgradeRule.TryGetSellBonusUpgradeCost,
                    () => ApplicationFlow.Current != null
                        && ApplicationFlow.Current.TryPurchaseShopSellBonusUpgrade()
                ));

            float rowHeight = 40f;
            float panelWidth = 340f;

            RectTransform panelRect =
                RuntimeUiFactory.CreateUiObject(
                    "UpgradeShopPanel",
                    parent);

            upgradeShopPanel =
                panelRect.gameObject;

            panelRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            panelRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            panelRect.pivot =
                new Vector2(0.5f, 1f);

            panelRect.anchoredPosition =
                new Vector2(0f, -76f);

            panelRect.sizeDelta =
                new Vector2(panelWidth, rows.Count * rowHeight);

            Image panelImage =
                panelRect.gameObject.AddComponent<Image>();

            panelImage.color =
                new Color(0.1f, 0.1f, 0.14f, 0.85f);

            for (int i = 0; i < rows.Count; i++)
            {
                (string label, Func<int> getLevel, TryGetUpgradeCost tryGetCost, Func<bool> purchase) row =
                    rows[i];

                float rowY =
                    -(i * rowHeight) - 2f;

                RectTransform labelRect =
                    RuntimeUiFactory.CreateUiObject(
                        $"Label_{i}",
                        panelRect);

                labelRect.anchorMin =
                    new Vector2(0f, 1f);

                labelRect.anchorMax =
                    new Vector2(0f, 1f);

                labelRect.pivot =
                    new Vector2(0f, 1f);

                labelRect.anchoredPosition =
                    new Vector2(12f, rowY);

                labelRect.sizeDelta =
                    new Vector2(panelWidth - 100f, rowHeight - 4f);

                Text labelText =
                    labelRect.gameObject.AddComponent<Text>();

                RuntimeUiFactory.ConfigureText(
                    labelText,
                    string.Empty,
                    14,
                    FontStyle.Normal,
                    TextAnchor.MiddleLeft);

                RectTransform buyButtonRect =
                    RuntimeUiFactory.CreateUiObject(
                        $"Buy_{i}",
                        panelRect);

                buyButtonRect.anchorMin =
                    new Vector2(1f, 1f);

                buyButtonRect.anchorMax =
                    new Vector2(1f, 1f);

                buyButtonRect.pivot =
                    new Vector2(1f, 1f);

                buyButtonRect.anchoredPosition =
                    new Vector2(-8f, rowY);

                buyButtonRect.sizeDelta =
                    new Vector2(80f, rowHeight - 4f);

                Image buyButtonImage =
                    buyButtonRect.gameObject.AddComponent<Image>();

                buyButtonImage.color =
                    new Color(0.2f, 0.2f, 0.26f, 1f);

                Button buyButton =
                    buyButtonRect.gameObject.AddComponent<Button>();

                buyButton.targetGraphic =
                    buyButtonImage;

                Func<bool> capturedPurchase =
                    row.purchase;

                buyButton.onClick.AddListener(
                    () =>
                    {
                        if (capturedPurchase())
                        {
                            RefreshProfile(); // 구매 후 프로필과 도전과제 진행도 갱신
                            RefreshLobbyLabels();
                            RefreshUpgradeRows();
                        }
                    });

                RectTransform buyLabelRect =
                    RuntimeUiFactory.CreateStretchedRect(
                        "Label",
                        buyButtonRect);

                Text buyLabelText =
                    buyLabelRect.gameObject.AddComponent<Text>();

                RuntimeUiFactory.ConfigureText(
                    buyLabelText,
                    "구매",
                    14,
                    FontStyle.Normal,
                    TextAnchor.MiddleCenter);

                buyLabelText.raycastTarget =
                    false;

                string capturedLabel =
                    row.label;

                Func<int> capturedGetLevel =
                    row.getLevel;

                TryGetUpgradeCost capturedTryGetCost =
                    row.tryGetCost;

                upgradeRows.Add(
                    new UpgradeRowView
                    {
                        LabelText = labelText,
                        BuyButton = buyButton,
                        ComputeLabel = () => DescribeUpgradeRow(
                            capturedLabel,
                            capturedGetLevel(),
                            capturedTryGetCost),
                        ComputeCanAfford = () => CanAffordRow(
                            capturedGetLevel(),
                            capturedTryGetCost)
                    });
            }

            upgradeShopPanel.SetActive(
                false); // 기본은 접힌 상태
        }

        private static string DescribeUpgradeRow(
            string label,
            int level,
            TryGetUpgradeCost tryGetCost)
        {
            bool hasNextLevel =
                tryGetCost(
                    level,
                    out int cost);

            return hasNextLevel
                ? $"{label}  Lv.{level} → {level + 1}  ({cost} 조각)"
                : $"{label}  Lv.{level} (최대)";
        }

        private bool CanAffordRow(
            int level,
            TryGetUpgradeCost tryGetCost)
        {
            bool hasNextLevel =
                tryGetCost(
                    level,
                    out int cost);

            return hasNextLevel
                && profile != null
                && profile.PermanentGrowth.MemoryShards >= cost;
        }

        private void ToggleUpgradeShop()
        {
            bool nextState =
                !upgradeShopPanel.activeSelf;

            upgradeShopPanel.SetActive(
                nextState);

            upgradeShopToggleText.text =
                nextState
                    ? "강화 닫기"
                    : "강화";
        }

        private void RefreshLobbyLabels()
        {
            int memoryShards =
                profile != null
                    ? profile.PermanentGrowth.MemoryShards
                    : 0;

            shardText.text =
                $"기억의 조각 {memoryShards}";

            int unlockedAchievements =
                achievementProgress != null
                    ? achievementProgress.UnlockedCount
                    : 0;

            int totalAchievements =
                achievementProgress != null
                    ? achievementProgress.TotalCount
                    : AchievementCatalog.ExpectedCount;

            achievementText.text =
                achievementProgress != null
                && achievementProgress.IsComplete
                    ? $"도전과제 {unlockedAchievements} / {totalAchievements}  ★ 완전 달성"
                    : $"도전과제 {unlockedAchievements} / {totalAchievements}";

            freeExplorationText.text =
                freeExplorationUnlocked
                    ? "자유 탐험: 해금"
                    : "자유 탐험: 잠김 (주요 엔딩 1개 필요)";
        }

        private void RefreshUpgradeRows()
        {
            for (int i = 0; i < upgradeRows.Count; i++)
            {
                UpgradeRowView row =
                    upgradeRows[i];

                row.LabelText.text =
                    row.ComputeLabel();

                row.BuyButton.interactable =
                    row.ComputeCanAfford();
            }
        }

        private static Text CreateCenteredLabel(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize)
        {
            RectTransform rect =
                RuntimeUiFactory.CreateUiObject(
                    name,
                    parent);

            rect.anchorMin =
                new Vector2(0.5f, 0.5f);

            rect.anchorMax =
                new Vector2(0.5f, 0.5f);

            rect.pivot =
                new Vector2(0.5f, 0.5f);

            rect.anchoredPosition =
                anchoredPosition;

            rect.sizeDelta =
                size;

            Text text =
                rect.gameObject.AddComponent<Text>();

            RuntimeUiFactory.ConfigureText(
                text,
                string.Empty,
                fontSize,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);

            return text;
        }
    }
}
