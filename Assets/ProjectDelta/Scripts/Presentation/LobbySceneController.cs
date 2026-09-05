using ProjectDelta.Application; // ApplicationFlow와 도전과제 진행도 사용
using ProjectDelta.Data; // ProfileData 사용
using ProjectDelta.Domain; // 영구 강화 규칙과 자유 탐험 해금 규칙 사용
using UnityEngine; // Unity OnGUI 기능 사용

namespace ProjectDelta.Presentation
{
    // 123일차: 타이틀(새 게임)과 던전 사이의 로비 화면 - TitleSceneController(24일차)와
    // 완전히 같은 임시 OnGUI 패턴을 따른다. "던전 입장" 버튼이 실제로 새 런을 시작한다
    // (ApplicationFlow.StartNewGame - 이 메서드 자체는 그대로 두고 호출 지점만 옮겼다).
    // 134일차: 로비 진입 때 도전과제 100개를 재평가하고 진행도·자유 탐험 해금 상태를 표시한다.
    // TODO: 실제 로비 UI(아트/애니메이션, 인벤토리·유물·상점·탐험 강화 등)는 이후 별도 일차에서 정식으로 만든다.
    public sealed class LobbySceneController : MonoBehaviour
    {
        private GUIStyle titleStyle; // 제목 글자 스타일
        private GUIStyle buttonStyle; // 버튼 글자 스타일

        // 125일차: 영구 성장 재화(기억의 조각) 표시용 스타일.
        private GUIStyle shardStyle; // 기억의 조각 글자 스타일

        // 134일차: 도전과제 진행도와 자유 탐험 해금 상태 표시용 스타일.
        private GUIStyle metaProgressStyle; // 메타 진행도 글자 스타일

        // 126일차: 강화 상점 패널 스타일.
        private GUIStyle upgradeRowLabelStyle; // 강화 항목 글자 스타일
        private GUIStyle upgradeBuyButtonStyle; // 강화 구매 버튼 스타일

        private ProfileData profile; // 현재 영구 프로필 데이터
        private AchievementProgressSnapshot achievementProgress; // 현재 도전과제 진행도
        private bool freeExplorationUnlocked; // 자유 탐험 해금 상태

        // 126일차: 상점 패널을 열고 닫는 상태.
        private bool showUpgradeShop; // 강화 상점 표시 상태

        private static readonly string[] UpgradeDisplayNames = // 영구 스탯 표시 이름
        {
            "공격력", // 공격력 표시 이름
            "방어력", // 방어력 표시 이름
            "최대 체력" // 최대 체력 표시 이름
        };

        private void OnEnable()
        {
            RefreshProfile(); // 로비 진입 시 프로필과 메타 진행도 갱신

            UiScaleSettings.Refresh(); // 136일차: 설정 화면에서 바뀐 UI 배율 반영
        }

        private void RefreshProfile()
        {
            profile = // 최신 프로필 읽기
                ApplicationFlow.Current?.ReadOrCreateProfile();

            if (profile == null) // ApplicationFlow 준비 여부 확인
            {
                achievementProgress = AchievementProgressSnapshot.Empty(); // 빈 도전과제 진행도 적용
                freeExplorationUnlocked = false; // 자유 탐험 잠금 적용
                return; // 추가 갱신 중단
            }

            achievementProgress = // 기존 영구 기록 기반 도전과제 판정
                AchievementProgressService.EvaluateAndRecord(profile);

            ApplicationFlow.Current?.SyncSteamAchievements( // 134일차: 이번에 새로 True가 된 항목만 Steam으로 전달
                achievementProgress);

            int unlockedMainEndingCount = // 기존 세이브 포함 주요 엔딩 획득 수 계산
                profile.PermanentRecord != null
                && profile.PermanentRecord.UnlockedMainEndingIds != null
                    ? profile.PermanentRecord.UnlockedMainEndingIds.Count
                    : 0;

            freeExplorationUnlocked = // 주요 엔딩 1개 이상 자유 탐험 해금 판정
                FreeExplorationUnlockRule.IsUnlocked(unlockedMainEndingCount);

            if (achievementProgress.NewlyUnlockedCount > 0) // 새 도전과제 발생 여부 확인
            {
                ApplicationFlow.Current?.WriteProfile(profile); // 신규 달성 ID 영구 저장
            }
        }

        private void OnGUI()
        {
            EnsureStyles(); // OnGUI 스타일 준비

            Matrix4x4 previousMatrix = // 136일차: UI 배율 적용 - 끝에서 반드시 복원
                UiScaleSettings.ApplyGuiMatrix();

            float centerX = // 화면 가로 중앙 좌표 계산
                Screen.width / 2f;

            GUI.Label( // 로비 제목 표시
                new Rect(
                    centerX - 200f,
                    Screen.height * 0.25f,
                    400f,
                    60f),
                "로비",
                titleStyle);

            int memoryShards = // 현재 기억의 조각 계산
                profile != null
                    ? profile.PermanentGrowth.MemoryShards
                    : 0;

            GUI.Label( // 기억의 조각 표시
                new Rect(
                    centerX - 200f,
                    Screen.height * 0.25f + 56f,
                    400f,
                    28f),
                $"기억의 조각 {memoryShards}",
                shardStyle);

            int unlockedAchievements = // 현재 도전과제 달성 수 계산
                achievementProgress != null
                    ? achievementProgress.UnlockedCount
                    : 0;

            int totalAchievements = // 전체 도전과제 수 계산
                achievementProgress != null
                    ? achievementProgress.TotalCount
                    : AchievementCatalog.ExpectedCount;

            string achievementText = // 도전과제 진행도 문구 구성
                achievementProgress != null
                && achievementProgress.IsComplete
                    ? $"도전과제 {unlockedAchievements} / {totalAchievements}  ★ 완전 달성"
                    : $"도전과제 {unlockedAchievements} / {totalAchievements}";

            GUI.Label( // 도전과제 진행도 표시
                new Rect(
                    centerX - 220f,
                    Screen.height * 0.25f + 84f,
                    440f,
                    26f),
                achievementText,
                metaProgressStyle);

            GUI.Label( // 자유 탐험 해금 상태 표시
                new Rect(
                    centerX - 220f,
                    Screen.height * 0.25f + 110f,
                    440f,
                    26f),
                freeExplorationUnlocked
                    ? "자유 탐험: 해금"
                    : "자유 탐험: 잠김 (주요 엔딩 1개 필요)",
                metaProgressStyle);

            float buttonWidth = 220f; // 버튼 가로 크기
            float buttonHeight = 50f; // 버튼 세로 크기
            float spacing = buttonHeight + 16f; // 버튼 간 세로 간격
            float buttonX = centerX - (buttonWidth / 2f); // 버튼 중앙 정렬 좌표
            float y = Screen.height * 0.45f; // 첫 버튼 세로 시작 좌표

            if (GUI.Button( // 던전 입장 버튼 입력 확인
                    new Rect(buttonX, y, buttonWidth, buttonHeight),
                    "던전 입장",
                    buttonStyle))
            {
                ApplicationFlow.Current?.StartNewGame(); // 새 런 시작
            }

            y += spacing; // 다음 버튼 위치 이동

            if (GUI.Button( // 강화 버튼 입력 확인
                    new Rect(buttonX, y, buttonWidth, buttonHeight),
                    showUpgradeShop
                        ? "강화 닫기"
                        : "강화",
                    buttonStyle))
            {
                showUpgradeShop = // 강화 상점 열림 상태 반전
                    !showUpgradeShop;
            }

            y += spacing; // 다음 버튼 위치 이동

            if (GUI.Button( // 타이틀 이동 버튼 입력 확인
                    new Rect(buttonX, y, buttonWidth, buttonHeight),
                    "타이틀로",
                    buttonStyle))
            {
                ApplicationFlow.Current?.EnterTitle(); // 타이틀 씬 이동
            }

            if (showUpgradeShop) // 강화 상점 표시 여부 확인
            {
                DrawUpgradeShop( // 강화 상점 패널 표시
                    centerX,
                    y + spacing);
            }

            UiScaleSettings.RestoreGuiMatrix( // 136일차: 배율 적용 복원
                previousMatrix);
        }

        // 126일차: 기억의 조각을 소비해 영구 능력치를 강화하는 임시 상점 패널.
        private void DrawUpgradeShop(
            float centerX,
            float startY)
        {
            float panelWidth = 320f; // 강화 패널 가로 크기
            float rowHeight = 40f; // 강화 행 세로 크기
            float panelX = centerX - (panelWidth / 2f); // 강화 패널 중앙 정렬 좌표
            float y = startY + 12f; // 첫 강화 행 세로 좌표

            for (int index = 0; // 영구 스탯 강화 항목 순회 시작
                 index < PermanentStatUpgradeRule.UpgradableStatIds.Length;
                 index++)
            {
                string statId = // 현재 강화 스탯 ID 선택
                    PermanentStatUpgradeRule.UpgradableStatIds[index];

                int level = // 현재 강화 레벨 조회
                    PermanentStatUpgradeRule.GetLevel(
                        profile?.PermanentGrowth.PermanentStatUpgradeLevels,
                        statId);

                bool hasNextLevel = // 다음 강화 비용 존재 여부 확인
                    PermanentStatUpgradeRule.TryGetUpgradeCost(
                        profile?.PermanentGrowth.PermanentStatUpgradeLevels,
                        statId,
                        out int cost);

                string rowLabel = // 강화 행 표시 문구 구성
                    hasNextLevel
                        ? $"{UpgradeDisplayNames[index]}  Lv.{level} → {level + 1}  ({cost} 조각)"
                        : $"{UpgradeDisplayNames[index]}  Lv.{level} (최대)";

                GUI.Label( // 강화 항목 문구 표시
                    new Rect(panelX, y, panelWidth - 90f, rowHeight),
                    rowLabel,
                    upgradeRowLabelStyle);

                bool canAfford = // 현재 구매 가능 여부 계산
                    hasNextLevel
                    && profile != null
                    && profile.PermanentGrowth.MemoryShards >= cost;

                GUI.enabled = // 구매 버튼 활성화 상태 적용
                    canAfford;

                if (GUI.Button( // 영구 스탯 구매 버튼 입력 확인
                        new Rect(panelX + panelWidth - 80f, y, 80f, rowHeight - 4f),
                        "구매",
                        upgradeBuyButtonStyle)
                    && ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchasePermanentStatUpgrade(
                        statId))
                {
                    RefreshProfile(); // 구매 후 프로필과 도전과제 진행도 갱신
                }

                GUI.enabled = // 다음 GUI를 위해 활성화 상태 복원
                    true;

                y += rowHeight; // 다음 강화 행 위치 이동
            }

            y += // 스탯 강화와 확장 강화 사이 여백 적용
                rowHeight;

            DrawSimpleUpgradeRow( // 인벤토리 슬롯 강화 행 표시
                panelX, panelWidth, rowHeight, y,
                "인벤토리 슬롯",
                profile != null ? profile.PermanentGrowth.InventorySlotUpgradeLevel : 0,
                InventorySlotUpgradeRule.TryGetUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseInventorySlotUpgrade());

            y += // 다음 강화 행 위치 이동
                rowHeight;

            DrawSimpleUpgradeRow( // 유물 보유량 강화 행 표시
                panelX, panelWidth, rowHeight, y,
                "유물 보유량",
                profile != null ? profile.PermanentGrowth.RelicSlotUpgradeLevel : 0,
                RelicSlotUpgradeRule.TryGetUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseRelicSlotUpgrade());

            y += // 다음 강화 행 위치 이동
                rowHeight;

            DrawSimpleUpgradeRow( // 상점 구매 할인 강화 행 표시
                panelX, panelWidth, rowHeight, y,
                "상점 구매 할인",
                profile != null ? profile.PermanentGrowth.ShopDiscountLevel : 0,
                ShopUpgradeRule.TryGetDiscountUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseShopDiscountUpgrade());

            y += // 다음 강화 행 위치 이동
                rowHeight;

            DrawSimpleUpgradeRow( // 상점 재고 확장 강화 행 표시
                panelX, panelWidth, rowHeight, y,
                "상점 재고 확장",
                profile != null ? profile.PermanentGrowth.ShopStockLevel : 0,
                ShopUpgradeRule.TryGetStockUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseShopStockUpgrade());

            y += // 다음 강화 행 위치 이동
                rowHeight;

            DrawSimpleUpgradeRow( // 희귀 상품 확률 강화 행 표시
                panelX, panelWidth, rowHeight, y,
                "희귀 상품 확률",
                profile != null ? profile.PermanentGrowth.ShopRareChanceLevel : 0,
                ShopUpgradeRule.TryGetRareChanceUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseShopRareChanceUpgrade());

            y += // 다음 강화 행 위치 이동
                rowHeight;

            DrawSimpleUpgradeRow( // 상점 판매가 강화 행 표시
                panelX, panelWidth, rowHeight, y,
                "상점 판매가",
                profile != null ? profile.PermanentGrowth.ShopSellBonusLevel : 0,
                ShopUpgradeRule.TryGetSellBonusUpgradeCost,
                () => ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryPurchaseShopSellBonusUpgrade());
        }

        // 130일차: 인벤토리 슬롯(127)·유물 보유량(128)·상점 강화 4종(130) 모두 "레벨 하나 +
        // 비용 조회 델리게이트 + 구매 델리게이트" 모양이 완전히 같아서 공용 행 그리기로 합쳤다.
        private delegate bool TryGetUpgradeCost( // 강화 비용 조회 함수 형식
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
            bool hasNextLevel = // 다음 강화 단계 존재 여부 확인
                tryGetCost(
                    level,
                    out int cost);

            string rowLabel = // 단순 강화 행 표시 문구 구성
                hasNextLevel
                    ? $"{label}  Lv.{level} → {level + 1}  ({cost} 조각)"
                    : $"{label}  Lv.{level} (최대)";

            GUI.Label( // 단순 강화 행 문구 표시
                new Rect(panelX, y, panelWidth - 90f, rowHeight),
                rowLabel,
                upgradeRowLabelStyle);

            bool canAfford = // 단순 강화 구매 가능 여부 계산
                hasNextLevel
                && profile != null
                && profile.PermanentGrowth.MemoryShards >= cost;

            GUI.enabled = // 구매 버튼 활성화 상태 적용
                canAfford;

            if (GUI.Button( // 단순 강화 구매 버튼 입력 확인
                    new Rect(panelX + panelWidth - 80f, y, 80f, rowHeight - 4f),
                    "구매",
                    upgradeBuyButtonStyle)
                && purchase())
            {
                RefreshProfile(); // 구매 후 프로필과 도전과제 진행도 갱신
            }

            GUI.enabled = // 다음 GUI를 위해 활성화 상태 복원
                true;
        }

        private void EnsureStyles()
        {
            if (titleStyle == null) // 제목 스타일 생성 여부 확인
            {
                titleStyle = // 제목 스타일 생성
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter, // 제목 가운데 정렬
                        fontSize = 36, // 제목 글자 크기
                        fontStyle = FontStyle.Bold // 제목 굵게 표시
                    };

                titleStyle.normal.textColor = // 제목 흰색 적용
                    Color.white;
            }

            if (buttonStyle == null) // 버튼 스타일 생성 여부 확인
            {
                buttonStyle = // 버튼 스타일 생성
                    new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 20 // 버튼 글자 크기
                    };
            }

            if (shardStyle == null) // 기억의 조각 스타일 생성 여부 확인
            {
                shardStyle = // 기억의 조각 스타일 생성
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter, // 기억의 조각 가운데 정렬
                        fontSize = 18 // 기억의 조각 글자 크기
                    };

                shardStyle.normal.textColor = // 기억의 조각 금색 계열 적용
                    new Color(0.86f, 0.72f, 0.3f);
            }

            if (metaProgressStyle == null) // 메타 진행도 스타일 생성 여부 확인
            {
                metaProgressStyle = // 메타 진행도 스타일 생성
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter, // 메타 진행도 가운데 정렬
                        fontSize = 15 // 메타 진행도 글자 크기
                    };

                metaProgressStyle.normal.textColor = // 메타 진행도 밝은 회색 적용
                    new Color(0.85f, 0.85f, 0.85f);
            }

            if (upgradeRowLabelStyle == null) // 강화 행 스타일 생성 여부 확인
            {
                upgradeRowLabelStyle = // 강화 행 스타일 생성
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft, // 강화 행 왼쪽 정렬
                        fontSize = 15 // 강화 행 글자 크기
                    };

                upgradeRowLabelStyle.normal.textColor = // 강화 행 흰색 적용
                    Color.white;
            }

            if (upgradeBuyButtonStyle == null) // 강화 구매 버튼 스타일 생성 여부 확인
            {
                upgradeBuyButtonStyle = // 강화 구매 버튼 스타일 생성
                    new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 14 // 강화 구매 버튼 글자 크기
                    };
            }
        }
    }
}
