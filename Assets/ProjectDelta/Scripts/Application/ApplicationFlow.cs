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

        public void StartNewGame() => StartNewGame(ActiveSlot);

        public void StartNewGame(int slot)
        {
            ActiveSlot =
                slot;

            DefeatSceneState.Clear();
            BattleEncounterCheckpointStore.Clear(); // 전투 체크포인트 초기화

            string runId =
                Guid.NewGuid().ToString();

            _log.Info($"Starting new game: {runId} (slot {slot})");
            DungeonSaveMapper.ClearPendingRestore();
            RunContext.Begin(runId);
            LoadWithLoadingScreen(SceneNames.Dungeon);
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

            DungeonSaveMapper.ApplyBasics(
                RunContext.Current,
                savedRun);

            BattleEncounterCheckpointStore.Restore(
                savedRun.BattleEncounterCheckpoint); // 전투 직전 체크포인트 복원

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
            }

            DefeatSceneState.Clear();

            _log.Info("Returning to TitleScene");
            _sceneLoader.LoadSingle(SceneNames.Title);
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
