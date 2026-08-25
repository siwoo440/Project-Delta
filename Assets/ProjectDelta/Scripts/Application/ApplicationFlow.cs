using System;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public sealed class ApplicationFlow
    {
        public static ApplicationFlow Current { get; private set; }

        private readonly ISceneLoaderService _sceneLoader;
        private readonly ILogService _log;
        private readonly ISaveService _saveService;
        private string _pendingSceneName;

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

        public void StartNewGame()
        {
            DefeatSceneState.Clear();

            string runId =
                Guid.NewGuid().ToString();

            _log.Info($"Starting new game: {runId}");
            DungeonSaveMapper.ClearPendingRestore();
            RunContext.Begin(runId);
            LoadWithLoadingScreen(SceneNames.Dungeon);
        }

        public bool HasSavedRun()
        {
            return _saveService != null
                && _saveService.HasRun();
        }

        public void ContinueGame()
        {
            DefeatSceneState.Clear();

            if (_saveService == null
                || !_saveService.HasRun())
            {
                _log.Info("이어할 저장 데이터가 없어 새 게임으로 시작합니다");
                StartNewGame();
                return;
            }

            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            RunData savedRun =
                _saveService.ReadRun();

            RunContext.Begin(
                savedRun.BasicInfo.RunId);

            DungeonSaveMapper.ApplyBasics(
                RunContext.Current,
                savedRun);

            DungeonSaveMapper.BeginRestore(
                savedRun);

            _log.Info($"저장된 런 이어하기: {savedRun.BasicInfo.RunId}");
            LoadWithLoadingScreen(SceneNames.Dungeon);
        }

        public void SaveDungeonProgress()
        {
            if (_saveService == null
                || RunContext.Current == null)
            {
                return;
            }

            RunData data =
                DungeonSaveMapper.BuildFromRunContext(
                    RunContext.Current);

            _saveService.WriteRun(
                data,
                "InProgress");
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
                _saveService?.DeleteRun();
                DungeonSaveMapper.ClearPendingRestore();
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
                _saveService?.DeleteRun();
                DungeonSaveMapper.ClearPendingRestore();
            }

            DefeatSceneState.Clear();

            _log.Info("Returning to TitleScene");
            _sceneLoader.LoadSingle(SceneNames.Title);
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
