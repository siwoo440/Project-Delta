using System;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Application
{
    public sealed class ApplicationFlow
    {
        public static ApplicationFlow Current { get; private set; }

        private readonly ISceneLoaderService _sceneLoader;
        private readonly ILogService _log;
        private readonly ISaveService _saveService;
        private string _pendingSceneName;

        // 109일차: 저장 슬롯 UI가 생기기 전까지는 항상 0번(기존 단일 저장 파일)을 쓴다.
        public int ActiveSlot { get; private set; }

        public ApplicationFlow(
            ISceneLoaderService sceneLoader,
            ILogService log,
            ISaveService saveService)
        {
            _sceneLoader =
                sceneLoader;

            _log =
                log;

            _saveService =
                saveService;

            Current =
                this;
        }

        public void EnterTitle()
        {
            DefeatSceneState.Clear();
            _log.Info("Entering TitleScene");
            _sceneLoader.LoadSingle(SceneNames.Title);
        }

        // 123일차: 타이틀의 "새 게임"이 던전으로 바로 들어가는 대신 로비를 먼저 거친다 -
        // 아직 RunContext를 시작하지 않은 상태다(StartNewGame()이 실제로 회차를 시작한다).
        public void EnterLobby()
        {
            DefeatSceneState.Clear();
            _log.Info("Entering LobbyScene");
            _sceneLoader.LoadSingle(SceneNames.Lobby);
        }

        public void StartNewGame() => StartNewGame(ActiveSlot);

        public void StartNewGame(int slot)
        {
            ActiveSlot =
                slot;

            DefeatSceneState.Clear();
            BattleEncounterCheckpointStore.Clear(); // 전투 체크포인트 초기화
            EventBattleCheckpointStore.Clear(); // 120일차: 이벤트 전투 체크포인트 초기화

            string runId =
                Guid.NewGuid().ToString();

            _log.Info($"Starting new game: {runId} (slot {slot})");
            DungeonSaveMapper.ClearPendingRestore();
            RunContext.Begin(runId);
            ApplyPermanentGrowth(
                RunContext.Current.Player,
                true); // 새 런은 시작부터 영구 강화 보너스만큼 채워서 시작한다.
            ApplyPermanentInventoryGrowth(
                RunContext.Current.Inventory);
            ApplyPermanentRelicGrowth(
                RunContext.Current.Relics);
            LoadWithLoadingScreen(SceneNames.Dungeon);
        }

        // 126일차: 기억의 조각으로 산 영구 능력치 강화를 런 시작 시점에 한 번 계산해 채운다.
        // ProfileData(Data)와 StatBlock(Domain)을 둘 다 아는 Application에서만 연결할 수 있다
        // (PlayerRunState/Domain은 ProfileData를 몰라야 한다).
        private void ApplyPermanentGrowth(
            PlayerRunState player,
            bool refillCurrentResources)
        {
            if (player == null)
            {
                return;
            }

            ProfileData profile =
                ReadOrCreateProfile();

            player.PermanentBonusStats =
                PermanentStatUpgradeRule.BuildBonusStats(
                    profile.PermanentGrowth.PermanentStatUpgradeLevels);

            if (!refillCurrentResources)
            {
                return;
            }

            StatBlock finalStats =
                player.GetFinalStats();

            player.CurrentHp =
                finalStats.MaxHealth;

            player.CurrentMana =
                finalStats.MaxMana;

            player.CurrentStamina =
                finalStats.MaxStamina;
        }

        // 127일차: 인벤토리 슬롯 영구 확장 보너스를 InventoryRunState에 적용한다.
        // 기존 가방(102일차 BagExpansionService) 보너스는 그대로 보존한다.
        private void ApplyPermanentInventoryGrowth(
            InventoryRunState inventory)
        {
            if (inventory == null)
            {
                return;
            }

            ProfileData profile =
                ReadOrCreateProfile();

            int bonusSlots =
                InventorySlotUpgradeRule.GetBonusSlots(
                    profile.PermanentGrowth.InventorySlotUpgradeLevel);

            inventory.SetCapacityBonuses(
                bonusSlots,
                inventory.BagSlotBonus);
        }

        // 128일차: 유물 보유량 영구 확장을 RelicRunState에 적용한다.
        // DungeonSaveMapper가 아직 유물을 저장/복원하지 않으므로(RelicRunState 참고),
        // 127일차 인벤토리처럼 ApplyBasics 이후에 다시 덮어쓸 필요가 없다.
        private void ApplyPermanentRelicGrowth(
            RelicRunState relics)
        {
            if (relics == null)
            {
                return;
            }

            ProfileData profile =
                ReadOrCreateProfile();

            relics.SetMaxCapacity(
                RelicSlotUpgradeRule.GetMaxCapacity(
                    profile.PermanentGrowth.RelicSlotUpgradeLevel));
        }

        // 126일차: 로비 강화 상점 - 기억의 조각을 소비해 한 단계 강화를 산다.
        // 실행 중인 런이 없을 때(로비)만 호출되므로 RunContext는 건드리지 않는다.
        public bool TryPurchasePermanentStatUpgrade(
            string statId)
        {
            ProfileData profile =
                ReadOrCreateProfile();

            if (!PermanentStatUpgradeRule.TryGetUpgradeCost(
                    profile.PermanentGrowth.PermanentStatUpgradeLevels,
                    statId,
                    out int cost))
            {
                return false;
            }

            if (profile.PermanentGrowth.MemoryShards < cost)
            {
                return false;
            }

            profile.PermanentGrowth.MemoryShards -=
                cost;

            int currentLevel =
                PermanentStatUpgradeRule.GetLevel(
                    profile.PermanentGrowth.PermanentStatUpgradeLevels,
                    statId);

            profile.PermanentGrowth.PermanentStatUpgradeLevels[statId] =
                currentLevel + 1;

            WriteProfile(
                profile);

            return true;
        }

        // 127일차: 로비 강화 상점 - 인벤토리 슬롯 확장 구매.
        public bool TryPurchaseInventorySlotUpgrade()
        {
            ProfileData profile =
                ReadOrCreateProfile();

            if (!InventorySlotUpgradeRule.TryGetUpgradeCost(
                    profile.PermanentGrowth.InventorySlotUpgradeLevel,
                    out int cost))
            {
                return false;
            }

            if (profile.PermanentGrowth.MemoryShards < cost)
            {
                return false;
            }

            profile.PermanentGrowth.MemoryShards -=
                cost;

            profile.PermanentGrowth.InventorySlotUpgradeLevel++;

            WriteProfile(
                profile);

            return true;
        }

        // 128일차: 로비 강화 상점 - 유물 보유량 확장 구매.
        public bool TryPurchaseRelicSlotUpgrade()
        {
            ProfileData profile =
                ReadOrCreateProfile();

            if (!RelicSlotUpgradeRule.TryGetUpgradeCost(
                    profile.PermanentGrowth.RelicSlotUpgradeLevel,
                    out int cost))
            {
                return false;
            }

            if (profile.PermanentGrowth.MemoryShards < cost)
            {
                return false;
            }

            profile.PermanentGrowth.MemoryShards -=
                cost;

            profile.PermanentGrowth.RelicSlotUpgradeLevel++;

            WriteProfile(
                profile);

            return true;
        }

        // 130일차: 로비 강화 상점 - 상점 구매 할인 구매.
        public bool TryPurchaseShopDiscountUpgrade()
        {
            ProfileData profile =
                ReadOrCreateProfile();

            if (!ShopUpgradeRule.TryGetDiscountUpgradeCost(
                    profile.PermanentGrowth.ShopDiscountLevel,
                    out int cost))
            {
                return false;
            }

            if (profile.PermanentGrowth.MemoryShards < cost)
            {
                return false;
            }

            profile.PermanentGrowth.MemoryShards -=
                cost;

            profile.PermanentGrowth.ShopDiscountLevel++;

            WriteProfile(
                profile);

            return true;
        }

        // 130일차: 로비 강화 상점 - 상점 재고 확장 구매.
        public bool TryPurchaseShopStockUpgrade()
        {
            ProfileData profile =
                ReadOrCreateProfile();

            if (!ShopUpgradeRule.TryGetStockUpgradeCost(
                    profile.PermanentGrowth.ShopStockLevel,
                    out int cost))
            {
                return false;
            }

            if (profile.PermanentGrowth.MemoryShards < cost)
            {
                return false;
            }

            profile.PermanentGrowth.MemoryShards -=
                cost;

            profile.PermanentGrowth.ShopStockLevel++;

            WriteProfile(
                profile);

            return true;
        }

        // 130일차: 로비 강화 상점 - 희귀 상품 확률 구매.
        public bool TryPurchaseShopRareChanceUpgrade()
        {
            ProfileData profile =
                ReadOrCreateProfile();

            if (!ShopUpgradeRule.TryGetRareChanceUpgradeCost(
                    profile.PermanentGrowth.ShopRareChanceLevel,
                    out int cost))
            {
                return false;
            }

            if (profile.PermanentGrowth.MemoryShards < cost)
            {
                return false;
            }

            profile.PermanentGrowth.MemoryShards -=
                cost;

            profile.PermanentGrowth.ShopRareChanceLevel++;

            WriteProfile(
                profile);

            return true;
        }

        // 130일차: 로비 강화 상점 - 판매 가격 증가 구매.
        public bool TryPurchaseShopSellBonusUpgrade()
        {
            ProfileData profile =
                ReadOrCreateProfile();

            if (!ShopUpgradeRule.TryGetSellBonusUpgradeCost(
                    profile.PermanentGrowth.ShopSellBonusLevel,
                    out int cost))
            {
                return false;
            }

            if (profile.PermanentGrowth.MemoryShards < cost)
            {
                return false;
            }

            profile.PermanentGrowth.MemoryShards -=
                cost;

            profile.PermanentGrowth.ShopSellBonusLevel++;

            WriteProfile(
                profile);

            return true;
        }

        // 130일차: 상점 화면·재고 구성 양쪽에서 같은 판매가 비율을 쓰기 위한 공용 조회.
        // 0.5(기본 50%)에 ShopUpgradeRule.GetSellPricePercent가 반환하는 %를 반영한다.
        public double GetShopSellPriceRatio()
        {
            ProfileData profile =
                ReadOrCreateProfile();

            return ShopUpgradeRule.GetSellPricePercent(
                profile.PermanentGrowth.ShopSellBonusLevel)
                / 100.0;
        }

        // 130일차: 상점 재고를 구성할 때 필요한 강화 수치(할인율·재고 보너스·희귀 확률)를
        // 한 번에 조회한다 - NpcShopStockBuilder가 매번 프로필을 따로 읽지 않도록 묶어준다.
        public ShopUpgradeSnapshot GetShopUpgradeSnapshot()
        {
            ProfileData profile =
                ReadOrCreateProfile();

            return new ShopUpgradeSnapshot(
                ShopUpgradeRule.GetDiscountPercent(
                    profile.PermanentGrowth.ShopDiscountLevel),
                ShopUpgradeRule.GetBonusStockCount(
                    profile.PermanentGrowth.ShopStockLevel),
                ShopUpgradeRule.GetRareChancePercent(
                    profile.PermanentGrowth.ShopRareChanceLevel));
        }

        public bool HasSavedRun() => HasSavedRun(ActiveSlot);

        public bool HasSavedRun(int slot)
        {
            return _saveService != null
                && _saveService.HasRun(slot);
        }

        public void ContinueGame() => ContinueGame(ActiveSlot);

        public void ContinueGame(int slot)
        {
            DefeatSceneState.Clear();

            if (_saveService == null
                || !_saveService.HasRun(slot))
            {
                _log.Info("이어할 저장 데이터가 없어 새 게임으로 시작합니다");
                StartNewGame(slot);
                return;
            }

            ActiveSlot =
                slot;

            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            RunData savedRun =
                _saveService.ReadRun(slot);

            RunContext.Begin(
                savedRun.BasicInfo.RunId);

            ApplyPermanentGrowth(
                RunContext.Current.Player,
                false); // 이어하기는 저장된 현재 자원을 그대로 쓴다 - 강제로 채우지 않는다.

            DungeonSaveMapper.ApplyBasics(
                RunContext.Current,
                savedRun);

            // 127일차: ApplyBasics가 저장 시점의(예전) 인벤토리 슬롯 보너스로 덮어쓰므로,
            // 프로필 기준의 현재 값으로 다시 한번 확정한다 - 순서가 중요하다.
            ApplyPermanentInventoryGrowth(
                RunContext.Current.Inventory);

            ApplyPermanentRelicGrowth(
                RunContext.Current.Relics);

            BattleEncounterCheckpointStore.Restore(
                savedRun.BattleEncounterCheckpoint); // 전투 직전 체크포인트 복원

            // 120일차: 이벤트 전투는 정확한 턴 재현 없이 "있었다"는 사실과 저장 직전 수치만
            // 로그로 남기고 비운다 - 위 일반 전투 체크포인트가 같은 조우를 처음부터 다시 연다.
            EventBattleCheckpointStore.RestoreAndClear(
                savedRun.EventBattleCheckpoint,
                _log.Info);

            DungeonSaveMapper.BeginRestore(
                savedRun);

            _log.Info($"저장된 런 이어하기: {savedRun.BasicInfo.RunId} (slot {slot})");
            LoadWithLoadingScreen(SceneNames.Dungeon);
        }

        // 109일차: 저장 슬롯 UI의 "저장" 버튼 - 현재 진행 중인 런을 지정한 슬롯에
        // 명시적으로 저장한다. 이후 자동 저장도 이 슬롯을 계속 대상으로 삼는다.
        public bool SaveToSlot(
            int slot)
        {
            ActiveSlot =
                slot;

            return TryWriteDungeonProgress();
        }

        // 109일차: 저장 슬롯 UI에서 슬롯 카드 정보를 채울 때 쓴다.
        public bool TryGetSlotSummary(
            int slot,
            out SaveSlotSummary summary)
        {
            if (_saveService == null)
            {
                summary =
                    SaveSlotSummary.Empty(
                        slot);

                return false;
            }

            return _saveService.TryGetRunSummary(
                slot,
                out summary);
        }

        public void SaveDungeonProgress()
        {
            TryWriteDungeonProgress();
        }

        public bool SaveBattleEncounterCheckpoint(
            EncounterContext encounterContext)
        {
            if (encounterContext == null)
            {
                return false;
            }

            BattleEncounterCheckpointStore.Capture(
                encounterContext.RoomId,
                encounterContext.MonsterDefinitionId,
                new Vector2Int(
                    encounterContext.MonsterGridPosition.X,
                    encounterContext.MonsterGridPosition.Z),
                encounterContext.MonsterGroupDefinitionIds); // 전투 직전 체크포인트 생성

            bool saved =
                TryWriteDungeonProgress(); // 전투 시작 직전 자동 저장

            if (saved)
            {
                BattleEncounterCheckpointStore.Clear(); // 전투 중 추가 저장 방지
            }

            return saved;
        }

        // 119일차: Presentation은 Infrastructure(AppRoot/SaveService)를 직접 참조하지
        // 않는다(어셈블리 계층 - Presentation → Application → Data/Domain). ApplicationFlow는
        // 이미 Application 어셈블리에 있으면서 SaveService를 들고 있어서, 프로필 읽기/쓰기도
        // Run 저장(TryWriteDungeonProgress 등)과 같은 방식으로 여기서 중계한다.
        public ProfileData ReadOrCreateProfile()
        {
            if (_saveService == null)
            {
                return new ProfileData();
            }

            return _saveService.HasProfile()
                ? _saveService.ReadProfile()
                : new ProfileData();
        }

        public void WriteProfile(
            ProfileData profile)
        {
            if (profile == null
                || _saveService == null)
            {
                return;
            }

            _saveService.WriteProfile(
                profile);
        }

        public void OpenSettings()
        {
            _log.Info("Opening SettingsScene");
            _sceneLoader.LoadSingle(SceneNames.Settings);
        }

        public void EnterDefeat()
        {
            if (RunContext.Current != null)
            {
                _log.Info("Ending current run after defeat");
                RunContext.End();
                _saveService?.DeleteRun(ActiveSlot);
                DungeonSaveMapper.ClearPendingRestore();
                BattleEncounterCheckpointStore.Clear(); // 패배 체크포인트 제거
                EventBattleCheckpointStore.Clear(); // 120일차: 패배 시 이벤트 전투 체크포인트 제거
            }

            _log.Info("Entering DefeatScene");
            _sceneLoader.LoadSingle(SceneNames.Defeat);
        }

        public void ReturnToTitle()
        {
            if (RunContext.Current != null)
            {
                _log.Info("Abandoning current run");
                RunContext.End();
                _saveService?.DeleteRun(ActiveSlot);
                DungeonSaveMapper.ClearPendingRestore();
                BattleEncounterCheckpointStore.Clear(); // 런 포기 체크포인트 제거
                EventBattleCheckpointStore.Clear(); // 120일차: 런 포기 시 이벤트 전투 체크포인트 제거
            }

            DefeatSceneState.Clear();

            _log.Info("Returning to TitleScene");
            _sceneLoader.LoadSingle(SceneNames.Title);
        }

        // 123일차: "던전 클리어 후 돌아오는 버튼" - ReturnToTitle()과 완전히 같은 정리
        // 절차(런 종료·저장 삭제·체크포인트 정리)를 거치지만 타이틀 대신 로비로 돌아간다.
        // 아직 5층 마왕(124일차)이 없어서 지금은 던전 안 아무 곳에서나 부를 수 있는
        // 일반적인 "나가기" 버튼이다 - 124일차에서 마왕 처치 시 자동으로 이 메서드를
        // 부르도록 연결하면 된다.
        public void ReturnToLobby()
        {
            if (RunContext.Current != null)
            {
                _log.Info("Returning to lobby - ending current run");
                RunContext.End();
                _saveService?.DeleteRun(ActiveSlot);
                DungeonSaveMapper.ClearPendingRestore();
                BattleEncounterCheckpointStore.Clear();
                EventBattleCheckpointStore.Clear();
            }

            DefeatSceneState.Clear();

            _log.Info("Returning to LobbyScene");
            _sceneLoader.LoadSingle(SceneNames.Lobby);
        }

        private bool TryWriteDungeonProgress()
        {
            if (_saveService == null
                || RunContext.Current == null)
            {
                return false;
            }

            RunData data =
                DungeonSaveMapper.BuildFromRunContext(
                    RunContext.Current);

            BattleEncounterCheckpointStore.ApplyTo(
                data); // 대기 중 전투 체크포인트 포함

            EventBattleCheckpointStore.ApplyTo(
                data); // 120일차: 대기 중 이벤트 전투 체크포인트 포함

            _saveService.WriteRun(
                data,
                "InProgress",
                ActiveSlot);

            AutoSaveNotification.RaiseSaved(); // 자동 저장 알림 전파

            return true;
        }

        private void LoadWithLoadingScreen(
            string targetSceneName)
        {
            _pendingSceneName =
                targetSceneName;

            _sceneLoader.LoadSingle(SceneNames.Loading);
        }

        public void ProceedFromLoadingScreen()
        {
            if (string.IsNullOrEmpty(
                    _pendingSceneName))
            {
                _log.Info("No pending scene to load from LoadingScene, returning to Title");
                _sceneLoader.LoadSingle(SceneNames.Title);
                return;
            }

            string target =
                _pendingSceneName;

            _pendingSceneName =
                null;

            _log.Info($"Loading {target} from LoadingScene");
            _sceneLoader.LoadSingle(target);
        }
    }
}
